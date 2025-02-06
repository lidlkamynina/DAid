using System;
using System.Threading;
using System.Threading.Tasks;
using DAid.Servers;
using System.Linq;

namespace DAid.Clients
{
    public class Client
    {
        private readonly Server _server;
        private VisualizationWindow _visualizationWindow;
        private bool _isCalibrated = false;
        private bool _isVisualizing = false;

        private double _copXLeft = 0, _copYLeft = 0;
        private double _copXRight = 0, _copYRight = 0;
        int setCount = 0;

        public Client(Server server)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Client started. Enter commands: connect, calibrate, start, stop, exit");

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.Write("> ");
                string command = Console.ReadLine()?.Trim().ToLower();

                if (string.IsNullOrWhiteSpace(command)) continue;

                if (command == "exit")
                {
                    Console.WriteLine("Stopping client...");
                    _server.Stop();
                    return;
                }

                try
                {
                    switch (command)
                    {
                        case "connect":
                            await HandleConnectCommandAsync(cancellationToken);
                            break;
                        case "calibrate":
                            HandleCalibrateCommand();
                            break;
                        case "start":
                            HandleStartCommand();
                            break;
                        case "stop":
                            HandleStopCommand();
                            break;
                        default:
                            Console.WriteLine("Unknown command. Valid commands: connect, calibrate, start, stop, exit.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing command '{command}': {ex.Message}");
                }
            }
        }

        private async Task HandleConnectCommandAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("[Client]: Requesting available COM ports from the server...");

            await _server.HandleConnectCommandAsync(
                cancellationToken,
                async ports =>
                {
                    Console.WriteLine("[Client]: Available COM ports:");
                    foreach (var port in ports)
                    {
                        Console.WriteLine($"- {port}");
                    }
                    await Task.CompletedTask;
                });
        }

        private void HandleCalibrateCommand()
        {
            if (_isCalibrated)
            {
                Console.WriteLine("Sensors are already calibrated. Use 'start' to begin visualization.");
                return;
            }

            _server.HandleCalibrateCommand();
            _isCalibrated = true;
            Console.WriteLine("Calibration completed. Use 'start' to begin visualization.");
        }

private async Task HandleStartCommand()
{
    if (!_isCalibrated)
    {
        Console.WriteLine("Calibration is required before starting visualization. Use 'calibrate' first.");
        return;
    }

    if (_isVisualizing)
    {
        Console.WriteLine("Visualization is already running.");
        return;
    }

    _server.StartDataStream();
    OpenVisualizationWindow();
    SubscribeToDeviceUpdates();
    _isVisualizing = true;

    var firstExercise = ExerciseList.Exercises.FirstOrDefault(e => e.ExerciseID == 1);
    if (firstExercise == null)
    {
        Console.WriteLine("Error: First exercise not found!");
        return;
    }

    await RunExerciseAsync(firstExercise);
}


private async Task RunExerciseAsync(ExerciseData exercise)
{
    DateTime startTime = DateTime.Now;
    DateTime outOfZoneTime = DateTime.MinValue;
    bool lostBalance = false;
    DateTime lastRedZoneWarningTime = DateTime.MinValue;
    bool wasInGreenZone = false;
    bool wasInRedZone = false;
    bool wasOutOfZone = false;
    int lastZone = 0; 

    Console.WriteLine($"[Exercise]: {exercise.Name} started for {exercise.Timing} seconds...");

    while ((DateTime.Now - startTime).TotalSeconds < exercise.Timing)
    {
        if (_visualizationWindow == null || _visualizationWindow.IsDisposed) break;

        double copX = 0, copY = 0;
        if (exercise.LegsUsed.Contains("right"))
        {
            copX = _copXRight;
            copY = _copYRight;
        }
        else if (exercise.LegsUsed.Contains("left"))
        {
            copX = _copXLeft;
            copY = _copYLeft;
        }
        else if (exercise.LegsUsed.Contains("both"))
        {
            copX = (_copXLeft + _copXRight) / 2; 
            copY = (_copYLeft + _copYRight) / 2;
        }

        // Determine primary zones (Green and Red)
        bool isGreenZone = exercise.IsInGreenZone(copX, copY);
        bool isRedZone = exercise.IsInRedZone(copX, copY);
        bool isOutOfZone = !isGreenZone && !isRedZone;

        // Determine additional position zones (3, 4, 5, 6)
        int positionZone = 0;
        if (copX > 0 && copY > 0)
        {
            positionZone = 3; // Front Right
        }
        else if (copX < 0 && copY > 0)
        {
            positionZone = 4; // Front Left
        }
        else if (copX > 0 && copY < 0)
        {
            positionZone = 5; // Back Right
        }
        else if (copX < 0 && copY < 0)
        {
            positionZone = 6; // Back Left
        }
        if (isGreenZone && lastZone != 1)
        {
            Console.WriteLine("Zone 1 (Green)");
            lastZone = 1;
            wasInGreenZone = true;
            wasInRedZone = false;
            wasOutOfZone = false;
        }
        else if (isRedZone && !wasInRedZone)
        {
            Console.WriteLine("Zone 2 (Red Zone)");
            if (positionZone > 0)
            {
                Console.WriteLine($"Zone {positionZone}");
            }
            wasInRedZone = true;
            wasOutOfZone = false;
            lastZone = 2;
        }
        else if (isOutOfZone && !wasOutOfZone)
        {
            Console.WriteLine("Out of Zone");
            if (positionZone > 0)
            {
                Console.WriteLine($"Zone {positionZone}");
            }
            wasOutOfZone = true;
            wasInRedZone = false;
            lastZone = 0;
        }
        if (isGreenZone)
        {
            outOfZoneTime = DateTime.MinValue;
        }
        else
        {
            if (outOfZoneTime == DateTime.MinValue)
            {
                outOfZoneTime = DateTime.Now;
            }
            else if ((DateTime.Now - outOfZoneTime).TotalSeconds >= 4)
            {
                lostBalance = true;
                break;
            }
        }
    }

    if (lostBalance)
    {
        Console.WriteLine("You lost balance, exercise restarts in 5 seconds...");
        Thread.Sleep(5000);
        await RunExerciseAsync(exercise);
    }
    else
    {
        Console.WriteLine("Good work! Now is a pause for 15 seconds.");
        Thread.Sleep(15000);

        if (exercise.ExerciseID == 2 && setCount == 0)
        {
            setCount++; // Marks that the first set of exercises has been done once
            var firstExercise = ExerciseList.Exercises.FirstOrDefault(e => e.ExerciseID == 1);
            if (firstExercise != null)
            {
                await RunExerciseAsync(firstExercise);
            }
            return;
        }

        int nextExerciseID = exercise.ExerciseID + 1;
        var nextExercise = ExerciseList.Exercises.FirstOrDefault(e => e.ExerciseID == nextExerciseID);
        if (nextExercise != null)
        {
            await RunExerciseAsync(nextExercise);
        }
        else
        {
            Console.WriteLine("All exercises completed! Well done.");
            _isVisualizing = false;
        }
    }
}

        private void HandleStopCommand()
        {
            if (!_isVisualizing)
            {
                Console.WriteLine("[Client]: Visualization is not running.");
                return;
            }

            Console.WriteLine("[Client]: Stopping visualization and data streams...");
            _server.StopDataStream();
            CloseVisualizationWindow();
            _isVisualizing = false;
            Console.WriteLine("[Client]: Visualization and data streams stopped.");
        }

        private void OpenVisualizationWindow()
        {
            if (_visualizationWindow == null || _visualizationWindow.IsDisposed)
            {
                _visualizationWindow = new VisualizationWindow();
                Task.Run(() => System.Windows.Forms.Application.Run(_visualizationWindow));
            }
        }

        private void CloseVisualizationWindow()
        {
            if (_visualizationWindow != null && !_visualizationWindow.IsDisposed)
            {
                _visualizationWindow.Invoke(new Action(() => _visualizationWindow.Close()));
                _visualizationWindow = null;
            }
        }

        private void SubscribeToDeviceUpdates()
        {
            var activeDevices = _server.Manager.GetConnectedDevices();

            if (!activeDevices.Any())
            {
                Console.WriteLine("[Client]: No active devices to subscribe to.");
                return;
            }

            foreach (var device in activeDevices)
            {
                device.CoPUpdated -= OnCoPUpdated;
                device.CoPUpdated += OnCoPUpdated;
            }
        }
private void OnCoPUpdated(object sender, (string DeviceName, double CoPX, double CoPY, double[] Pressures) copData)
{
    if (_visualizationWindow == null || _visualizationWindow.IsDisposed) return;

    if (sender is Device device)
    {
        if (device.IsLeftSock)
        {
            _copXLeft = copData.CoPX;
            _copYLeft = copData.CoPY;
        }
        else
        {
            _copXRight = copData.CoPX;
            _copYRight = copData.CoPY;
        }

        _visualizationWindow.UpdateVisualization(
            xLeft: _copXLeft,
            yLeft: _copYLeft,
            pressuresLeft: device.IsLeftSock ? copData.Pressures : Array.Empty<double>(),
            xRight: _copXRight,
            yRight: _copYRight,
            pressuresRight: !device.IsLeftSock ? copData.Pressures : Array.Empty<double>()
        );
    }
}


    }
}