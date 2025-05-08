using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class HMDDataReceiver : MonoBehaviour
{
    public static HMDDataReceiver Instance { get; private set; }
    private bool restartInProgress = false;                        // flag for exercise restart
    private TcpListener _listener;
    private TcpClient _client;
    private NetworkStream _stream;
    private bool _isConnected = false;
    public bool IsClientConnected => _isConnected;

    [Header("Server Settings")]
    [Tooltip("Server IP Address (e.g., 127.0.0.1)")]
    public string serverIp = "127.0.0.1";
    [Tooltip("Server Port")]
    public int serverPort = 9003;

    [Header("Debug / Visual Objects")]
    [Tooltip("Debug Sphere for connection status")]
    public Transform debugSphere;
    [Tooltip("Left Foot object for zone updates")]
    public Transform leftFoot;
    [Tooltip("Right Foot object for zone updates")]
    public Transform rightFoot;

    private static readonly Queue<Action> mainThreadActions = new Queue<Action>();
    private string incomingBuffer = "";

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        StartServer();
    }

    void Update()
    {
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
                mainThreadActions.Dequeue().Invoke();
        }

        if (!_isConnected || _stream == null || !_stream.DataAvailable) return;

        try
        {
            byte[] buffer = new byte[1024];
            int bytesRead = _stream.Read(buffer, 0, buffer.Length);
            string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Debug.Log($"Saņemtais JSON: {receivedData}");
            LogToFile($"Received JSON chunk: {receivedData}");

            incomingBuffer += receivedData;
            while (true)
            {
                int startIndex = incomingBuffer.IndexOf("{");
                if (startIndex < 0)
                {
                    incomingBuffer = "";
                    break;
                }
                int braceCount = 0, endIndex = -1;
                for (int i = startIndex; i < incomingBuffer.Length; i++)
                {
                    if (incomingBuffer[i] == '{') braceCount++;
                    else if (incomingBuffer[i] == '}') braceCount--;
                    if (braceCount == 0) { endIndex = i; break; }
                }
                if (endIndex < 0) break;

                string completeJson = incomingBuffer.Substring(startIndex, endIndex - startIndex + 1);
                Debug.Log($"Processing complete JSON message: {completeJson}");
                LogToFile($"Processing JSON message: {completeJson}");
                ParseAndUpdateVisualization(completeJson);
                incomingBuffer = incomingBuffer.Substring(endIndex + 1);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Kļūda datu saņemšanā: {ex.Message}");
            LogToFile($"Error reading data: {ex.Message}");
        }
    }

    public static void RunOnMainThread(Action action)
    {
        if (action == null) return;
        lock (mainThreadActions)
            mainThreadActions.Enqueue(action);
    }

    private void LogToFile(string message)
    {
        string path = Application.persistentDataPath + "/server_log.txt";
        using (StreamWriter writer = new StreamWriter(path, true))
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
    }

    private void StartServer()
    {
        try
        {
            LogToFile("Server starting on port " + serverPort);
            _listener = new TcpListener(IPAddress.Any, serverPort);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Start();
            LogToFile("Server started on port " + serverPort);
            _listener.BeginAcceptTcpClient(OnClientConnect, _listener);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error starting the server: {ex.Message}");
            LogToFile($"Server start error: {ex.Message}");
        }
    }

    private void OnClientConnect(IAsyncResult result)
    {
        try
        {
            TcpClient newClient = _listener.EndAcceptTcpClient(result);
            if (_client != null)
            {
                Debug.LogWarning("Already connected to a client, disconnecting old connection.");
                LogToFile("Disconnecting old client");
                DisconnectFromServer();
            }
            _client = newClient;
            _stream = _client.GetStream();
            _isConnected = true;
            LogToFile("Client connected to HMD");

            RunOnMainThread(() =>
            {
                Debug.Log("Client connected to HMD.");
                if (debugSphere != null)
                {
                    var debugRenderer = debugSphere.GetComponent<Renderer>();
                    if (debugRenderer != null)
                        debugRenderer.material.color = Color.blue;
                }
            });

            _listener.BeginAcceptTcpClient(OnClientConnect, _listener);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error accepting client connection: {ex.Message}");
            LogToFile($"Accept client error: {ex.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        DisconnectFromServer();
        if (_listener != null)
            _listener.Stop();
    }

    private void DisconnectFromServer()
    {
        if (_stream != null) { _stream.Close(); _stream = null; }
        if (_client != null) { _client.Close(); _client = null; }
        _isConnected = false;
        Debug.Log("Atvienots no servera.");
        LogToFile("Disconnected from HMD client");
    }

    private void ParseAndUpdateVisualization(string jsonData)
    {
        var baseMsg = JsonUtility.FromJson<BaseMessage>(jsonData);
        if (baseMsg == null || string.IsNullOrEmpty(baseMsg.MessageType))
        {
            Debug.LogWarning("Nederīgi ziņojumi!");
            LogToFile("Invalid messageType in JSON");
            return;
        }
        switch (baseMsg.MessageType)
        {
            case "Feedback":
                var feedback = JsonUtility.FromJson<FeedbackMessage>(jsonData);
                UpdateFootFeedback(feedback);
                break;
            case "ExerciseConfig":
                var config = JsonUtility.FromJson<ExerciseConfigMessage>(jsonData);
                GameManager.Instance?.UpdateExerciseConfiguration(config);
                break;
            case "Command":
                var cmd = JsonUtility.FromJson<CommandData>(jsonData);
                ProcessCommand(cmd);
                break;
            default:
                Debug.LogWarning("Unknown MessageType: " + baseMsg.MessageType);
                LogToFile("Unknown MessageType: " + baseMsg.MessageType);
                break;
        }
    }

    private void UpdateFootFeedback(FeedbackMessage feedback)
    {
        Debug.Log($"Saņemts feedback: Foot={feedback.Foot}, Zone={feedback.Zone}");
        LogToFile($"Feedback received: Foot={feedback.Foot}, Zone={feedback.Zone}");

        if (feedback.Zone == 8)
        {
            Debug.Log("Zone 8 received: triggering preparation restart.");
            LogToFile("Zone 8 received: triggering preparation restart.");
            ProcessCommand(new CommandData { Command = "PREPARATION_RESTART" });
            return;
        }
        if (feedback.Zone == 7)
        {
            Debug.Log("Zone 7 received: triggering exercise restart.");
            LogToFile("Zone 7 received: triggering exercise restart.");
            ProcessCommand(new CommandData { Command = "RESTART_EXERCISE" });
            return;
        }

        if (restartInProgress)
        {
            Debug.Log("Restart in progress, ignoring feedback.");
            return;
        }

        string footKey = feedback.Foot.Equals("left", StringComparison.OrdinalIgnoreCase) ? "Left"
                        : feedback.Foot.Equals("right", StringComparison.OrdinalIgnoreCase) ? "Right"
                        : "Both";
        GameManager.Instance?.UpdateFootStatusForFoot(feedback.Zone, footKey);
    }

    private void ProcessCommand(CommandData cmdData)
    {
        Debug.Log($"Saņemta komanda: {cmdData.Command}");
        LogToFile($"ProcessCommand: {cmdData.Command}");
        switch (cmdData.Command)
        {
            case "PREPARATION_SUCCESS":
                GameManager.Instance?.MarkPreparationSuccessful();
                break;
            case "PREPARATION_RESTART":
                GameManager.Instance?.RequestPreparationRestart();
                break;
            case "RESTART_EXERCISE":
                GameManager.Instance?.RequestExerciseRestart();
                break;
            default:
                Debug.LogWarning("Nezināma komanda: " + cmdData.Command);
                LogToFile("Unknown command: " + cmdData.Command);
                break;
        }
    }

    [Serializable] public class BaseMessage { public string MessageType; }
    [Serializable] public class FeedbackMessage { public string MessageType; public int RepetitionID; public string Foot; public int Zone; }
    [Serializable] public class ExerciseConfigMessage { public string MessageType; public int RepetitionID; public string Name; public string LegsUsed; public int Intro; public int Demo; public int PreparationCop; public int TimingCop; public int Release; public int Switch; public int Sets; public ZoneSequenceItem[] ZoneSequence; }
    [Serializable] public class ZoneSequenceItem { public double Duration; public Vector2 GreenZoneX; public Vector2 GreenZoneY; public Vector2 RedZoneX; public Vector2 RedZoneY; }
    [Serializable] public class CommandData { public string Command; }
}
