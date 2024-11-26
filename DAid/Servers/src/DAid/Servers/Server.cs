using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DAid.Servers
{
    /// <summary>
    /// The Server class handles incoming commands and manages sensor connections.
    /// </summary>
    public class Server
    {
        private readonly object syncLock = new object();
        private SensorAdapter sensorAdapter;
        public Manager Manager { get; }
        private bool isRunning;
        private bool isAcquiringData;

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

                if (sensorAdapter != null)
                {
                    try
                    {
                        sensorAdapter.Cleanup();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Server]: Error during sensor cleanup: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Processes a command sent by the client.
        /// </summary>
        public async Task HandleCommandAsync(string command, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[Server]: Command received: {command}");

            try
            {
                switch (command.ToLower())
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
                    case "exit":
                        HandleExitCommand();
                        break;
                    default:
                        Console.WriteLine("[Server]: Unknown command. Valid commands: connect, calibrate, start, stop, exit.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server]: Error processing command '{command}': {ex.Message}");
            }
        }

        /// <summary>
        /// Connects to a sensor by scanning available COM ports.
        /// </summary>
        private async Task HandleConnectCommandAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("[Server]: Scanning available COM ports...");
            var ports = SensorAdapter.ScanPorts();

            if (ports.Count == 0)
            {
                Console.WriteLine("[Server]: No available COM ports.");
                return;
            }

            string comPort = ports[0];

            lock (syncLock)
            {
                sensorAdapter = new SensorAdapter();
            }

            try
            {
                sensorAdapter.Initialize(comPort, 9600);
                Console.WriteLine($"[Server]: Sensor connected on {comPort}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server]: Failed to connect sensor: {ex.Message}");
            }
        }

        /// <summary>
        /// Calibrates the connected sensor.
        /// </summary>
        private void HandleCalibrateCommand()
        {
            lock (syncLock)
            {
                if (sensorAdapter == null)
                {
                    Console.WriteLine("[Server]: No sensor connected. Use 'connect' command first.");
                    return;
                }
            }

            Console.WriteLine("[Server]: Calibrating sensor...");
            try
            {
                bool success = sensorAdapter.Calibrate();
                Console.WriteLine(success ? "[Server]: Sensor calibrated successfully." : "[Server]: Sensor calibration failed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server]: Error during calibration: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts data acquisition from the sensor.
        /// </summary>
        private void HandleStartCommand()
        {
            lock (syncLock)
            {
                if (sensorAdapter == null)
                {
                    Console.WriteLine("[Server]: No sensor connected. Use 'connect' command first.");
                    return;
                }

                if (isAcquiringData)
                {
                    Console.WriteLine("[Server]: Data acquisition already in progress.");
                    return;
                }

                isAcquiringData = true;
            }

            Console.WriteLine("[Server]: Starting data acquisition...");
            try
            {
                sensorAdapter.StartSensorStream();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server]: Error starting data acquisition: {ex.Message}");
                lock (syncLock) { isAcquiringData = false; }
            }
        }

        /// <summary>
        /// Stops data acquisition from the sensor.
        /// </summary>
        private void HandleStopCommand()
        {
            lock (syncLock)
            {
                if (sensorAdapter == null || !isAcquiringData)
                {
                    Console.WriteLine("[Server]: No data acquisition in progress to stop.");
                    return;
                }

                isAcquiringData = false;
            }

            Console.WriteLine("[Server]: Stopping data acquisition...");
            try
            {
                sensorAdapter.StopSensorStream();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server]: Error stopping data acquisition: {ex.Message}");
            }
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
