using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace DAid.Servers
{
    public sealed class Device
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string path;
        private readonly float frequency;
        private SensorAdapter sensorAdapter;

        private readonly object _syncLock = new object();
        private CancellationTokenSource cancellationTokenSource;

        public event EventHandler<string> RawDataReceived;

        public bool IsConnected { get; private set; }
        public string Path => path;
        public float Frequency => frequency;
        public string Name { get; private set; }

        public Device(string path, float frequency, string name)
        {
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            this.frequency = frequency;
            this.Name = name ?? throw new ArgumentNullException(nameof(name));

            InitializeSensorAdapter();
        }

        private void InitializeSensorAdapter()
        {
            sensorAdapter = new SensorAdapter();

            // Subscribe to RawDataReceived
            sensorAdapter.RawDataReceived += OnRawDataReceived;

            // Initialize sensor
            sensorAdapter.Initialize(path, (int)frequency);
        }

        public void Connect()
        {
            lock (_syncLock)
            {
                if (IsConnected)
                {
                    logger.Info($"Device on {path} is already connected.");
                    return;
                }

                logger.Info($"Connecting to device on {path}...");
                try
                {
                    if (sensorAdapter == null)
                        InitializeSensorAdapter();

                    sensorAdapter.StartSensorStream();
                    IsConnected = true;
                    logger.Info($"Device successfully connected on {path}.");
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
                    logger.Warn($"Cannot start acquisition. Device on {path} is not connected.");
                    return;
                }

                logger.Info($"Starting data acquisition for device on {path}...");
                cancellationTokenSource = new CancellationTokenSource();
            }
        }

        public void Stop()
        {
            lock (_syncLock)
            {
                try
                {
                    if (!IsConnected)
                    {
                        logger.Warn($"Cannot stop. Device on {path} is not connected.");
                        return;
                    }

                    logger.Info($"Stopping device on {path}...");
                    cancellationTokenSource?.Cancel();
                    sensorAdapter.StopSensorStream();
                    IsConnected = false;
                    logger.Info($"Device stopped on {path}.");
                }
                catch (Exception ex)
                {
                    logger.Error($"Error stopping device on {path}: {ex.Message}");
                }
            }
        }

        private void OnRawDataReceived(object sender, string rawData)
        {
            Task.Run(() => RawDataReceived?.Invoke(this, rawData));
        }
    }
}
