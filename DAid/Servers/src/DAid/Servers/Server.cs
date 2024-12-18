using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DAid.Servers
{
    public class Server
    {
        private readonly object syncLock = new object();
        public Manager Manager { get; }
        private bool isRunning;
        private bool isAcquiringData;
        private bool isSensorConnected = false;

        private bool isCalibrating = false; // Prevent multiple calibrations

        // Track connected devices and sensor adapters
        private readonly List<Device> connectedDevices = new List<Device>();
        private readonly List<SensorAdapter> sensorAdapters = new List<SensorAdapter>();

        public Server()
        {
            Manager = new Manager();
        }

        /// <summary>
        /// Starts the server in a separate task, scanning for devices.
        /// </summary>
        public Task StartProcessingAsync(CancellationToken cancellationToken)
        {
            lock (syncLock)
            {
                if (isRunning)
                {
                    Console.WriteLine("[Server]: Already running.");
                    return Task.CompletedTask;
                }

                isRunning = true;
            }

            return Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("[Server]: Starting... Scanning for devices...");
                    Manager.Scan();
                    Console.WriteLine("[Server]: Devices scanned. Ready for commands.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server]: Error during startup: {ex.Message}");
                    Stop();
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Stops the server and cleans up resources.
        /// </summary>
        public void Stop()
        {
            lock (syncLock)
            {
                if (!isRunning)
                {
                    Console.WriteLine("[Server]: Not running.");
                    return;
                }

                Console.WriteLine("[Server]: Stopping...");
                isRunning = false;
                isAcquiringData = false;

                Manager.Cleanup();
                connectedDevices.Clear();
                sensorAdapters.Clear();
            }
        }

        /// <summary>
        /// Connects to a sensor by scanning available COM ports.
        /// </summary>
        public Task HandleConnectCommandAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("[Server]: Scanning available COM ports...");
            var ports = SensorAdapter.ScanPorts();

            if (ports.Count == 0)
            {
                Console.WriteLine("[Server]: No available COM ports.");
                return Task.CompletedTask;
            }

            int deviceCount = 2; // Number of devices to connect
            for (int i = 1; i <= deviceCount; i++)
            {
                Console.Write($"[Server]: Enter the COM port to connect to Device {i}: ");
                string comPort = Console.ReadLine()?.Trim();

                if (!ports.Contains(comPort))
                {
                    Console.WriteLine($"[Server]: Invalid COM port '{comPort}'. Skipping Device {i}.");
                    continue;
                }

                // Check if this port is already connected
                if (connectedDevices.Any(d => d.Path == comPort))
                {
                    Console.WriteLine($"[Server]: Device on {comPort} is already connected. Skipping.");
                    continue;
                }

                var connectedDevice = Manager.Connect(comPort);
                if (connectedDevice != null)
                {
                    connectedDevices.Add(connectedDevice); // Add device to the list
                    sensorAdapters.Add(connectedDevice.SensorAdapter); // Store adapter reference
                    Console.WriteLine($"[Server]: Device {connectedDevice.Name} connected successfully on {comPort}.");
                }
                else
                {
                    Console.WriteLine($"[Server]: Failed to connect to device on {comPort}.");
                }
            }

            Console.WriteLine("[Server]: All devices connected. Waiting for further commands.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles the calibrate command.
        /// </summary>
        public void HandleCalibrateCommand()
        {
            lock (syncLock)
            {
                if (!connectedDevices.Any())
                {
                    Console.WriteLine("[Server]: No devices connected. Use 'connect' command first.");
                    return;
                }

                if (isCalibrating)
                {
                    Console.WriteLine("[Server]: Calibration is already in progress.");
                    return;
                }
            }

            foreach (var device in connectedDevices)
            {
                Console.WriteLine($"[Server]: Calibrating device {device.Name}...");
                device.Calibrate();
            }

            Console.WriteLine("[Server]: Calibration complete for all connected devices.");
        }

        /// <summary>
        /// Starts data acquisition for all connected devices.
        /// </summary>
        private void StartDataStream()
        {
            lock (syncLock)
            {
                if (isAcquiringData)
                {
                    Console.WriteLine("[Server]: Data acquisition is already running.");
                    return;
                }

                isAcquiringData = true;
            }

            foreach (var device in connectedDevices)
            {
                Console.WriteLine($"[Server]: Starting data stream for device {device.Name}...");
                device.Start();
            }
        }

        /// <summary>
        /// Handles the start command to begin visualization.
        /// </summary>
        private void HandleStartVisualizationCommand()
        {
            if (!connectedDevices.Any())
            {
                Console.WriteLine("[Server]: No devices connected. Use 'connect' command first.");
                return;
            }

            StartDataStream();
            Console.WriteLine("[Server]: Data stream started for all devices.");
        }

        /// <summary>
        /// Handles the stop command to stop all device data streams.
        /// </summary>
        private void HandleStopCommand()
        {
            foreach (var device in connectedDevices)
            {
                Console.WriteLine($"[Server]: Stopping data stream for device {device.Name}...");
                device.Stop();
            }

            lock (syncLock)
            {
                isAcquiringData = false;
            }

            Console.WriteLine("[Server]: Data streams stopped for all devices.");
        }

        /// <summary>
        /// Stops the server and exits.
        /// </summary>
        private void HandleExitCommand()
        {
            Console.WriteLine("[Server]: Exiting and cleaning up resources...");
            Stop();
        }
    }
}
