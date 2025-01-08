using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using NLog;

namespace DAid.Servers
{
    public sealed class Device
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string path;
        private readonly int baudRate;
        private SensorAdapter sensorAdapter;

        private readonly object _syncLock = new object();
        private readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
        private string logFilePath; // Path for the CSV log file
        private CancellationTokenSource loggingCancellationTokenSource; // Added declaration

        private bool isLogging; // Flag to control logging state

        public event EventHandler<string> RawDataReceived;
        public event EventHandler<(string DeviceName, double CoPX, double CoPY, double[] Pressures)> CoPUpdated;

        public bool IsConnected { get; private set; }
        public string ModuleName { get; private set; } = "Unknown";
        public bool IsLeftSock { get; private set; } = false;

        public bool IsStreaming { get; private set; } // Tracks streaming state
        public string Path => path;                  // COM port path
        public float Frequency { get; private set; }
        public string Name { get; private set; }     // Device name for identification

        // Expose the SensorAdapter as a read-only property
        public SensorAdapter SensorAdapter => sensorAdapter;

        public Device(string path, float frequency, string name)
        {
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            this.baudRate = (int)frequency;
            this.Frequency = frequency;
            this.Name = name ?? throw new ArgumentNullException(nameof(name));

            ModuleName = "Unknown"; // Default value before retrieving module info
            IsLeftSock = false;     // Default to right sock until determined
            InitializeSensorAdapter();
        }

        private void InitializeSensorAdapter()
        {
            sensorAdapter = new SensorAdapter(Name);

            // Subscribe to events
            sensorAdapter.RawDataReceived += OnRawDataReceived;
            sensorAdapter.CoPUpdated += OnCoPUpdated;

            sensorAdapter.ModuleInfoUpdated += (sender, moduleInfo) =>
            {
                ModuleName = moduleInfo.ModuleName;
                IsLeftSock = moduleInfo.IsLeftSock;
                logger.Info($"Device {ModuleName} updated: IsLeftSock={IsLeftSock}");
            };
        }

        public void Connect()
        {
            lock (_syncLock)
            {
                if (IsConnected)
                {
                    logger.Info($"Device {Name} on {path} is already connected.");
                    return;
                }

                logger.Info($"Connecting to device {Name} on {path}...");
                try
                {
                    sensorAdapter.Initialize(path, baudRate);

                    // Retrieve module name
                    sensorAdapter.RetrieveModuleName();
                    while (!sensorAdapter.moduleNameRetrieved)
                    {
                        Thread.Sleep(500); // Wait for retrieval
                    }

                    // Persist module information
                    ModuleName = sensorAdapter.ModuleName;
                    IsLeftSock = int.TryParse(ModuleName, out int moduleNumber) && moduleNumber % 2 != 0;

                    logger.Info($"Device {ModuleName} is a {(IsLeftSock ? "Left" : "Right")} sock.");
                    IsConnected = true;
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to connect to device on {path}: {ex.Message}");
                    IsConnected = false;
                }
            }
        }

        public void Start()
        {
            lock (_syncLock)
            {
                if (!IsConnected)
                {
                    logger.Warn($"Cannot start acquisition for device {Name} because it is not connected.");
                    return;
                }

                if (IsStreaming)
                {
                    logger.Info($"Data acquisition for device {Name} is already in progress.");
                    return;
                }

                try
                {
                    sensorAdapter.StartSensorStream();
                    IsStreaming = true;

                    loggingCancellationTokenSource = new CancellationTokenSource();
                    StartLogging(loggingCancellationTokenSource.Token);
                    logger.Info($"Data acquisition started for device {Name}.");
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to start data acquisition for {Name}: {ex.Message}");
                    IsStreaming = false;
                }
            }
        }

        public void Stop()
        {
            lock (_syncLock)
            {
                if (!IsConnected)
                {
                    logger.Warn($"Cannot stop acquisition for device {Name} on {path} because it is not connected.");
                    return;
                }

                if (!IsStreaming)
                {
                    logger.Info($"Data acquisition for device {Name} is not currently running.");
                    return;
                }

                logger.Info($"Stopping data acquisition for device {Name} on {path}...");
                try
                {
                    sensorAdapter.StopSensorStream();
                    StopLogging();
                    loggingCancellationTokenSource?.Cancel();
                    IsStreaming = false;
                    logger.Info($"Device {Name} on {path} stopped successfully.");
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to stop data acquisition for {Name} on {path}: {ex.Message}");
                }
            }
        }

        public bool Calibrate()
        {
            try
            {
                bool success = sensorAdapter.Calibrate();
                if (success)
                {
                    logger.Info($"Device {Name} calibration successful.");
                    return true;
                }
                else
                {
                    logger.Warn($"Device {Name} calibration failed.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Device {Name}]: Error during calibration: {ex.Message}");
                return false;
            }
        }

        private void StartLogging(CancellationToken cancellationToken)
        {
            const int expectedIntervalMs = 1000; // Expected interval between data points in milliseconds
            DateTime? lastTimestamp = null;

            // log file path
            logFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{Name}_LogWithTimescale_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            File.AppendAllText(logFilePath, "Timestamp,RawData\n");

            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    while (logQueue.TryDequeue(out string logEntry))
                    {
                        string[] parts = logEntry.Split(',');
                        if (parts.Length < 2) continue;

                        DateTime currentTimestamp = DateTime.Parse(parts[0], CultureInfo.InvariantCulture);
                        string rawData = parts[1];

                        if (lastTimestamp.HasValue && (currentTimestamp - lastTimestamp.Value).TotalMilliseconds > expectedIntervalMs)
                        {
                            DateTime gapStart = lastTimestamp.Value.AddMilliseconds(expectedIntervalMs);
                            while (gapStart < currentTimestamp)
                            {
                                string placeholder = $"{gapStart:yyyy-MM-dd HH:mm:ss},NO DATA\n";
                                File.AppendAllText(logFilePath, placeholder);
                                gapStart = gapStart.AddMilliseconds(expectedIntervalMs);
                            }
                        }

                        string currentLog = $"{currentTimestamp:yyyy-MM-dd HH:mm:ss},{rawData}\n";
                        File.AppendAllText(logFilePath, currentLog);
                        lastTimestamp = currentTimestamp;
                    }

                    await Task.Delay(100);
                }
            }, cancellationToken);
        }

        private void StopLogging()
        {
            isLogging = false;
        }

        private void OnRawDataReceived(object sender, string rawData)
        {
            if (!IsStreaming) return;

            // Add a timestamp to the raw data
            string timestamp = DateTime.Now.ToString("o", CultureInfo.InvariantCulture); // ISO 8601 format
            logQueue.Enqueue($"{timestamp},{rawData}");

            // Trigger RawDataReceived event for this device
            Task.Run(() => RawDataReceived?.Invoke(this, rawData));
        }

        private void OnCoPUpdated(object sender, (double CoPX, double CoPY, double[] Pressures) copData)
        {
            if (sender == sensorAdapter)
            {
                string sockType = IsLeftSock ? "Left Sock" : "Right Sock";

                // Forward the CoP data with device name
                CoPUpdated?.Invoke(this, (Name, copData.CoPX, copData.CoPY, copData.Pressures));
            }
            else
            {
                Console.WriteLine("[Device]: CoP update received from an unknown source.");
            }
        }

        public double[] GetSensorPressures()
        {
            if (!IsConnected || !IsStreaming)
            {
                logger.Warn($"Attempted to get sensor pressures for device {Name} while not streaming.");
                return Array.Empty<double>();
            }

            return sensorAdapter.GetSensorPressures();
        }
    }
}
