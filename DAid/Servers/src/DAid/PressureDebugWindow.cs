using System;
using System.Linq;
using System.Windows.Forms;
using System.Text;

    public class PressureDebugWindow : Form
    {
        private Timer updateTimer;
        private TextBox outputBox;
        private double[] lastLeftPressures = Array.Empty<double>();
        private double[] lastRightPressures = Array.Empty<double>();
        private DateTime lastUpdate = DateTime.MinValue;

        public void UpdatePressures(double[] left, double[] right)
        {
            lastLeftPressures = left;
            lastRightPressures = right;
            lastUpdate = DateTime.Now;
        }

        public PressureDebugWindow()
        {
            this.Text = "Pressure Debug Output";
            this.Size = new System.Drawing.Size(600, 400);

            outputBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 10)
            };

            this.Controls.Add(outputBox);

            updateTimer = new Timer { Interval = 500 };
            updateTimer.Tick += (s, e) => RefreshOutput();
            updateTimer.Start();
        }

        private void RefreshOutput()
        {
            if ((DateTime.Now - lastUpdate).TotalMilliseconds > 2000)
            {
                outputBox.AppendText($"[{DateTime.Now:HH:mm:ss}] No recent data...\r\n");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}]");

            if (lastLeftPressures.Length > 0)
                sb.AppendLine($"Left:  {string.Join(", ", lastLeftPressures.Select(p => p.ToString("F5")))}");

            if (lastRightPressures.Length > 0)
                sb.AppendLine($"Right: {string.Join(", ", lastRightPressures.Select(p => p.ToString("F5")))}");

            sb.AppendLine();
            outputBox.AppendText(sb.ToString());
        }
    }


