using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientGUI
{
    public partial class Form1 : Form
    {
        private TcpListener _server;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _listenerThread;
        private bool _isRunning = true;
        private List<string> selectedPorts = new List<string>();
        private StringBuilder _messageBuffer = new StringBuilder();

        // for serializing log-file writes
        private readonly object _logLock = new object();
        private readonly string _logFilePath =
            Path.Combine(Application.StartupPath, "message.log");

        // user-selection state
        private string selectedUserConfig = "";
        private Dictionary<string, string> userConfigDict = new Dictionary<string, string>();
        private bool _portsParsed = false;
        private string usersFilePath = Path.Combine(Application.StartupPath, "users.txt");

        public Form1()
        {
            InitializeComponent();
            this.Icon = new Icon(Path.Combine(Application.StartupPath, "GuiLogo.ico"));

            // hide controls until a user is chosen
            connectButton.Visible = false;
            statusLabel.Visible = false;
            textBox1.Visible = false;

            ShowUserSelectionScreen();
            StartTcpServer();
        }

        private void ShowUserSelectionScreen()
        {
            flowLayoutPanel1.Controls.Clear();

            var newUserButton = new Button
            {
                Text = "Jauns lietotājs",
                Width = 150,
                Height = 30,
                Margin = new Padding(10),
                BackColor = Color.LightBlue
            };
            newUserButton.Click += newUserButton_Click;
            flowLayoutPanel1.Controls.Add(newUserButton);

            var contButton = new Button
            {
                Text = "Turpināt ar esošu lietotāju",
                Width = 200,
                Height = 30,
                Margin = new Padding(10),
                BackColor = Color.LightGreen
            };
            contButton.Click += continueButton_Click;
            flowLayoutPanel1.Controls.Add(contButton);
        }

        private void newUserButton_Click(object sender, EventArgs e)
        {
            var newUserForm = new NewUserForm(usersFilePath);
            if (newUserForm.ShowDialog() == DialogResult.OK)
            {
                AppendText("New user saved.");
                ProceedAfterUserSelection();
            }
        }

        private void continueButton_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Controls.Clear();

            var selectLabel = new Label
            {
                Text = "Izvēlēties lietotāju:",
                AutoSize = true,
                Margin = new Padding(5)
            };
            flowLayoutPanel1.Controls.Add(selectLabel);

            var userComboBox = new ComboBox
            {
                Width = 200,
                Margin = new Padding(5)
            };

            if (File.Exists(usersFilePath))
            {
                foreach (var line in File.ReadAllLines(usersFilePath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        var display = $"ID: {parts[0]} - {parts[1]}";
                        userComboBox.Items.Add(display);
                        userConfigDict[display] = line;
                    }
                }
            }
            else
            {
                MessageBox.Show("No user data file found. Please create a new user.");
                ShowUserSelectionScreen();
                return;
            }

            if (userComboBox.Items.Count == 0)
            {
                MessageBox.Show("No users available. Please create a new user.");
                ShowUserSelectionScreen();
                return;
            }

            userComboBox.SelectedIndex = 0;
            flowLayoutPanel1.Controls.Add(userComboBox);

            var selectUserButton = new Button
            {
                Text = "OK",
                Width = 60,
                Height = 30,
                Margin = new Padding(5),
                BackColor = Color.LightGreen
            };
            selectUserButton.Click += (s, ea) =>
            {
                var selectedDisplay = userComboBox.SelectedItem.ToString();
                selectedUserConfig = userConfigDict[selectedDisplay];
                AppendText($"User selected: {selectedDisplay}");
                ProceedAfterUserSelection();
            };
            flowLayoutPanel1.Controls.Add(selectUserButton);
        }

        private void ProceedAfterUserSelection()
        {
            flowLayoutPanel1.Controls.Clear();
            connectButton.Visible = true;
            statusLabel.Visible = true;
            textBox1.Visible = true;
            AppendText("User confirmed. Now you may connect to the client.");
        }

        private void StartTcpServer()
        {
            _listenerThread = new Thread(() =>
            {
                try
                {
                    _server = new TcpListener(IPAddress.Loopback, 5555);
                    _server.Start();
                    AppendText("GUI Server started. Waiting for Client...");

                    _client = _server.AcceptTcpClient();
                    AppendText("Client connected!");

                    _stream = _client.GetStream();

                    var messageListenerThread = new Thread(ListenForMessages)
                    {
                        IsBackground = true
                    };
                    messageListenerThread.Start();
                }
                catch (Exception ex)
                {
                    AppendText($"Error: {ex.Message}");
                }
            })
            {
                IsBackground = true
            };
            _listenerThread.Start();
        }

        private void ListenForMessages()
        {
            var buffer = new byte[1024];
            while (_isRunning)
            {
                try
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string dataChunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    _messageBuffer.Append(dataChunk);

                    // Split messages using newline delimiter
                    string delimiter = "\n";
                    int delimiterIndex;
                    while ((delimiterIndex = _messageBuffer.ToString().IndexOf(delimiter)) != -1)
                    {
                        string message = _messageBuffer.ToString(0, delimiterIndex).Trim();
                        _messageBuffer.Remove(0, delimiterIndex + delimiter.Length);

                        if (!string.IsNullOrEmpty(message))
                        {
                            ProcessReceivedMessage(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendText($"Error reading message: {ex.Message}");
                    AppendText("Connection closed.");
                    break;
                }
            }
        }

        private void ProcessReceivedMessage(string message)
        {
            AppendText($"Client: {message}");

            if (!_portsParsed && message.ToLower().Contains("com"))
            {
                _portsParsed = true;
                var portsString = message.Replace("Client:", "").Trim();
                var ports = portsString.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                AppendText($"Parsed ports: {string.Join(", ", ports)}");
                CreatePortButtons(ports);
            }
        }

        private void CreatePortButtons(string[] ports)
        {
            try
            {
                AppendText($"[DEBUG] CreatePortButtons called with {ports.Length} ports.");
                selectedPorts.Clear();

                // use MethodInvoker instead of Action
                MethodInvoker clearPanel = delegate
                {
                    flowLayoutPanel1.Controls.Clear();
                    AppendText("[DEBUG] Cleared FlowLayoutPanel.");
                };
                if (flowLayoutPanel1.InvokeRequired)
                    flowLayoutPanel1.Invoke(clearPanel);
                else
                    clearPanel();

                if (ports.Length == 0)
                {
                    AppendText("No COM ports received. Buttons not created.");
                    return;
                }

                foreach (var port in ports)
                {
                    AppendText($"[DEBUG] Creating button for: {port}");
                    var button = new Button
                    {
                        Text = port,
                        Width = 150,
                        Height = 40,
                        Margin = new Padding(5),
                        BackColor = Color.LightBlue
                    };
                    button.Click += (s, e) =>
                    {
                        if (selectedPorts.Count < 2)
                        {
                            selectedPorts.Add(port);
                            button.BackColor = Color.LightGreen;
                            button.ForeColor = Color.White;
                            button.Enabled = false;
                            AppendText($"Selected: {port}");

                            if (selectedPorts.Count == 2)
                            {
                                var portsMessage = string.Join(",", selectedPorts);
                                AppendText($"Sent selected ports to client: {portsMessage}");
                                File.WriteAllText(
                                    Path.Combine(Application.StartupPath, "selected_ports.txt"),
                                    portsMessage);
                                AppendText("Written selected ports to file.");

                                MethodInvoker clearAfterSelect = delegate
                                {
                                    flowLayoutPanel1.Controls.Clear();
                                    AppendText("[DEBUG] Cleared FlowLayoutPanel after selecting two ports.");
                                };
                                if (flowLayoutPanel1.InvokeRequired)
                                    flowLayoutPanel1.Invoke(clearAfterSelect);
                                else
                                    clearAfterSelect();

                                SwitchToNextScreen();
                            }
                        }
                    };

                    MethodInvoker addBtn = delegate
                    {
                        flowLayoutPanel1.Controls.Add(button);
                        flowLayoutPanel1.PerformLayout();
                        flowLayoutPanel1.Refresh();
                        AppendText($"[DEBUG] Added button for {port}.");
                    };
                    if (flowLayoutPanel1.InvokeRequired)
                        flowLayoutPanel1.Invoke(addBtn);
                    else
                        addBtn();
                }

                AppendText($"[DEBUG] Created {ports.Length} buttons.");
            }
            catch (Exception ex)
            {
                AppendText($"[ERROR] CreatePortButtons failed: {ex.Message}");
            }
        }

        private void SwitchToNextScreen()
        {
            var calibrateButton = new Button
            {
                Text = "Calibrate",
                Width = 150,
                Height = 40,
                Margin = new Padding(10),
                BackColor = Color.LightGreen
            };
            calibrateButton.Click += (s, e) =>
            {
                SendMessageToClient("calibrate");
                calibrateButton.Enabled = false;
                AppendText("Calibrate command sent to client.");
                ShowControlButtons();
            };
            AddButtonToPanel(calibrateButton);

            void ShowControlButtons()
            {
                var startButton = new Button
                {
                    Text = "Start",
                    Width = 150,
                    Height = 40,
                    Margin = new Padding(10),
                    BackColor = Color.LightGreen
                };
                startButton.Click += (s, e) =>
                {
                    SendMessageToClient("start");
                    startButton.Enabled = false;
                    AppendText("Start command sent to client.");
                };

                var stopsButton = new Button
                {
                    Text = "Stop",
                    Width = 150,
                    Height = 40,
                    Margin = new Padding(10),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(25, Color.IndianRed),
                    ForeColor = Color.White
                };
                stopsButton.FlatAppearance.BorderSize = 0;
                stopsButton.FlatAppearance.MouseDownBackColor = Color.Transparent;
                stopsButton.FlatAppearance.MouseOverBackColor = Color.Transparent;
                stopsButton.Click += (s, e) =>
                {
                    SendMessageToClient("stop");
                    AppendText("Stop command sent to client.");
                };

                var exitButton = new Button
                {
                    Text = "Exit",
                    Width = 150,
                    Height = 40,
                    Margin = new Padding(10),
                    BackColor = Color.IndianRed
                };
                exitButton.Click += (s, e) =>
                {
                    SendMessageToClient("exit");
                    AppendText("Exit command sent to client.");
                };

                AddButtonToPanel(startButton);
                AddButtonToPanel(stopsButton);
                AddButtonToPanel(exitButton);
            }
        }

        private void AddButtonToPanel(Button button)
        {
            MethodInvoker add = delegate
            {
                flowLayoutPanel1.Controls.Add(button);
                flowLayoutPanel1.PerformLayout();
                flowLayoutPanel1.Refresh();
            };

            if (flowLayoutPanel1.InvokeRequired)
                flowLayoutPanel1.Invoke(add);
            else
                add();
        }

        private void SendMessageToClient(string message)
        {
            if (_stream == null) return;
            var bytes = Encoding.UTF8.GetBytes(message);
            _stream.Write(bytes, 0, bytes.Length);
            _stream.Flush();
            AppendText($"SEND → {message}");
        }

        private void AppendText(string text)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff");
            var line = $"{timestamp}  {text}{Environment.NewLine}";

            try
            {
                lock (_logLock)
                {
                    File.AppendAllText(_logFilePath, line);
                }
            }
            catch (IOException)
            {
                // ignore file-in-use errors
            }

            MethodInvoker uiAppend = delegate
            {
                textBox1.AppendText(text + Environment.NewLine);
            };

            if (InvokeRequired)
                Invoke(uiAppend);
            else
                uiAppend();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _isRunning = false;
                _stream?.Close();
                _client?.Close();
                _server?.Stop();
                Console.WriteLine("[Server]: Connection closed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedUserConfig))
            {
                SendMessageToClient("config:" + selectedUserConfig);
                AppendText("Selected user's config sent to client.");
            }
            else
            {
                AppendText("No user configuration available to send.");
            }

            SendMessageToClient("connect");
            AppendText("Connect command sent to client. Waiting for port list...");

            connectButton.Enabled = false;
            AppendText("Connect button disabled. Awaiting port information.");

            Task.Run(() => WaitForClientResponse());
        }

        private void WaitForClientResponse()
        {
            try
            {
                var buffer = new byte[1024];
                while (_isRunning && _stream != null)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        AppendText("Client connection lost. No data received.");
                        break;
                    }

                    var clientMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    AppendText($"Client: {clientMessage}");

                    if (clientMessage.ToLower() == "ports")
                    {
                        AppendText("Client has sent COM ports information.");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendText($"Error while waiting for client response: {ex.Message}");
            }
        }
    }
}