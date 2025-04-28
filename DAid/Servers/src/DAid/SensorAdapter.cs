using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

public class SensorAdapter
{
    private SerialPort serialPort;
    private const byte StartByte = 0xF0;
    private const byte StopByte = 0x55;
    private const int PacketLength = 47;
    private const int MedianWindowSize = 10; //this?
    private readonly Queue<double>[] pressureHistories = new Queue<double>[4];
    private readonly byte[] buffer = new byte[2048]; 
    private int bufferPos = 0;
    private double[] calibrationOffsets = new double[4];

    private double[] calibrationOffsetsLeft = new double[4];
    private double[] calibrationOffsetsRight = new double[4];
    private bool isCalibrated = false;

    private string moduleName = "Unknown";
    public string ModuleName => moduleName; 

    private const int DefaultBaudRate = 92600;
    private readonly int[] RightSensorPositions = { 30, 32, 38, 40 }; 
    private readonly int[] LeftSensorPositions = {  32, 30, 40, 38 }; 
    private int[] SensorPositions;    
    private readonly double[] XPositions = { 2.0, -2.0, 2.0, -2.0 }; //for left
    private readonly double[] YPositions = { 4.0, 4.0, -4.0, -4.0 };
    private double[] sensorResistance = new double[4];
    private double[] sensorPressures = new double[3]; // 3 combined pressures
    private double[] rawSensorPressures = new double[4]; // for debug window
    private const int FilterWindowSize = 16;
    private bool isStreaming = false;
    private readonly object syncLock = new object();
    public string DeviceId { get; } 
    private bool isLeftSock; //checks for isleftsock based of module number
    private double[] sensorOffsets = new double[4];

    public bool moduleNameRetrieved = false;
    private PressureDebugWindow _pressureDebugWindow;
    private Thread _debugWindowThread;
    private DateTime lastCoPUpdate = DateTime.MinValue;
    private readonly TimeSpan CoPUpdateInterval = TimeSpan.FromMilliseconds(200);
    public event EventHandler<string> ModuleNameRetrieved; 
    
    public event EventHandler<(string ModuleName, bool IsLeftSock)> ModuleInfoUpdated; //this??

    public event EventHandler<string> RawDataReceived;
    public event EventHandler<(double CoPX, double CoPY, double[] Pressures)> CoPUpdated;

      public SensorAdapter(string deviceId)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        SensorPositions = RightSensorPositions;

        for (int i = 0; i < pressureHistories.Length; i++)
            pressureHistories[i] = new Queue<double>(FilterWindowSize); //maybe in datareceivedhandler?
    }
    public void Initialize(string comPort, int baudRate = DefaultBaudRate)
    {
            if (serialPort != null && serialPort.IsOpen)
            {
                Console.WriteLine($"[SensorAdapter {DeviceId}]: Serial port already initialized.");
                return;
            }
        try
        {
            serialPort = new SerialPort(comPort, baudRate)
            {
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 5000,
                WriteTimeout = 5000
            };

            ConfigureBTS1("8");
            ConfigureBTS234("1");
            ConfigureBTS8("*,2");

            serialPort.DataReceived += DataReceivedHandler;
            serialPort.Open();
            Console.WriteLine($"[SensorAdapter]: Initialized on {comPort} at {baudRate} baud.");
           // StartSensorStream(); this??
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SensorAdapter]: Error initializing on {comPort}: {ex.Message}");
            throw;
        }
    }
     public static List<string> ScanPorts()
    {
        try
        {
            var ports = SerialPort.GetPortNames().ToList();
            return ports;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SensorAdapter]: Error scanning ports: {ex.Message}");
            return new List<string>();
        }
    }
    public void ConfigureBTS8(string config) => SendCommand($"BTS8={config}");
    public void ConfigureBTS1(string config) => SendCommand($"BTS1={config}");
    public void ConfigureBTS234(string config) {
        SendCommand($"BTS2={config}");
        SendCommand($"BTS3={config}");
        SendCommand($"BTS4={config}");
        }

        private void SendCommand(string command)
    {
          lock (syncLock)
        {
            try
            {
                if (serialPort?.IsOpen == true)
                {
                    serialPort.WriteLine(command + "\r");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SensorAdapter {DeviceId}]: Error sending command '{command}': {ex.Message}");
            }
        }
    }



    public void StartSensorStream()
    {
        lock (syncLock)
        {
            if (serialPort?.IsOpen == true && !isStreaming)
            {
                SendCommand("BT^START");
                isStreaming = true;

                if (_pressureDebugWindow == null || _pressureDebugWindow.IsDisposed)
                {
                    _debugWindowThread = new Thread(() =>
                    {
                        _pressureDebugWindow = new PressureDebugWindow();
                        _pressureDebugWindow.Text = $"Pressure Debug - {(isLeftSock ? "Left" : "Right")}";
                        Application.Run(_pressureDebugWindow);
                    });
                    _debugWindowThread.SetApartmentState(ApartmentState.STA);
                    _debugWindowThread.IsBackground = true;
                    _debugWindowThread.Start();
                }
            }
        }
    }

    public void StopSensorStream()
    {
        lock (syncLock)
        {
            if (serialPort?.IsOpen == true && isStreaming)
            {
                SendCommand("BT^STOP");
                isStreaming = false;
                Console.WriteLine("[SensorAdapter]: Data stream stopped.");
            }
        }
    }

    private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
{
    try
    {
        int bytesToRead = serialPort.BytesToRead;
        byte[] incomingData = new byte[bytesToRead];
        serialPort.Read(incomingData, 0, bytesToRead);
        
        RawDataReceived?.Invoke(this, BitConverter.ToString(incomingData));
        ProcessIncomingData(incomingData);

        if (!moduleNameRetrieved)
        {
            RetrieveModuleName();

            if (moduleName != "Unknown") 
            {
                moduleNameRetrieved = true;
                ModuleNameRetrieved?.Invoke(this, moduleName); // Notify listeners
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SensorAdapter]: Error receiving data: {ex.Message}");
    }
}
public void RetrieveModuleName()
{
    if (moduleNameRetrieved) return;

    lock (syncLock)
    {
        try
        {
            serialPort.DiscardInBuffer();
            SendCommand("BTS6?");
            Thread.Sleep(1000);

            int bytesToRead = serialPort.BytesToRead;
            if (bytesToRead > 0)
            {
                byte[] incomingData = new byte[bytesToRead];
                serialPort.Read(incomingData, 0, bytesToRead);

                string response = Encoding.ASCII.GetString(incomingData).Trim();
                foreach (var line in response.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.IndexOf("Register 6 value:", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        int index = line.IndexOf(':');
                        if (index != -1 && index + 1 < line.Length)
                        {
                            string moduleValue = line.Substring(index + 1).Trim();
                            if (int.TryParse(moduleValue, out int moduleNumber))
                            {
                                moduleName = moduleValue;
                                moduleNameRetrieved = true;

                                bool isLeftSock = moduleNumber % 2 != 0;
                                SensorPositions = isLeftSock ? LeftSensorPositions : RightSensorPositions;
                                ModuleInfoUpdated?.Invoke(this, (moduleName, isLeftSock));
                                return;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SensorAdapter]: Error retrieving module name: {ex.Message}");
        }
    }
}


 private void CheckLeftOrRight(int moduleNumber)
{
    if (moduleNumber % 2 == 0)
    {
        SensorPositions = RightSensorPositions; 
    }
    else
    {
        SensorPositions = LeftSensorPositions; // Reverse sensor order for left sock
    }
}


    private void ProcessIncomingData(byte[] incomingData)
    {
        lock (syncLock)
        {
            if (incomingData.Length + bufferPos > buffer.Length)
            {
                Console.WriteLine("[SensorAdapter]: Incoming data exceeds buffer size.");
                return;
            }

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
                    //Console.WriteLine($"[SensorAdapter]: Extracted Packet (HEX): {BitConverter.ToString(packet).Replace("-", " ")}");

                    if (ValidatePacket(packet)) 
                    {
                        ExtractSensorValues(packet);
                        CalculateAndNotifyCoP();
                    }

                    bufferPos -= startIndex + PacketLength;
                    Array.Copy(buffer, startIndex + PacketLength, buffer, 0, bufferPos);
                }
                else break;
            }
        }
    }

private void ExtractSensorValues(byte[] packet)
{
    lock (syncLock)
    {
        double[] rawSensorValues = new double[4];
        //double[] rawSensorValues = new double[sensorResistance.Length]; 

        for (int i = 0; i < SensorPositions.Length; i++)
        {
            int pos = SensorPositions[i];
            int rawValue = (packet[pos] << 8) | packet[pos + 1];
            rawSensorValues[i] = rawValue > 0 ? (1000.0 / rawValue) : 0.0;
       }
        //var medianFiltered = ApplyRollingMedian(rawSensorValues);
        //sensorResistance = MovingAverageFilter(medianFiltered, 4);
        for (int i = 0; i < 4; i++)
    {//calibration offsets!!! should they be divided for left and right?
        double adjustedValue = isCalibrated ? (rawSensorValues[i] - calibrationOffsets[Math.Min(i, 2)]) : rawSensorValues[i]; 
        adjustedValue = Math.Max(adjustedValue, 0.0);

        pressureHistories[i].Enqueue(adjustedValue);
        if (pressureHistories[i].Count > FilterWindowSize)
            pressureHistories[i].Dequeue();
    }

    double frontRight = pressureHistories[0].Average();
    double frontLeft = pressureHistories[1].Average();
    double rearRight = MedianFilter(pressureHistories[2]);
    double rearLeft = MedianFilter(pressureHistories[3]);

    // Weighted average for rear
    double rearTotal = rearRight + rearLeft;
    double rearWeighted = rearTotal > 0.00001
    ? ((rearRight * rearRight) + (rearLeft * rearLeft)) / rearTotal
    : 0.0;

    sensorPressures[0] = frontRight;
    sensorPressures[1] = frontLeft;
    sensorPressures[2] = rearWeighted;
    //sensorPressures[2] = (rearRight + rearLeft) / 2;

    for (int i = 0; i < 4; i++)
        rawSensorPressures[i] = rawSensorValues[i];
}

    }

public double[] GetSensorPressures()
{
    lock (syncLock)
    {
        return (double[])sensorResistance.Clone();
    }
}


public bool Calibrate(bool isLeftSock)
{
    int seconds = 10;
    double[] sampleSum = new double[4]; //sum of samples
    //double maxPressure = double.MinValue, minPressure = double.MaxValue;
    //double totalX = 0, totalY = 0;
    int sampleCount = 0;
    DateTime startTime = DateTime.Now;

    Console.WriteLine("[Calibration]: Stand with both feet. Lift each foot one at a time after 1 second.");

    while ((DateTime.Now - startTime).TotalSeconds < seconds)
    {
        lock (syncLock)
        {
            for (int i = 0; i < rawSensorPressures.Length; i++)
                {
                    Console.Write($"S{i + 1}: {rawSensorPressures[i]:F4} | ");
                    sampleSum[i] += rawSensorPressures[i];
                }
                sampleCount++;
                Thread.Sleep(50);
            }
            lock (syncLock)
        {
            for (int i = 0; i < calibrationOffsets.Length; i++)
                calibrationOffsets[i] = sampleSum[i] / sampleCount;

            isCalibrated = true;
        }
    }
    if (sampleCount == 0)
    {
        Console.WriteLine("[Calibration]: Calibration failed. Invalid pressure range.");
        return false;
    }
    if (isLeftSock)
    {
        calibrationOffsetsLeft = calibrationOffsets;
        //Console.WriteLine($"[Calibration]: Left Foot Offset X: {calibrationOffsetsLeft.x0}, Y: {calibrationOffsetsLeft.y0}");
    }
    else
    {
        calibrationOffsetsRight = calibrationOffsets;
       // Console.WriteLine($"[Calibration]: Right Foot Offset X: {calibrationOffsetsRight.x0}, Y: {calibrationOffsetsRight.y0}");
    }
    Console.WriteLine($"[Calibration]: Completed.");
    return true;
}

private double MedianFilter(Queue<double> history)
    {
        var arraySorted = history.OrderBy(x => x).ToList();
        int count = arraySorted.Count;
        return (count % 2 == 1)
            ? arraySorted[count / 2]
            : (arraySorted[(count - 1) / 2] + arraySorted[count / 2]) / 2.0;
    }


private void CalculateAndNotifyCoP()
{
    lock (syncLock)
    {
        if ((DateTime.Now - lastCoPUpdate) < CoPUpdateInterval)
                return;

        double[] cleanPressures = new double[3];
        for (int i = 0; i < 3; i++)
        cleanPressures[i] = (sensorPressures[i] < 0.0005) ? 0.0 : sensorPressures[i];
        double[] adjustedXPositions = SensorPositions == RightSensorPositions 
            ? XPositions.Select(x =>-x).ToArray()  
            : XPositions;

        double totalPressure = sensorPressures.Sum(); 

            if (totalPressure <= 0.00001)
            {
                Task.Run(() => CoPUpdated?.Invoke(this, (0.0, 0.0, cleanPressures)));
                Console.WriteLine("[CoP]: No valid pressure detected, CoP remains at (0,0).");

                return;
            }
            double copX = 0.0, copY = 0.0;

            for (int i = 0; i < sensorPressures.Length; i++)
            {
                copX += cleanPressures[i] * adjustedXPositions[i];
                copY += cleanPressures[i] * YPositions[i];
            }

            copX /= totalPressure;
            copY /= totalPressure;
            lastCoPUpdate = DateTime.Now;

            _pressureDebugWindow?.UpdatePressures(
                isLeftSock ? rawSensorPressures : Array.Empty<double>(),
                !isLeftSock ? rawSensorPressures : Array.Empty<double>());

            Task.Run(() => CoPUpdated?.Invoke(this, (copX, copY, (double[])cleanPressures.Clone())));
        }
}
    private bool ValidatePacket(byte[] packet)
    {
        if (packet.Length < PacketLength) return false;
        byte calculatedChecksum = CalculateChecksum(packet, PacketLength - 2);
        return calculatedChecksum == packet[PacketLength - 2] && packet[PacketLength - 1] == StopByte;
    }

    private byte CalculateChecksum(byte[] data, int length)
    {
        int sum = data.Take(length).Sum(b => b);
        return (byte)(sum & 0xFF);
    }
    public void Cleanup()
    {
        lock (syncLock)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    StopSensorStream();
                    serialPort.Close();
                    Console.WriteLine($"[SensorAdapter {DeviceId}]: Serial port closed.");

                    if (_pressureDebugWindow != null && !_pressureDebugWindow.IsDisposed)
                    {
                        _pressureDebugWindow.Invoke(new Action(() => _pressureDebugWindow.Close()));
                        _pressureDebugWindow = null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SensorAdapter {DeviceId}]: Error closing serial port: {ex.Message}");
                }
            }
        }
    }
}