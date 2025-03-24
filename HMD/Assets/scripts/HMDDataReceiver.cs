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
    private bool restartInProgress = false;
    private TcpListener _listener;
    private TcpClient _client;
    private NetworkStream _stream;
    private bool _isConnected = false;
    public bool IsClientConnected => _isConnected;

    [Header("Server Settings")]
    [Tooltip("Server IP Address (e.g., 127.0.0.1)")]
    public string serverIp = "127.0.0.1";
    [Tooltip("Server Port")]
    public int serverPort = 9001;

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
        {
            Destroy(gameObject); // prevent duplicate instances
            return;
        }
        Instance = this;
    }

    void Start()
    {
        StartServer();
    }

    void Update()
    {
        // Process queued actions on the main thread.
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                mainThreadActions.Dequeue().Invoke();
            }
        }

        // Process incoming data only if a client is connected.
        if (_isConnected && _stream != null && _stream.DataAvailable)
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log($"Saòemtais JSON: {receivedData}");

                // Append the received data to our persistent buffer.
                incomingBuffer += receivedData;

                // Process complete JSON objects from the buffer.
                while (true)
                {
                    // Find the first opening brace.
                    int startIndex = incomingBuffer.IndexOf("{");
                    if (startIndex == -1)
                    {
                        // No JSON object found.
                        incomingBuffer = "";
                        break;
                    }

                    int braceCount = 0;
                    int endIndex = -1;
                    // Loop over the buffer starting at the first '{'.
                    for (int i = startIndex; i < incomingBuffer.Length; i++)
                    {
                        if (incomingBuffer[i] == '{')
                        {
                            braceCount++;
                        }
                        else if (incomingBuffer[i] == '}')
                        {
                            braceCount--;
                        }

                        // When the brace count returns to zero, we have a complete JSON object.
                        if (braceCount == 0)
                        {
                            endIndex = i;
                            break;
                        }
                    }

                    // If we didn't find a complete JSON object, wait for more data.
                    if (endIndex == -1)
                    {
                        break;
                    }

                    // Extract the complete JSON message.
                    string completeJson = incomingBuffer.Substring(startIndex, endIndex - startIndex + 1);
                    Debug.Log($"Processing complete JSON message: {completeJson}");
                    ParseAndUpdateVisualization(completeJson);

                    // Remove the processed JSON from the buffer.
                    incomingBuffer = incomingBuffer.Substring(endIndex + 1);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Kïûda datu saòemðanâ: {ex.Message}");
            }
        }
    }


    public static void RunOnMainThread(Action action)
    {
        if (action == null) return;
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

   
    private void LogToFile(string message)
    {
        string path = Application.persistentDataPath + "/server_log.txt";
        using (StreamWriter writer = new StreamWriter(path, true))
        {
            writer.WriteLine($"{System.DateTime.Now}: {message}");
        }
    }

    private void StartServer()
    {
        try
        {
            LogToFile("Server starting on port " + serverPort);
            _listener = new TcpListener(IPAddress.Any, serverPort);
            _listener.Start();
            LogToFile("Server started on port " + serverPort);
            _listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), _listener);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error starting the server: {ex.Message}");
        }
    }


    private void OnClientConnect(IAsyncResult result)
    {
        try
        {
            // Get the new client connection.
            TcpClient newClient = _listener.EndAcceptTcpClient(result);

            // If there's already an active connection, disconnect it.
            if (_client != null)
            {
                Debug.LogWarning("Already connected to a client, disconnecting the old connection and accepting new one.");
                DisconnectFromServer();
            }

            // Assign the new client and set up the stream.
            _client = newClient;
            _stream = _client.GetStream();
            _isConnected = true;

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

            // Accept the next client connection.
            _listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), _listener);
        }
        catch (Exception ex)
        {
            RunOnMainThread(() =>
            {
                Debug.LogError($"Error accepting client connection: {ex.Message}");
            });
        }
    }

    private void OnApplicationQuit()
    {
        // Properly disconnect any client and stop the listener.
        DisconnectFromServer();
        if (_listener != null)
        {
            _listener.Stop();
            _listener = null;
        }
    }



    private void DisconnectFromServer()
    {
        if (_stream != null)
        {
            _stream.Close();
            _stream = null;
        }
        if (_client != null)
        {
            _client.Close();
            _client = null;
        }
        _isConnected = false;
        Debug.Log("Atvienots no servera.");
    }

    /// <summary>
    /// Parses the incoming JSON based on a MessageType field and routes it accordingly.
    /// </summary>
    /// <param name="jsonData">The JSON data received from the client.</param>
    private void ParseAndUpdateVisualization(string jsonData)
    {
        BaseMessage baseMsg = JsonUtility.FromJson<BaseMessage>(jsonData);
        if (baseMsg == null || string.IsNullOrEmpty(baseMsg.MessageType))
        {
            Debug.LogWarning("Nederîgi ziòojumi!");
            return;
        }
        if (baseMsg.MessageType == "Feedback")
        {
            FeedbackMessage feedback = JsonUtility.FromJson<FeedbackMessage>(jsonData);
            UpdateFootFeedback(feedback);
        }
        else if (baseMsg.MessageType == "ExerciseConfig")
        {
            ExerciseConfigMessage config = JsonUtility.FromJson<ExerciseConfigMessage>(jsonData);
            GameManager.Instance?.UpdateExerciseConfiguration(config);
        }
        else if (baseMsg.MessageType == "Command")
        {
            CommandData cmdData = JsonUtility.FromJson<CommandData>(jsonData);
            ProcessCommand(cmdData);
        }
    }

    /// <summary>
    /// Updates the visual feedback on the appropriate foot based on the feedback message.
    /// </summary>
    /// <param name="feedback">The feedback message containing zone and foot info.</param>
    private void UpdateFootFeedback(FeedbackMessage feedback)
    {
        Debug.Log($"Saòemts feedback: Foot: {feedback.Foot}, Zone: {feedback.Zone}");

        // Check if this is a restart command (zone 7)
        if (feedback.Zone == 7)
        {
            if (!restartInProgress)
            {
                restartInProgress = true;
                Debug.Log("Zone 7 received: triggering restart.");
                // Instead of normal feedback handling, convert this into a command call.
                ProcessCommand(new CommandData { Command = "RESTART_EXERCISE" });
            }
            return;
        }

        // If a restart is already in progress, ignore other feedback messages.
        if (restartInProgress)
        {
            Debug.Log("Restart in progress, ignoring feedback.");
            return;
        }

        // Process feedback normally:
        if (feedback.Foot.ToLower() == "left")
        {
            GameManager.Instance?.UpdateFootStatusForFoot(feedback.Zone, "Left");
        }
        else if (feedback.Foot.ToLower() == "right")
        {
            GameManager.Instance?.UpdateFootStatusForFoot(feedback.Zone, "Right");
        }
        else if (feedback.Foot.ToLower() == "both")
        {
            GameManager.Instance?.UpdateFootStatusForFoot(feedback.Zone, "Both");
        }
    }

    /// <summary>
    /// Processes command messages sent from the client.
    /// </summary>
    /// <param name="cmdData">The command data.</param>
    private void ProcessCommand(CommandData cmdData)
    {
        Debug.Log($"Saòemta komanda: {cmdData.Command}");
        switch (cmdData.Command)
        {
            case "PREPARATION_SUCCESS":
                GameManager.Instance?.MarkPreparationSuccessful();
                break;
            case "RESTART_EXERCISE":
                GameManager.Instance?.RequestExerciseRestart();
                Debug.Log("Received RESTART_EXERCISE command. Calling RequestExerciseRestart.");
                // Start a coroutine to reset the restart flag after a delay,
                // allowing the restart command to complete before resuming normal updates.
                StartCoroutine(ResetRestartFlagAfterDelay(5f));
                break;
            default:
                Debug.LogWarning("Nezinâma komanda!");
                break;
        }
    }

    private System.Collections.IEnumerator ResetRestartFlagAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        restartInProgress = false;
        Debug.Log("Restart flag reset, resuming normal feedback processing.");
    }

    // --------------------- JSON Message Classes ---------------------
    [Serializable]
    public class BaseMessage
    {
        public string MessageType;
    }

    [Serializable]
    public class FeedbackMessage
    {
        public string MessageType;
        public int RepetitionID;
        public string Foot;
        public int Zone;  
    }


    [Serializable]
    public class ExerciseConfigMessage
    {
        public string MessageType;
        public int RepetitionID;
        public string Name;
        public string LegsUsed;
        public int Intro;
        public int Demo;
        public int PreparationCop;
        public int TimingCop;
        public int Release;
        public int Switch;
        public int Sets;
        public ZoneSequenceItem[] ZoneSequence;
    }

    [Serializable]
    public class ZoneSequenceItem
    {
        public int Duration;
        public Vector2 GreenZoneX;
        public Vector2 GreenZoneY;
        public Vector2 RedZoneX;
        public Vector2 RedZoneY;
    }

    [Serializable]
    public class CommandData
    {
        public string Command;
    }
}
