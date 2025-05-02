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

        private string[] ports;
        private bool isRunning;
        private bool isAcquiringData;
        private bool isCalibrating = false;

        private readonly List<Device> connectedDevices = new List<Device>();
        private readonly List<SensorAdapter> sensorAdapters = new List<SensorAdapter>();

        /// <summary>
        /// Callback registered by the client to receive feedback messages.
        /// </summary>
        private Action<string> _onDeviceConnectionFeedback;

        /// <summary>
        /// Registers a callback for sending connection or status messages to the client.
        /// </summary>
        public void RegisterFeedbackCallback(Action<string> callback)
        {
            _onDeviceConnectionFeedback = callback;
        }

        /// <summary>
        /// Sends feedback to client and logs to console.
        /// </summary>
        private void SendFeedbackToClient(string message)
        {
            Console.WriteLine(message);
            _onDeviceConnectionFeedback?.Invoke(message);
        }

        /// <summary>
        /// Initializes the server and device manager.
        /// </summary>
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
        /// Stores the client-selected COM ports.
        /// </summary>
        public void HandlePortResponse(string port1, string port2)
        {
            ports = new[] { port1, port2 };
        }

        /// <summary>
        /// Retrieves the stored COM ports selected by the client.
        /// </summary>
        public string[] GetPorts() => ports;

        /// <summary>
        /// Connects to devices using the selected COM ports and notifies the client of results.
        /// </summary>
        public async Task HandleConnectCommandAsync(CancellationToken cancellationToken, Func<List<string>, Task> sendPortsToClient)
        {
            Console.WriteLine("[Server]: Scanning available COM ports...");
            var ports = SensorAdapter.ScanPorts();
            if (ports.Count == 0)
            {
                SendFeedbackToClient("[Server]: No available COM ports.");
                return;
            }

            Console.WriteLine("[Server]: Received COM ports: " + string.Join(", ", ports));
            await sendPortsToClient(ports.ToList());

            string[] coms = GetPorts();

            for (int i = 0; i < coms.Length; i++)
            {
                string comPort = coms[i];
                if (!SensorAdapter.ScanPorts().Contains(comPort))
                {
                    SendFeedbackToClient($"[Server]: Invalid COM port '{comPort}'. Skipping Device {i + 1}.");
                    continue;
                }

                if (connectedDevices.Any(d => d.Path == comPort))
                {
                    SendFeedbackToClient($"[Server]: Device on {comPort} is already connected. Skipping.");
                    continue;
                }

                var connectedDevice = Manager.Connect(comPort);
                if (connectedDevice != null)
                {
                    connectedDevices.Add(connectedDevice);
                    sensorAdapters.Add(connectedDevice.SensorAdapter);
                    string side = connectedDevice.IsLeftSock ? "Left" : "Right";
                    SendFeedbackToClient($"[Server]: Device {connectedDevice.ModuleName} is a {side} Sock.");
                }
                else
                {
                    SendFeedbackToClient($"[Server]: Device Unknown on {comPort} is a Right Sock (connection failed).");
                }
            }

            SendFeedbackToClient("[Server]: All devices connected. Waiting for further commands.");
        }

        /// <summary>
        /// Handles the calibrate command and performs per-sock calibration.
        /// </summary>
        public void HandleCalibrateCommand()
        {
            lock (syncLock)
            {
                if (!connectedDevices.Any())
                {
                    SendFeedbackToClient("[Server]: No devices connected. Use 'connect' command first.");
                    return;
                }

                if (isCalibrating)
                {
                    SendFeedbackToClient("[Server]: Calibration is already in progress.");
                    return;
                }

                if (!isAcquiringData)
                {
                    StartDataStream();
                }

                isCalibrating = true;
            }

            try
            {
                var sortedDevices = connectedDevices.OrderBy(d => d.IsLeftSock ? 0 : 1).ToList();

                foreach (var device in sortedDevices)
                {
                    foreach (var d in connectedDevices)
                    {
                        if (d.IsStreaming)
                            d.SensorAdapter.StopSensorStream();
                    }

                    device.SensorAdapter.StartSensorStream();

                    string side = device.IsLeftSock ? "Left" : "Right";
                    SendFeedbackToClient($"[Server]: Now calibrating {side} foot (Device {device.ModuleName})...");

                    bool calibrationSuccessful = device.Calibrate(device.IsLeftSock);

                    if (calibrationSuccessful)
                    {
                        SendFeedbackToClient($"[Server]: Calibration successful for {side} foot (Device {device.ModuleName}).");
                    }
                    else
                    {
                        SendFeedbackToClient($"[Server]: Calibration FAILED for {side} foot (Device {device.ModuleName}).");
                    }

                    device.SensorAdapter.StopSensorStream();
                }

                foreach (var d in connectedDevices)
                {
                    d.SensorAdapter.StartSensorStream();
                }
            }
            finally
            {
                lock (syncLock)
                {
                    isCalibrating = false;
                }
            }
        }

        /// <summary>
        /// Starts data acquisition for all connected devices.
        /// </summary>
        public void StartDataStream()
        {
            lock (syncLock)
            {
                if (isAcquiringData)
                {
                    SendFeedbackToClient("[Server]: Data acquisition is already running.");
                    return;
                }

                isAcquiringData = true;
            }

            foreach (var device in connectedDevices)
            {
                SendFeedbackToClient($"[Server]: Starting data stream for device {device.ModuleName}...");
                device.Start();
            }
        }

        /// <summary>
        /// Stops the data streams for all connected devices.
        /// </summary>
        public void StopDataStream()
        {
            lock (syncLock)
            {
                if (!isAcquiringData)
                {
                    SendFeedbackToClient("[Server]: No active data streams to stop.");
                    return;
                }

                SendFeedbackToClient("[Server]: Stopping data streams for all devices...");
                foreach (var device in connectedDevices)
                {
                    try
                    {
                        device.Stop();
                        SendFeedbackToClient($"[Server]: Data stream stopped for device {device.ModuleName}.");
                    }
                    catch (Exception ex)
                    {
                        SendFeedbackToClient($"[Server]: Failed to stop data stream for device {device.ModuleName}. Error: {ex.Message}");
                    }
                }

                isAcquiringData = false;
                SendFeedbackToClient("[Server]: All data streams stopped.");
            }
        }

        /// <summary>
        /// Optional handler for CoP updates, currently unused.
        /// </summary>
        private void OnCoPUpdated(object sender, (string DeviceName, double CoPX, double CoPY, double[] Pressures) copData)
        {
            if (sender is Device device)
            {
                string sockType = device.IsLeftSock ? "Left Sock" : "Right Sock";
                // Optionally log CoP data here
            }
            else
            {
                SendFeedbackToClient("[Server]: CoP update received from an unknown source.");
            }
        }

        /// <summary>
        /// Stops the server and exits.
        /// </summary>
        private void HandleExitCommand()
        {
            SendFeedbackToClient("[Server]: Exiting and cleaning up resources...");
            Stop();
        }
    }
}
