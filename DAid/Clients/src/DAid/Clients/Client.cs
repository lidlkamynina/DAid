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

    // Call the server's HandleConnectCommandAsync and handle the received COM ports
    await _server.HandleConnectCommandAsync(
        cancellationToken,
        async ports =>
        {
            // Print the available COM ports to the console
            Console.WriteLine("[Client]: Available COM ports:");
            foreach (var port in ports)
            {
                Console.WriteLine($"- {port}");
            }
            await Task.CompletedTask; // Ensures the delegate matches the signature
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

        private void HandleStartCommand()
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

            Console.WriteLine("Starting visualization...");
            OpenVisualizationWindow();
            SubscribeToDeviceUpdates();
            _isVisualizing = true;
        }

        private void HandleStopCommand()
{
    if (!_isVisualizing)
    {
        Console.WriteLine("[Client]: Visualization is not running.");
        return;
    }

    Console.WriteLine("[Client]: Stopping visualization and data streams...");
    
    // Stop the data streams for all connected devices through the server
    _server.StopDataStream();

    // Close the visualization window
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

                Console.WriteLine($"[Client]: Subscribed to CoP updates for Device: {device.Name}");
            }
        }

        private void OnCoPUpdated(object sender, (string DeviceName, double CoPX, double CoPY, double[] Pressures) copData)
{
    if (_visualizationWindow == null || _visualizationWindow.IsDisposed) return;

    if (sender is Device device)
    {
        if (device.IsLeftSock) // Assuming Device has an IsLeftSock property
        {
            _visualizationWindow.UpdateVisualization(
                xLeft: copData.CoPX,
                yLeft: copData.CoPY,
                pressuresLeft: copData.Pressures,
                xRight: 0,
                yRight: 0,
                pressuresRight: Array.Empty<double>()
            );
        }
        else
        {
            _visualizationWindow.UpdateVisualization(
                xLeft: 0,
                yLeft: 0,
                pressuresLeft: Array.Empty<double>(),
                xRight: copData.CoPX,
                yRight: copData.CoPY,
                pressuresRight: copData.Pressures
            );
        }
    }
}

    }
}
