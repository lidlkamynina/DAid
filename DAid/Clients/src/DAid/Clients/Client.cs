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

            Console.WriteLine("Requesting server to calibrate connected devices...");
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

    var exercise = ExerciseList.Exercises.FirstOrDefault(e => e.ExerciseID == 1);
    if (exercise == null)
    {
        Console.WriteLine("Error: Exercise not found!");
        return;
    }

    OpenVisualizationWindow();
    SubscribeToDeviceUpdates();
    _isVisualizing = true;

    await RunExerciseAsync(exercise);
}


private async Task RunExerciseAsync(ExerciseData exercise)
{
    DateTime startTime = DateTime.Now;
    DateTime outOfZoneTime = DateTime.MinValue;
    bool lostBalance = false;
    DateTime lastRedZoneWarningTime = DateTime.MinValue;
    bool wasInGreenZone = false;
    bool wasInRedZone = false;

    Console.WriteLine($"[Exercise]: {exercise.Name} started for {exercise.Timing} seconds...");

    while ((DateTime.Now - startTime).TotalSeconds < exercise.Timing)
    {
        if (_visualizationWindow == null || _visualizationWindow.IsDisposed) break;

        bool isInGreenZone = false;
        bool isInRedZone = false;

        if (exercise.LegsUsed.Contains("right"))
        {
            isInGreenZone = exercise.IsInGreenZone(_copXRight, _copYRight);
            isInRedZone = exercise.IsInRedZone(_copXRight, _copYRight);
        }
        else if (exercise.LegsUsed.Contains("left"))
        {
            isInGreenZone = exercise.IsInGreenZone(_copXLeft, _copYLeft);
            isInRedZone = exercise.IsInRedZone(_copXLeft, _copYLeft);
        }
        else if (exercise.LegsUsed.Contains("both"))
        {
            isInGreenZone = exercise.IsInGreenZone(_copXLeft, _copYLeft) && exercise.IsInGreenZone(_copXRight, _copYRight);
            isInRedZone = exercise.IsInRedZone(_copXLeft, _copYLeft) || exercise.IsInRedZone(_copXRight, _copYRight);
        }

        if (isInGreenZone && !wasInGreenZone)
        {
            Console.WriteLine("Entered Green Zone");
            wasInGreenZone = true;
            wasInRedZone = false;
        }
        else if (isInRedZone && !wasInRedZone)
        {
            Console.WriteLine("Entered Red Zone. Center your leg.");
            wasInRedZone = true;
            wasInGreenZone = false;
        }

        if (isInGreenZone)
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
        await Task.Delay(5000);
    
        Console.WriteLine($"Restarting {exercise.Name}...");
        await RunExerciseAsync(exercise);
    }
    else
    {
        Console.WriteLine("Good work! Now is a pause for 15 seconds.");
        await Task.Delay(15000);

        int nextExerciseID = exercise.ExerciseID + 1;
        var nextExercise = ExerciseList.Exercises.FirstOrDefault(e => e.ExerciseID == nextExerciseID);
        if (nextExercise != null)
        {
            Console.WriteLine($"➡️ Starting next exercise: {nextExercise.Name}");
            await RunExerciseAsync(nextExercise);
        }
        else
        {
            Console.WriteLine("🎉 All exercises completed! Well done.");
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
