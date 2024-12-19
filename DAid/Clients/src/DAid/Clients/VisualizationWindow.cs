using System.Drawing;
using System;
using System.Windows.Forms;
using System.Linq;

namespace DAid.Clients
{
    public class VisualizationWindow : Form
    {
        private const int CanvasSize = 400;
        private const int SockSpacing = CanvasSize + 50; // Spacing between socks

        private double copXLeft = 0;
        private double copYLeft = 0;
        private double[] sensorPressuresLeft = Array.Empty<double>();
        private bool hasLeftData = false; // Track if left sock has data

        private double copXRight = 0;
        private double copYRight = 0;
        private double[] sensorPressuresRight = Array.Empty<double>();
        private bool hasRightData = false; // Track if right sock has data

        private bool leftSockUpdated = false;
        private bool rightSockUpdated = false;

        public VisualizationWindow()
        {
            this.Text = "Real-Time CoP Visualization";
            this.Size = new Size(SockSpacing * 2, CanvasSize + 100); // Adjusted width
            this.DoubleBuffered = true;

            this.FormClosing += (sender, e) =>
            {
                Application.Exit();
            };

            this.Shown += (sender, e) => Console.WriteLine("[VisualizationWindow]: Visualization started.");
        }

        public void UpdateVisualization(
            double xLeft, double yLeft, double[] pressuresLeft,
            double xRight, double yRight, double[] pressuresRight)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateVisualization(xLeft, yLeft, pressuresLeft, xRight, yRight, pressuresRight)));
                return;
            }

            // Update left sock data if non-empty
            if (pressuresLeft.Length > 0)
            {
                copXLeft = xLeft;
                copYLeft = yLeft;
                sensorPressuresLeft = pressuresLeft;
                hasLeftData = true;
                leftSockUpdated = true;
            }

            // Update right sock data if non-empty
            if (pressuresRight.Length > 0)
            {
                copXRight = xRight;
                copYRight = yRight;
                sensorPressuresRight = pressuresRight;
                hasRightData = true;
                rightSockUpdated = true;
            }

            if (leftSockUpdated || rightSockUpdated)
            {
                Invalidate(); // Trigger a repaint only if data is updated
            }
        }

        protected override void OnPaint(PaintEventArgs e)
{
    base.OnPaint(e);
    var graphics = e.Graphics;
    graphics.Clear(Color.White);
    DrawSockVisualization(graphics, copXLeft, copYLeft, sensorPressuresLeft, SockSpacing / 2, false);
    DrawSockVisualization(graphics, copXRight, copYRight, sensorPressuresRight, SockSpacing + SockSpacing / 2, true);

    leftSockUpdated = false;
    rightSockUpdated = false;
}


       private void DrawSockVisualization(Graphics graphics, double copX, double copY, double[] pressures, int xOffset, bool isRightSock)
{
    // Draw grid
    graphics.DrawLine(Pens.Gray, xOffset, CanvasSize / 2, xOffset + CanvasSize, CanvasSize / 2); // Horizontal center line
    graphics.DrawLine(Pens.Gray, xOffset + CanvasSize / 2, 0, xOffset + CanvasSize / 2, CanvasSize); // Vertical center line

    if (pressures.Length == 0)
    {
        DrawNoDataMessage(graphics, xOffset);
        return;
    }

    // Scale CoP to canvas
    float scaledX = (float)(xOffset + CanvasSize / 2 + copX * (CanvasSize / 8));
    float scaledY = (float)(CanvasSize / 2 - copY * (CanvasSize / 8));

    // Draw CoP
    graphics.FillEllipse(Brushes.Red, scaledX - 5, scaledY - 5, 10, 10);

    // Draw pressures
    DrawPressures(graphics, pressures, xOffset, isRightSock);
}


       private void DrawPressures(Graphics graphics, double[] pressures, int xOffset, bool isRightSock)
{
    // These positions define the sensor locations relative to the grid center
    double[] XPositions = { 6.0, -6.0, 6.0, -6.0 };
    double[] YPositions = { 2.0, 2.0, -2.0, -2.0 };

    // Mirror the X positions for the right sock
    if (isRightSock)
    {
        XPositions = XPositions.Select(x => -x).ToArray(); // Flip X axis for the right sock
    }

    double maxPressure = pressures.Length > 0 ? pressures.Max() : 1.0;
    if (maxPressure <= 0.001) maxPressure = 1.0; // Prevent division by zero

    for (int i = 0; i < pressures.Length; i++)
    {
        float intensity = (float)(pressures[i] / maxPressure);
        intensity = Math.Max(0, Math.Min(1, intensity));

        Color pressureColor = Color.FromArgb((int)(255 * intensity), 0, 0);

        // Scale sensor positions to the canvas
        float scaledX = (float)(xOffset + CanvasSize / 2 + XPositions[i] * (CanvasSize / 16));
        float scaledY = (float)(CanvasSize / 2 - YPositions[i] * (CanvasSize / 16));

        graphics.FillEllipse(new SolidBrush(pressureColor), scaledX - 10, scaledY - 10, 20, 20);
        graphics.DrawString($"S{i + 1}", new Font("Arial", 8), Brushes.Black, scaledX - 5, scaledY - 15);
    }
}

        private void DrawNoDataMessage(Graphics graphics, int xOffset)
        {
            graphics.DrawString("No Data", new Font("Arial", 16, FontStyle.Bold), Brushes.Gray,
                new PointF(xOffset + CanvasSize / 4 - 50, CanvasSize / 2 - 20));
        }
    }
}
