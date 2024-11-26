using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using NLog;

namespace DAid.Servers
{
    public class SensorAdapter
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private SerialPort serialPort;
        private const byte StartByte = 0xF0;
        private const byte StopByte = 0x55;
        private const int PacketLength = 47;
        private readonly byte[] buffer = new byte[1024];
        private int bufferPos = 0;
        private const string CsvFilePath = "output.csv";

        private readonly string[] SensorNames = { "RS1", "RS2", "RS3", "RS4", "RS5", "RS6" };
        private double[] sensorValues = new double[6];
        private double[] xCoordinates = new double[6];
        private double[] yCoordinates = new double[6];

        private bool isCalibrated = false;

        // Sensor Positions
        private readonly double[] sensorXPositions = { 0.3, -0.3, 0.3, -0.3, 0.3, -0.3 };
        private readonly double[] sensorYPositions = { 1, 1, 0, 0, -1, -1 };

        private double copX = 0;
        private double copY = 0;

        // Configurable Multiplier
        public double ResistanceMultiplier { get; set; } = 1.0;

        // DAid Commands
        private const string StartStreamCommand = "BT^START\r";
        private const string StopStreamCommand = "BT^STOP\r";

        // Lock object for thread-safety
        private readonly object syncLock = new object();

        /// <summary>
        /// Event triggered when raw data is received from the sensor.
        /// </summary>
        public event EventHandler<string> RawDataReceived;

        /// <summary>
        /// Initializes the SensorAdapter with the given COM port and baud rate.
        /// </summary>
        public void Initialize(string comPort, int baudRate)
        {
            try
            {
                InitializeSerialPort(comPort, baudRate);
                serialPort.Open();
                logger.Info($"Connected to {comPort}.");
            }
            catch (Exception ex)
            {
                logger.Error($"Error initializing Sensor Adapter: {ex.Message}");
            }
        }
        public static List<string> ScanPorts()
{
    try
    {
        return SerialPort.GetPortNames().ToList();
    }
    catch (Exception ex)
    {
        logger.Error($"Error scanning ports: {ex.Message}");
        return new List<string>();
    }
}

        private void InitializeSerialPort(string comPort, int baudRate)
        {
            serialPort = new SerialPort(comPort, baudRate)
            {
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 3000,
                WriteTimeout = 3000
            };
            serialPort.DataReceived += DataReceivedHandler;
        }

        /// <summary>
        /// Starts the data stream from the sensor.
        /// </summary>
        public void StartSensorStream()
        {
            if (serialPort?.IsOpen == true)
            {
                serialPort.WriteLine(StartStreamCommand);
                logger.Info("Sensor data stream started.");
            }
            else
            {
                logger.Warn("Cannot start sensor stream; serial port is not open.");
            }
        }

        public bool Calibrate()
{
    try
    {
        // Add calibration logic here
        logger.Info("Calibrating sensor...");
        // Simulate calibration success
        return true;
    }
    catch (Exception ex)
    {
        logger.Error($"Calibration failed: {ex.Message}");
        return false;
    }
}


        /// <summary>
        /// Stops the data stream from the sensor.
        /// </summary>
        public void StopSensorStream()
        {
            if (serialPort?.IsOpen == true)
            {
                serialPort.WriteLine(StopStreamCommand);
                logger.Info("Sensor data stream stopped.");
            }
            else
            {
                logger.Warn("Cannot stop sensor stream; serial port is not open.");
            }
        }

        /// <summary>
        /// Cleans up the resources by closing the serial port.
        /// </summary>
        public void Cleanup()
        {
            if (serialPort?.IsOpen == true)
            {
                try
                {
                    serialPort.Close();
                    logger.Info("Serial port closed.");
                }
                catch (Exception ex)
                {
                    logger.Error($"Error closing serial port: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles data received from the serial port.
        /// </summary>
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (serialPort == null) return;

            try
            {
                int bytesToRead = serialPort.BytesToRead;
                byte[] incomingData = new byte[bytesToRead];
                serialPort.Read(incomingData, 0, bytesToRead);
                ProcessIncomingData(incomingData);
            }
            catch (Exception ex)
            {
                logger.Error($"Error during data reception: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes incoming data and handles complete packets.
        /// </summary>
        private void ProcessIncomingData(byte[] incomingData)
        {
            lock (syncLock)
            {
                Array.Copy(incomingData, 0, buffer, bufferPos, incomingData.Length);
                bufferPos += incomingData.Length;

                while (bufferPos >= PacketLength)
                {
                    int startIndex = Array.IndexOf(buffer, StartByte, 0, bufferPos);
                    if (startIndex == -1) break;

                    if (startIndex + PacketLength <= bufferPos)
                    {
                        byte[] packet = new byte[PacketLength];
                        Array.Copy(buffer, startIndex, packet, 0, PacketLength);

                        if (ValidatePacket(packet))
                        {
                            HandleValidPacket(packet);
                        }

                        bufferPos -= (startIndex + PacketLength);
                        Array.Copy(buffer, startIndex + PacketLength, buffer, 0, bufferPos);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Validates a packet for checksum and stop byte.
        /// </summary>
        private bool ValidatePacket(byte[] packet)
        {
            byte calculatedChecksum = CalculateChecksum(packet, PacketLength - 2);
            byte receivedChecksum = packet[PacketLength - 2];
            return calculatedChecksum == receivedChecksum && packet[PacketLength - 1] == StopByte;
        }

        /// <summary>
        /// Handles valid packets: extracts values, performs calibration, and calculates COP.
        /// </summary>
        private void HandleValidPacket(byte[] packet)
        {
            ExtractSensorValues(packet);

            if (!isCalibrated)
            {
                PerformCalibration();
            }
            else
            {
                NormalizeSensorValues();
                CalculateCoordinates();
                CalculateCOP();
                WriteToCsv();
            }

            // Trigger the RawDataReceived event with the extracted sensor data
            string rawData = string.Join(",", sensorValues.Select(v => v.ToString("F2")));
            RawDataReceived?.Invoke(this, rawData);
        }

        private void ExtractSensorValues(byte[] packet)
        {
            int[] mappedPositions = { 33, 35, 37, 39, 41, 43 };

            for (int i = 0; i < SensorNames.Length; i++)
            {
                int startPos = mappedPositions[i];
                if (startPos + 1 < PacketLength)
                {
                    int rawValue = (packet[startPos] << 8) | packet[startPos + 1];
                    sensorValues[i] = rawValue * ResistanceMultiplier;
                    logger.Debug($"Extracted {SensorNames[i]}: {sensorValues[i]}");
                }
            }
        }

        private void PerformCalibration()
        {
            isCalibrated = true;
            logger.Info("Calibration completed.");
        }

        private void NormalizeSensorValues()
        {
            double maxSensorValue = sensorValues.Max();
            if (maxSensorValue > 0)
            {
                for (int i = 0; i < SensorNames.Length; i++)
                {
                    sensorValues[i] /= maxSensorValue;
                }
            }
            else
            {
                logger.Warn("Max sensor value is zero; skipping normalization.");
            }
        }

        private void CalculateCoordinates()
        {
            double totalNormalized = sensorValues.Sum();
            for (int i = 0; i < SensorNames.Length; i++)
            {
                xCoordinates[i] = (sensorValues[i] / totalNormalized) * sensorXPositions[i];
                yCoordinates[i] = (sensorValues[i] / totalNormalized) * sensorYPositions[i];
            }
        }

        private void CalculateCOP()
        {
            double totalNormalized = sensorValues.Sum();
            if (totalNormalized > 0)
            {
                copX = sensorValues.Zip(sensorXPositions, (value, xPos) => value * xPos).Sum() / totalNormalized;
                copY = sensorValues.Zip(sensorYPositions, (value, yPos) => value * yPos).Sum() / totalNormalized;
                logger.Info($"Calculated COP: X = {copX:F2}, Y = {copY:F2}");
            }
            else
            {
                logger.Warn("Total normalized sensor value is zero; COP not calculated.");
            }
        }

        private void WriteToCsv()
        {
            try
            {
                string csvLine = string.Join(",", SensorNames.Zip(sensorValues, (name, value) => $"{name}:{value:F2}"))
                                 + $", COP_X: {copX:F2}, COP_Y: {copY:F2}";
                File.AppendAllLines(CsvFilePath, new[] { csvLine });
                logger.Info($"Data written to CSV: {csvLine}");
            }
            catch (Exception ex)
            {
                logger.Error($"Error writing to CSV: {ex.Message}");
            }
        }

        private byte CalculateChecksum(byte[] data, int length)
        {
            return (byte)data.Take(length).Sum(b => b);
        }
    }
}
