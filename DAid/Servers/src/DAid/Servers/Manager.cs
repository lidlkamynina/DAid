using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace DAid.Servers
{
    /// <summary>
    /// Manages communication and operations for all devices in the system.
    /// </summary>
    public class Manager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, Device> devices = new Dictionary<string, Device>();
        private readonly object syncLock = new object();

        /// <summary>
        /// Scans for available devices and adds them to the internal devices list.
        /// </summary>
        /// <param name="path">Optional path for device discovery.</param>
        public void Scan(string path = "")
        {
            logger.Info("Scanning for devices...");

            try
            {
                var discoveredDevices = DiscoverDevices(path);

                lock (syncLock)
                {
                    foreach (var device in discoveredDevices)
                    {
                        if (!devices.ContainsKey(device.Path))
                        {
                            devices[device.Path] = device;
                            try
                            {
                                device.Connect();
                                logger.Info($"Discovered and connected: {device.Name} on {device.Path}");
                            }
                            catch (Exception ex)
                            {
                                logger.Warn($"Failed to connect to device: {device.Name} on {device.Path}: {ex.Message}");
                            }
                        }
                    }
                }

                logger.Info("Device scan completed.");
            }
            catch (Exception ex)
            {
                logger.Error($"Error during device scan: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves a device by its path.
        /// </summary>
        /// <param name="path">The device path.</param>
        /// <returns>The device if found; otherwise, null.</returns>
        public Device Get(string path)
        {
            lock (syncLock)
            {
                if (devices.TryGetValue(path, out var device))
                {
                    if (!device.IsConnected)
                    {
                        logger.Info($"Reconnecting to device: {device.Name} on {device.Path}");
                        try
                        {
                            device.Connect();
                        }
                        catch (Exception ex)
                        {
                            logger.Warn($"Failed to reconnect to device: {device.Name} on {device.Path}: {ex.Message}");
                        }
                    }

                    return device;
                }
            }

            logger.Warn($"Device not found on path: {path}");
            return null;
        }

        /// <summary>
        /// Returns all registered devices.
        /// </summary>
        /// <returns>An enumerable collection of all devices.</returns>
        public IEnumerable<Device> GetAllDevices()
        {
            lock (syncLock)
            {
                return devices.Values.ToList();
            }
        }

        /// <summary>
        /// Cleans up and disconnects all devices.
        /// </summary>
        public void Cleanup()
        {
            logger.Info("Cleaning up devices...");

            lock (syncLock)
            {
                foreach (var device in devices.Values)
                {
                    try
                    {
                        logger.Info($"Disconnecting device: {device.Name} on {device.Path}");
                        device.Stop();
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Failed to disconnect device: {device.Name} on {device.Path}: {ex.Message}");
                    }
                }

                devices.Clear();
            }
        }

        /// <summary>
        /// Simulates device discovery.
        /// </summary>
        /// <param name="path">Optional discovery path.</param>
        /// <returns>A list of discovered devices.</returns>
        private IEnumerable<Device> DiscoverDevices(string path)
        {
            logger.Info($"Simulating device discovery with path: {path}");

            // Simulate discovering devices
            return new List<Device>
            {
                new Device("COM3", 9600, "SensorDevice1"),
                new Device("COM4", 115200, "SensorDevice2")
            };
        }
    }
}
