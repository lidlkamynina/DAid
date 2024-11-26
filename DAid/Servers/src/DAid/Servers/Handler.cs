using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using NLog;

namespace DAid.Servers
{
    /// <summary>
    /// Manages communication between connected clients and DAid sensors.
    /// </summary>
    public sealed class Handler
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Server server;
        private readonly MemoryStream stream;
        private readonly CancellationToken token;

        private readonly Dictionary<Device, Cache> devices = new Dictionary<Device, Cache>();
        private readonly object syncLock = new object();

        /// <summary>
        /// Represents a buffer cache for a specific device.
        /// </summary>
        private sealed class Cache
        {
            public readonly byte[] offsets;
            public readonly byte[] buffer;

            public Cache(byte index, byte[] offsets)
            {
                this.offsets = offsets;
                this.buffer = new byte[offsets.Sum(offset => offset) + 5];
                this.buffer[0] = index;
            }
        }

        public Handler(Server server, MemoryStream stream, CancellationToken token)
        {
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.token = token;
        }

        /// <summary>
        /// Initiates communication and negotiates requested devices.
        /// </summary>
        public async Task Start()
        {
            try
            {
                logger.Info("Handler started.");

                // Read the size of the message
                byte[] sizeBuffer = new byte[1];
                await stream.ReadAsync(sizeBuffer, 0, 1, token);
                int messageSize = sizeBuffer[0];

                // Read the device request message
                byte[] buffer = new byte[messageSize];
                await stream.ReadAsync(buffer, 0, messageSize, token);
                string[] requestedPaths = Encoding.ASCII.GetString(buffer)
                    .Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

                // Retrieve and register devices
                IEnumerable<Device> requestedDevices = GetRequestedDevices(requestedPaths);
                RegisterDevices(requestedDevices);

                // Prepare and send the response buffer
                byte[] response = PrepareResponseBuffer(requestedDevices);
                await stream.WriteAsync(response, 0, response.Length, token);

                // Subscribe to device events
                foreach (Device device in devices.Keys)
                {
                    device.RawDataReceived += OnRawDataReceived;
                }

                logger.Info("Handler setup complete.");
            }
            catch (Exception ex)
            {
                logger.Error($"Handler failed to start: {ex.Message}");
                Stop();
            }
        }

        /// <summary>
        /// Stops the handler and disconnects clients.
        /// </summary>
        public void Stop()
        {
            lock (syncLock)
            {
                try
                {
                    logger.Info("Stopping handler.");
                }
                catch
                {
                    // Ignored
                }

                foreach (var device in devices.Keys)
                {
                    device.RawDataReceived -= OnRawDataReceived;
                }

                devices.Clear();
            }
        }

        /// <summary>
        /// Handles raw data received from devices.
        /// </summary>
        private void OnRawDataReceived(object sender, string rawData)
        {
            try
            {
                if (sender is Device device && devices.TryGetValue(device, out Cache cache))
                {
                    byte[] data = Encoding.ASCII.GetBytes(rawData);
                    Array.Copy(data, 0, cache.buffer, 1, data.Length);

                    lock (syncLock)
                    {
                        stream.Write(cache.buffer, 0, cache.buffer.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"Error during data handling: {ex.Message}");
                Stop();
            }
        }

        /// <summary>
        /// Prepares the response buffer with device information.
        /// </summary>
        private byte[] PrepareResponseBuffer(IEnumerable<Device> devices)
        {
            var response = new List<byte>();
            foreach (var device in devices)
            {
                response.AddRange(Encoding.ASCII.GetBytes(device.Path));
                response.Add(0); // Null terminator
                response.AddRange(BitConverter.GetBytes(device.Frequency));
            }
            return response.ToArray();
        }

        /// <summary>
        /// Retrieves the requested devices from the server's manager.
        /// </summary>
        private IEnumerable<Device> GetRequestedDevices(string[] paths)
        {
            if (paths.Length == 0)
            {
                logger.Info("Requesting all available devices.");
                server.Manager.Scan("");
                return server.Manager.GetAllDevices();
            }

            logger.Info($"Requesting devices for paths: {string.Join(", ", paths)}");
            return paths.Select(path => server.Manager.Get(path)).Where(device => device != null);
        }

        /// <summary>
        /// Registers devices with internal caches and connects them.
        /// </summary>
        private void RegisterDevices(IEnumerable<Device> devicesToRegister)
        {
            byte deviceIndex = 0;

            foreach (var device in devicesToRegister)
            {
                if (!devices.ContainsKey(device))
                {
                    devices[device] = new Cache(deviceIndex++, new byte[0]); // Initialize cache
                    try
                    {
                        device.Connect();
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Failed to connect to device {device.Path}: {ex.Message}");
                    }
                }
            }
        }
    }
}
