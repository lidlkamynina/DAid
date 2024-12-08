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
                            await HandleCalibrateCommandAsync(cancellationToken);
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
            Console.WriteLine("Connecting to the server...");
            await _server.HandleCommandAsync("connect", cancellationToken);
            Console.WriteLine("Connection completed. Use 'calibrate' to start calibration.");
        }

        private async Task HandleCalibrateCommandAsync(CancellationToken cancellationToken)
        {
            if (_isCalibrated)
            {
                Console.WriteLine("Sensors are already calibrated. Use 'start' to begin visualization.");
                return;
            }

            Console.WriteLine("Calibrating sensors...");
            await _server.HandleCommandAsync("calibrate", cancellationToken);

            _isCalibrated = true;
            Console.WriteLine("Calibration completed. Data stream will remain active. Use 'start' to begin visualization.");
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
                Console.WriteLine("Visualization is not running.");
                return;
            }

            Console.WriteLine("Stopping visualization...");
            CloseVisualizationWindow();
            _isVisualizing = false;
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
    var activeDevice = _server.Manager.GetActiveDevice();

    if (activeDevice == null)
    {
        Console.WriteLine("[Client]: No active device to subscribe to.");
        return;
    }

    activeDevice.CoPUpdated -= OnCoPUpdated;
    activeDevice.CoPUpdated += OnCoPUpdated;

    Console.WriteLine($"[Client]: Subscribed to CoP updates for Device: {activeDevice.Name}");
}




        private void OnCoPUpdated(object sender, (double CoPX, double CoPY, double[] Pressures) copData)
        {

            if (_visualizationWindow == null || _visualizationWindow.IsDisposed) return;

            _visualizationWindow.Invoke(new Action(() =>
            {
                _visualizationWindow.UpdateVisualization(copData.CoPX, copData.CoPY, copData.Pressures);
            }));
        }
    }
}
