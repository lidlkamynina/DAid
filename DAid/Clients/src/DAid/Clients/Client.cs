using System;
using System.Threading;
using System.Threading.Tasks;
using DAid.Servers;
using System.Collections.Generic;
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
        private ExerciseData _currentExercise;
        private int _currentPhase;

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
    var completedExerciseSets = new HashSet<int>();
    var repeatSet = new Dictionary<int, List<int>>
    {
        { 2, new List<int> { 1, 2 } },  // Repeat 1 & 2 after 2
        { 10, new List<int> { 9, 10 } } // Repeat 9 & 10 after 10
    };

    for (int i = 0; i < exercises.Count; i++)
    {
        var exercise = exercises[i];
         if (!completedExerciseSets.Contains(exercise.RepetitionID))
         {
              if (exercise.Intro > 0)
            {
                await Task.Delay(2000).ConfigureAwait(false);
                Console.WriteLine($"[Intro]: Waiting {exercise.Intro} sec...");
                await Task.Delay(exercise.Intro * 1000).ConfigureAwait(false);
            }

            if (exercise.Demo > 0)
            {
                Console.WriteLine($"[Demo]: Showing {exercise.Demo} sec...");
                await Task.Delay(exercise.Demo * 1000).ConfigureAwait(false);
            }
         }
         if (exercise.PreparationCop > 0)
            {
                await CheckPreparationCop(exercise.PreparationCop, exercise.LegsUsed);
            }
        await RunExerciseAsync(exercise).ConfigureAwait(false);
        if (repeatSet.TryGetValue(exercise.RepetitionID, out var repeatExercises) && !completedExerciseSets.Contains(exercise.RepetitionID))
        {
            completedExerciseSets.Add(exercise.RepetitionID);
            Console.WriteLine($"Repeating Exercises: {string.Join(", ", repeatExercises)}...");
            foreach (var repeatID in repeatExercises)
            {
                var repeatExercise = exercises.FirstOrDefault(e => e.RepetitionID == repeatID);
                if (repeatExercise != null)
                {
                    await RunExerciseAsync(repeatExercise).ConfigureAwait(false);
                }
            }
        }
    }
    Console.WriteLine("All exercises completed!");
    _isVisualizing = false;
}
private async Task CheckPreparationCop(int duration, string activeLeg)
{
    Console.WriteLine($"[Preparation CoP]: Checking for {duration} sec (Active Leg: {activeLeg})...");
    DateTime startTime = DateTime.Now;
    
    while (true) 
    {
        bool isFootValid = false;
        (double Min, double Max) copRangeX = (-2.0, 2.0);
        (double Min, double Max) copRangeY = (-2.0, 2.0);

        double copXLeft = _copXLeft, copYLeft = _copYLeft;
        double copXRight = _copXRight, copYRight = _copYRight;

        if (activeLeg == "left")
        {
            isFootValid = copXLeft >= copRangeX.Min && copXLeft <= copRangeX.Max &&
                          copYLeft >= copRangeY.Min && copYLeft <= copRangeY.Max;
        }
        else if (activeLeg == "right")
        {
            isFootValid = copXRight >= copRangeX.Min && copXRight <= copRangeX.Max &&
                          copYRight >= copRangeY.Min && copYRight <= copRangeY.Max;
        }
        else if (activeLeg == "both") 
        {
            isFootValid = (copXLeft >= copRangeX.Min && copXLeft <= copRangeX.Max &&
                           copYLeft >= copRangeY.Min && copYLeft <= copRangeY.Max) &&
                          (copXRight >= copRangeX.Min && copXRight <= copRangeX.Max &&
                           copYRight >= copRangeY.Min && copYRight <= copRangeY.Max);
        }

        if (isFootValid)
        {
            if ((DateTime.Now - startTime).TotalSeconds >= duration)
            {
                Console.WriteLine($"[Preparation CoP]: {activeLeg} foot correctly positioned for the required time.");
                return;
            }
        }
        else
        {
            startTime = DateTime.Now; 
        }

        await Task.Delay(1000).ConfigureAwait(false); 
    }
}
private async Task RunExerciseAsync(ExerciseData exercise) //runs one exercise at a time
{
    Console.WriteLine($"[Exercise]: {exercise.Name} started for {exercise.TimingCop} seconds...");

    DateTime exerciseStartTime = DateTime.Now;
    int phaseIndex = 0;
    List<int> previousZonesLeft = new List<int>();
    List<int> previousZonesRight = new List<int>();

    while ((DateTime.Now - exerciseStartTime).TotalSeconds < exercise.TimingCop)
    {
        var phase = exercise.ZoneSequence[phaseIndex];

        Console.WriteLine($"[Phase {phaseIndex + 1}]: {phase.Duration} sec");

        DateTime phaseStartTime = DateTime.Now;
        bool lostBalance = false;
         DateTime outOfZoneTimeLeft = DateTime.MinValue;
        DateTime outOfZoneTimeRight = DateTime.MinValue;

        List<int> currentZonesLeft = new List<int>();
        List<int> currentZonesRight = new List<int>();

        while ((DateTime.Now - phaseStartTime).TotalSeconds < phase.Duration &&
               (DateTime.Now - exerciseStartTime).TotalSeconds < exercise.TimingCop)
        {
            double copXLeft = _copXLeft, copYLeft = _copYLeft;
            double copXRight = _copXRight, copYRight = _copYRight;
            currentZonesLeft.Clear();
            currentZonesRight.Clear();

            if (exercise.LegsUsed == "right")
            {
                currentZonesRight = Feedback(copXRight, copYRight, phase.GreenZoneX, phase.GreenZoneY, phase.RedZoneX, phase.RedZoneY);
            }
            else if (exercise.LegsUsed == "left")
            {
                currentZonesLeft = Feedback(copXLeft, copYLeft, phase.GreenZoneX, phase.GreenZoneY, phase.RedZoneX, phase.RedZoneY);
            }
            else if (exercise.LegsUsed == "both") 
            {
                if (exercise.RepetitionID == 5 || exercise.RepetitionID == 6 ){
                    if (phaseIndex == 2 || phaseIndex == 3){
                        currentZonesLeft = AddCopLeft(exercise, phaseIndex);
                    }
                }
                currentZonesLeft = Feedback(copXLeft, copYLeft, phase.GreenZoneX, phase.GreenZoneY, phase.RedZoneX, phase.RedZoneY);
                currentZonesRight = Feedback(copXRight, copYRight, phase.GreenZoneX, phase.GreenZoneY, phase.RedZoneX, phase.RedZoneY);
            }
            foreach (int zone in currentZonesLeft)  // Send feedback for left foot
            {
                if (!previousZonesLeft.Contains(zone) && zone != 1 && zone != 7) // Ignore 1 (Green) and 7 (Balance Lost)
                {
                    //Console.WriteLine($"[Exercise]: Left Foot Changed to Zone {zone}");
                    SendFeedback(zone, "Left");
                }
            }
            foreach (int zone in currentZonesRight)
            {
                if (!previousZonesRight.Contains(zone) && zone != 1 && zone != 7)
                {
                    //Console.WriteLine($"[Exercise]: Right Foot Changed to Zone {zone}");
                    SendFeedback(zone, "Right");
                }
            }
            previousZonesLeft = new List<int>(currentZonesLeft);
            previousZonesRight = new List<int>(currentZonesRight);

            // Check for Green Zone reset
            if (currentZonesLeft.Contains(1) || currentZonesRight.Contains(1))
            {
                //Console.WriteLine($"[Exercise]: Right Foot Changed to Zone {zone}");
                outOfZoneTimeLeft = DateTime.MinValue;
                outOfZoneTimeRight = DateTime.MinValue;
            }
            else
            {
                if (currentZonesLeft.Count > 0 && outOfZoneTimeLeft == DateTime.MinValue)
                {
                    outOfZoneTimeLeft = DateTime.Now;
                }
                if (currentZonesRight.Count > 0 && outOfZoneTimeRight == DateTime.MinValue)
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
                if (!previousZonesLeft.Contains(7))
                {
                    SendFeedback(7, "Left");
                }
            }
            if (exercise.LegsUsed == "both" || exercise.LegsUsed == "right")
            {
                if (!previousZonesRight.Contains(7))
                {
                    SendFeedback(7, "Right");
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
        Console.WriteLine("[Client]: Put leg down");
        await Task.Delay(exercise.Release*1000);

    Console.WriteLine($"[Exercise]: {exercise.Name} fully completed.");

    // Send final feedback only for the used foot
if (exercise.LegsUsed == "both" || exercise.LegsUsed == "left")
{
    foreach (int feedback in previousZonesLeft)
    {
        if (feedback != 1 && feedback != 7) 
        {
            SendFeedback(feedback, "Left");
        }
    }
}

if (exercise.LegsUsed == "both" || exercise.LegsUsed == "right")
{
    foreach (int feedback in previousZonesRight)
    {
        if (feedback != 1 && feedback != 7) 
        {
            SendFeedback(feedback, "Right");
        }
    }
}
}
private List<int> AddCopLeft(ExerciseData exercise, int phaseIndex)
{
    var adjustedZones = new List<int>();
    (int, (double, double), (double, double), (double, double), (double, double)) phaseData;

    if (exercise.RepetitionID == 5 && phaseIndex == 2)
    {
        phaseData = (2, (-1.5, 1.5), (0.3, 5.5), (-2.0, 2.0), (0.0, 6.0));
    }
    else if (exercise.RepetitionID == 5 && phaseIndex == 3)
    {
        phaseData = (2, (-1.5, 1.5), (-3.0, 3.0), (-1.9, 1.9), (-5.0, 5.0));
    }
    else if (exercise.RepetitionID == 6 && phaseIndex == 2)
    {
        phaseData = (2, (-1.5, 1.5), (0.5, 1.9), (-2.0, 2.0), (0.0, 6.0));
    }
    else if (exercise.RepetitionID == 6 && phaseIndex == 3)
    {
        phaseData = (2, (-1.5, 1.5), (0.5, 1.9), (-2.0, 2.0), (0.0, 6.0));
    }
    return adjustedZones;
}

private void SendFeedback(int feedbackCode, string foot)
        {
            Console.WriteLine($"[Feedback]: Sending code {feedbackCode} for {foot} foot");
            //var feedbackMessage = new FeedbackMessage
           // {
            //    MessageType = "Feedback",
           //     RepetitionID = currentRepetitionID,
            //    Foot = foot,
           //     Zone = feedbackCode
            //};
            //SendDataToHMD(feedbackMessage);
        }

private List<int> Feedback(double copX, double copY,  
                          (double Min, double Max) greenZoneX, (double Min, double Max) greenZoneY, 
                          (double Min, double Max) redZoneX, (double Min, double Max) redZoneY)
{
    List<int> feedbacks = new List<int>();

    // Green Zone takes priority and should return immediately
    bool isInGreenZone = copX >= greenZoneX.Min && copX <= greenZoneX.Max &&
                         copY >= greenZoneY.Min && copY <= greenZoneY.Max;
    if (isInGreenZone)
    {
        return new List<int> { 1 }; 
    }
    bool isInRedZone = copX >= redZoneX.Min && copX <= redZoneX.Max &&
                       copY >= redZoneY.Min && copY <= redZoneY.Max;
    if (isInRedZone)
    {
        feedbacks.Add(2); // Red Zone
    }
    if (copX < 0)
    {
        feedbacks.Add(3); // Back
    }
    if (copX > 0)
    {
        feedbacks.Add(4); // Front 
    }
    if (copY > 0)
    {
        feedbacks.Add(5); // Right
    }
    if (copY < 0)
    {
        feedbacks.Add(6); // Left
    }
    return feedbacks.Count > 0 ? feedbacks : new List<int> { 0 };
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
         if ((_currentExercise?.RepetitionID == 5 || _currentExercise?.RepetitionID == 6) && _currentPhase == 8)
        {
            Console.WriteLine($"[Client]: Skipping CoP check for Exercise {_currentExercise.RepetitionID}, Phase 8.");
            return; 
        }
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