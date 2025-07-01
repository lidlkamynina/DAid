using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using NLog;
using System.Text;

namespace DAid.Servers
{
    /// <summary>
    /// Represents a physical sensor device, managing sensor data streams, logging, and calibration processes.
    /// </summary>
    public sealed class Device
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string path;
        private readonly int baudRate;
        private SensorAdapter sensorAdapter;

        private readonly object _syncLock = new object();
        private readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
        private string logFilePath; // Path for the CSV log file
        private CancellationTokenSource loggingCancellationTokenSource; 

        private bool isLogging; 
        /// <summary>
        /// Occurs when raw data is received from the sensor.
        /// </summary>
        public event EventHandler<string> RawDataReceived;
        /// <summary>
        /// Occurs when Center of Pressure (CoP) data is updated.
        /// Provides device name, X and Y coordinates, and sensor pressures.
        /// </summary>
        public event EventHandler<(string DeviceName, double CoPX, double CoPY, double[] Pressures)> CoPUpdated;
        
        /// <summary>
        /// Gets a value indicating whether the device is currently connected.
        /// </summary>
        public bool IsConnected { get; private set; }
        
        /// <summary>
        /// Gets the module name of the sensor device.
        /// </summary>
        public string ModuleName { get; private set; } = "Unknown";
        
        /// <summary>
        /// Indicates whether this device is associated with the left sock.
        /// </summary>
        public bool IsLeftSock { get; private set; } = false;
        
        /// <summary>
        /// Indicates whether the device is currently streaming data.
        /// </summary>
        public bool IsStreaming { get; private set; } // Tracks streaming state
        
        /// <summary>
        /// Gets the COM port path associated with the device.
        /// </summary>
        public string Path => path; 
        /// <summary>
        /// Gets the frequency at which the sensor streams data.
        /// </summary>
        public float Frequency { get; private set; }

        /// <summary>
        /// Gets the name identifier for the device.
        /// </summary>
        public string Name { get; private set; }     // Device name for identification

        public SensorAdapter SensorAdapter => sensorAdapter;
        /// <summary>
        /// Initializes a new instance of the <see cref="Device"/> class with specified port, frequency, and name.
        /// </summary>
        /// <param name="path">COM port path.</param>
        /// <param name="frequency">Data stream frequency.</param>
        /// <param name="name">Device identification name.</param>
        public Device(string path, float frequency, string name)
        {
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            this.baudRate = (int)frequency;
            this.Frequency = frequency;
            this.Name = name ?? throw new ArgumentNullException(nameof(name));

            ModuleName = "Unknown"; 
            IsLeftSock = false;     // Default to right sock until determined
            InitializeSensorAdapter();
        }
        /// <summary>
        /// Initializes the SensorAdapter and subscribes to sensor events.
        /// </summary>
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
        /// <summary>
        /// Establishes connection to the sensor device.
        /// </summary>
        public void Connect()
        {
            lock (_syncLock)
            {
                if (IsConnected)
                {
                    return;
                }
                try
                {
                    sensorAdapter.Initialize(path, baudRate);
                    sensorAdapter.RetrieveModuleName();
                    while (!sensorAdapter.moduleNameRetrieved)
                    {
                        Thread.Sleep(500); 
                    }
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
        /// <summary>
        /// Starts data streaming and logging from the sensor device.
        /// </summary>
        public void Start()
        {
            lock (_syncLock)
            {
                if (!IsConnected)
                {
                    return;
                }

                if (IsStreaming)
                {
                    return;
                }

                try
                {
                    sensorAdapter.StartSensorStream();
                    IsStreaming = true;

                    loggingCancellationTokenSource = new CancellationTokenSource();
                    StartLogging(loggingCancellationTokenSource.Token);
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
                    return;
                }

                if (!IsStreaming)
                {
                    return;
                }
                try
                {
                    sensorAdapter.StopSensorStream();
                    StopLogging();
                    loggingCancellationTokenSource?.Cancel();
                    IsStreaming = false;                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to stop data acquisition for {Name} on {path}: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// Initiates calibration procedure for the device.
        /// </summary>
        /// <param name="isLeftSock">True if calibrating a left sock, false otherwise.</param>
        public bool Calibrate(bool isLeftSock)
        {
            try
            {
                bool success = sensorAdapter.Calibrate(isLeftSock);
                if (success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Device {Name}]: Error during calibration: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Starts asynchronous logging of sensor data into a CSV log file.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to terminate logging.</param>
private void StartLogging(CancellationToken cancellationToken)
{ 
    string logFilePath;
        string activeLogFile = IsLeftSock ? "ActiveLogFile_Left.txt" : "ActiveLogFile_Right.txt";

    try
    {
        using (FileStream fs = new FileStream(activeLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (StreamReader reader = new StreamReader(fs))
        {
            logFilePath = reader.ReadLine()?.Trim();
        }
        if (string.IsNullOrWhiteSpace(logFilePath) || !File.Exists(logFilePath))
        {
            Console.WriteLine("Error: Log file path is missing or invalid.");
            return;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error reading log file: {ex.Message}");
        return;
    }

    DateTime? lastTimestamp = null;
    var logBuffer = new StringBuilder();
    try
    {
        using (FileStream fs = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        using (StreamWriter writer = new StreamWriter(fs))
        {
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\tData receival has resumed.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error writing restart message: {ex.Message}");
        return;
    }

    isLogging = true;

    Task.Run(async () =>
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (logQueue.TryDequeue(out string logEntry))
                {
                    string[] parts = logEntry.Split(',');
                    if (parts.Length < 2) continue;

                    if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime currentTimestamp))
                        continue;

                    byte[] rawData;
                    try
                    {
                        rawData = parts[1].Split('-').Select(hex => Convert.ToByte(hex, 16)).ToArray();
                    }
                    catch
                    {
                        continue;
                    }

                    if (rawData.Length < 47 || rawData[0] != 0xF0 || rawData[46] != 0x55) continue;

                    try
                    {
                        byte battery = rawData[2];
                        uint timeMs = BitConverter.ToUInt32(rawData, 3);
                        uint q0 = BitConverter.ToUInt32(rawData, 7);
                        uint q1 = BitConverter.ToUInt32(rawData, 11);
                        uint q2 = BitConverter.ToUInt32(rawData, 15);
                        uint q3 = BitConverter.ToUInt32(rawData, 19);
                        short accX = BitConverter.ToInt16(rawData, 23);
                        short accY = BitConverter.ToInt16(rawData, 25);
                        short accZ = BitConverter.ToInt16(rawData, 27);
                        short sensor1 = BitConverter.ToInt16(rawData, 35);
                        short sensor2 = BitConverter.ToInt16(rawData, 33);
                        short sensor3 = BitConverter.ToInt16(rawData, 31);
                        short sensor4 = BitConverter.ToInt16(rawData, 37);
                        short sensor5 = BitConverter.ToInt16(rawData, 39);
                        short sensor6 = BitConverter.ToInt16(rawData, 29);
                        short sensor7 = BitConverter.ToInt16(rawData, 41);
                        short sensor8 = BitConverter.ToInt16(rawData, 43);

                        logBuffer.AppendLine($"{currentTimestamp:yyyy-MM-dd HH:mm:ss}\t{battery}\t{timeMs}\t{q0}\t{q1}\t{q2}\t{q3}\t{accX}\t{accY}\t{accZ}\t{sensor1}\t{sensor2}\t{sensor3}\t{sensor4}\t{sensor5}\t{sensor6}\t{sensor7}\t{sensor8}");
                        lastTimestamp = currentTimestamp;
                    }
                    catch
                    {
                        continue;
                    }
                }
                if (logBuffer.Length > 0)
                {
                    try
                    {
                        using (FileStream fs = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        using (StreamWriter writer = new StreamWriter(fs))
                        {
                            writer.Write(logBuffer.ToString());
                        }
                        logBuffer.Clear();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error writing to log file: {ex.Message}");
                    }
                }

                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Device {Name}]: Error in logging task: {ex.Message}");
        }
    }, cancellationToken);
}


 private void StopLogging()
{
    if (!isLogging) return;
    isLogging = false;

    string stopMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\tData receival has been stopped.\n";

    try
    {
        File.AppendAllText(logFilePath, stopMessage);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Device {Name}]: Error writing stop message to log file: {ex.Message}");
    }
}

        private void OnRawDataReceived(object sender, string rawData)
        {
            if (!IsStreaming) return;

            string timestamp = DateTime.Now.ToString("o", CultureInfo.InvariantCulture); 
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
        /// <summary>
        /// Retrieves the latest sensor pressure values.
        /// </summary>
        /// <returns>An array of sensor pressure values.</returns>
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
