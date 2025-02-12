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

    var exercises = ExerciseList.Exercises;
    bool firstTwoExercisesCompletedOnce = false; 

    for (int i = 0; i < exercises.Count; i++)
    {
        var exercise = exercises[i];
        if (exercise.ExerciseID == 1 || exercise.ExerciseID == 2)
        {
            await RunExerciseAsync(exercise).ConfigureAwait(false); 
            if (exercise.ExerciseID == 2 && !firstTwoExercisesCompletedOnce)
            {
                firstTwoExercisesCompletedOnce = true;

                Console.WriteLine("Repeating Single-Leg Stance - Right Leg...");
                await RunExerciseAsync(exercises.First(e => e.ExerciseID == 1)).ConfigureAwait(false);

                Console.WriteLine("Repeating Single-Leg Stance - Left Leg...");
                await RunExerciseAsync(exercises.First(e => e.ExerciseID == 2)).ConfigureAwait(false);
            }

            continue; 
        }

        await RunExerciseAsync(exercise).ConfigureAwait(false);
    }

    Console.WriteLine("All exercises completed! Well done.");
    _isVisualizing = false;
}


private async Task RunExerciseAsync(ExerciseData exercise) //runs one exercise at a time
{
    Console.WriteLine($"[Exercise]: {exercise.Name} started for {exercise.Timing} seconds...");

    DateTime exerciseStartTime = DateTime.Now;
    int phaseIndex = 0;
    int previousZoneLeft = -1;
    int previousZoneRight = -1;
    int feedbackLeft = -1;
    int feedbackRight = -1;

    while ((DateTime.Now - exerciseStartTime).TotalSeconds < exercise.Timing)
    {
        var phase = exercise.ZoneSequence[phaseIndex];

        Console.WriteLine($"[Phase {phaseIndex + 1}]: {phase.duration} sec");

        DateTime phaseStartTime = DateTime.Now;
        bool lostBalance = false;
        DateTime outOfZoneTimeLeft = DateTime.MinValue;
        DateTime outOfZoneTimeRight = DateTime.MinValue;
        int currentZoneLeft = -1, currentZoneRight = -1;

        while ((DateTime.Now - phaseStartTime).TotalSeconds < phase.duration &&
               (DateTime.Now - exerciseStartTime).TotalSeconds < exercise.Timing)
        {
            double copXLeft = _copXLeft, copYLeft = _copYLeft;
            double copXRight = _copXRight, copYRight = _copYRight;

            if (exercise.LegsUsed == "right")
            {
                currentZoneRight = Feedback(copXRight, copYRight, phase.GreenZoneX, phase.GreenZoneY, phase.RedZoneX, phase.RedZoneY);
                currentZoneLeft = -1; // No tracking for left foot
            }
            else if (exercise.LegsUsed == "left")
            {
                currentZoneLeft = Feedback(copXLeft, copYLeft, phase.GreenZoneX, phase.GreenZoneY, phase.RedZoneX, phase.RedZoneY);
                currentZoneRight = -1; // No tracking for right foot
            }
            else if (exercise.LegsUsed == "both")
            {
                currentZoneLeft = Feedback(copXLeft, copYLeft, phase.GreenZoneX, phase.GreenZoneY, phase.RedZoneX, phase.RedZoneY);
                currentZoneRight = Feedback(copXRight, copYRight, phase.GreenZoneX, phase.GreenZoneY, phase.RedZoneX, phase.RedZoneY);
            }
            if (currentZoneLeft != previousZoneLeft && currentZoneLeft != -1)
            {
                Console.WriteLine($"[Exercise]: Left Foot Changed to Zone {currentZoneLeft}");
                previousZoneLeft = currentZoneLeft;
                feedbackLeft = currentZoneLeft;
            }
            if (currentZoneRight != previousZoneRight && currentZoneRight != -1)
            {
                Console.WriteLine($"[Exercise]: Right Foot Changed to Zone {currentZoneRight}");
                previousZoneRight = currentZoneRight;
                feedbackRight = currentZoneRight;
            }
            if (currentZoneLeft == 1 || currentZoneRight == 1)
            {
                outOfZoneTimeLeft = DateTime.MinValue;
                outOfZoneTimeRight = DateTime.MinValue;
            }
            else
            {
                if (currentZoneLeft != -1 && outOfZoneTimeLeft == DateTime.MinValue)
                {
                    outOfZoneTimeLeft = DateTime.Now;
                }
                if (currentZoneRight != -1 && outOfZoneTimeRight == DateTime.MinValue)
                {
                    outOfZoneTimeRight = DateTime.Now;
                }
                bool leftFootOutTooLong = (outOfZoneTimeLeft != DateTime.MinValue) &&
                                          ((DateTime.Now - outOfZoneTimeLeft).TotalSeconds >= 2);
                bool rightFootOutTooLong = (outOfZoneTimeRight != DateTime.MinValue) &&
                                           ((DateTime.Now - outOfZoneTimeRight).TotalSeconds >= 2);

                if (leftFootOutTooLong || rightFootOutTooLong)
                {
                    lostBalance = true;
                    break;
                }
            }
        }

        if (lostBalance)
        {
            Console.WriteLine("You lost balance, restarting exercise...");
            if (exercise.LegsUsed == "both" || exercise.LegsUsed == "left")
            {
                if (previousZoneLeft != 7)
                {
                    SendFeedback(7, "Left");
                    previousZoneLeft = 7;
                }
            }
            if (exercise.LegsUsed == "both" || exercise.LegsUsed == "right")
            {
                if (previousZoneRight != 7)
                {
                    SendFeedback(7, "Right");
                    previousZoneRight = 7;
                }
            }

            await Task.Delay(5000).ConfigureAwait(false);
            exerciseStartTime = DateTime.Now;
            continue;
        }

        phaseIndex++;
        if (phaseIndex >= exercise.ZoneSequence.Count)
        {
            phaseIndex = 0;
        }
    }

    Console.WriteLine($"[Exercise]: {exercise.Name} fully completed.");

    //send final feedback only for the used foot
    if (exercise.LegsUsed == "both" || exercise.LegsUsed == "left")
    {
        if (feedbackLeft != -1) SendFeedback(feedbackLeft, "Left");
    }
    if (exercise.LegsUsed == "both" || exercise.LegsUsed == "right")
    {
        if (feedbackRight != -1) SendFeedback(feedbackRight, "Right");
    }
}


private void SendFeedback(int feedbackCode, string foot) //for HMD
{
    Console.WriteLine($"[Feedback]: Sending code {feedbackCode} for {foot} foot");
    // Implement feedback sending logic,send to HMD
}

private int Feedback(double copX, double copY,  //calculates the zone/feedback
                          (double Min, double Max) greenZoneX, (double Min, double Max) greenZoneY, 
                          (double Min, double Max) redZoneX, (double Min, double Max) redZoneY)
{
    bool isInGreenZone = copX >= greenZoneX.Min && copX <= greenZoneX.Max &&
                         copY >= greenZoneY.Min && copY <= greenZoneY.Max;
    
    if (isInGreenZone)
    {
        return 1; //Green Zone
    }

    bool isInRedZone = copX >= redZoneX.Min && copX <= redZoneX.Max &&
                       copY >= redZoneY.Min && copY <= redZoneY.Max;
    if (isInRedZone)
    {
        return 2; //Red Zone
    }

    if (copX > 0 && copY > 0)
    {
        return 3; //Front Right
    }
    else if (copX < 0 && copY > 0)
    {
        return 4; //Front Left
    }
    else if (copX > 0 && copY < 0)
    {
        return 5; //Back Right
    }
    else if (copX < 0 && copY < 0)
    {
        return 6; //Back Left
    }

    return 0; 
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