using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DAid.Clients
{
    public class VisualizationWindow : Form
    {
        private const int CanvasSize = 400;
        private const int SockSpacing = CanvasSize + 100;
        private const int DataTimeoutMilliseconds = 2000;
        private const int MaxTrailLength = 100;

        private (double X, double Y, double TotalPressure) _copLeft;
        private (double X, double Y, double TotalPressure) _copRight;
        private DateTime lastLeftDataUpdate = DateTime.MinValue;
        private DateTime lastRightDataUpdate = DateTime.MinValue;

        private readonly List<PointF> _copTrailLeft = new List<PointF>();
        private readonly List<PointF> _copTrailRight = new List<PointF>();

        private readonly float scaleX = CanvasSize / 8f;  
        private readonly float scaleY = CanvasSize / 14f; 

        public VisualizationWindow()
        {
            Text = "Real-Time CoP Visualization with Expanded Graph";
            Size = new Size(SockSpacing * 2, CanvasSize + 200);
            DoubleBuffered = true;

            FormClosing += (sender, e) => Application.Exit();
            Shown += (sender, e) => Console.WriteLine("[VisualizationWindow]: Visualization started.");
        }

        public void UpdateVisualization((double X, double Y, double TotalPressure) copLeft,
                                        (double X, double Y, double TotalPressure) copRight)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateVisualization(copLeft, copRight)));
                return;
            }

            _copLeft = copLeft;
            lastLeftDataUpdate = DateTime.Now;
            UpdateTrail(_copTrailLeft, copLeft);

            _copRight = copRight;
            lastRightDataUpdate = DateTime.Now;
            UpdateTrail(_copTrailRight, copRight);

            Invalidate(); 
        }

        private void UpdateTrail(List<PointF> trail, (double X, double Y, double TotalPressure) cop)
        {
            if (cop.TotalPressure > 0.0001)
            {
                PointF point = new PointF(
                    (float)(cop.X * scaleX),
                    (float)(-cop.Y * scaleY));

                trail.Add(point);

                if (trail.Count > MaxTrailLength)
                    trail.RemoveAt(0);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            graphics.Clear(Color.White);

            DrawFootPanel(graphics, SockSpacing / 2, "Left Foot", _copLeft, _copTrailLeft, lastLeftDataUpdate);
            DrawFootPanel(graphics, SockSpacing + SockSpacing / 2, "Right Foot", _copRight, _copTrailRight, lastRightDataUpdate);
        }

        private void DrawFootPanel(Graphics graphics, int xOffset, string title,
            (double X, double Y, double TotalPressure) cop, List<PointF> trail, DateTime lastUpdate)
        {
            graphics.TranslateTransform(xOffset, CanvasSize / 2 + 50);

            DrawAxes(graphics);
            graphics.DrawString(title, new Font("Arial", 12, FontStyle.Bold), Brushes.Black, -40, -CanvasSize / 2 - 40);

            bool hasRecentData = (DateTime.Now - lastUpdate).TotalMilliseconds < DataTimeoutMilliseconds;

            if (hasRecentData)
            {
                DrawTrail(graphics, trail);
                DrawCurrentCoP(graphics, cop);
            }
            else
            {
                graphics.DrawString("No Data", new Font("Arial", 14, FontStyle.Bold), Brushes.Gray, -30, -20);
            }

            graphics.ResetTransform();
        }

        private void DrawAxes(Graphics graphics)
        {
            Pen axisPen = new Pen(Color.LightGray, 1);

            graphics.DrawLine(axisPen, -4.0f * scaleX, 0, 4.0f * scaleX, 0);

            graphics.DrawLine(axisPen, 0, -7.0f * scaleY, 0, 7.0f * scaleY);

            graphics.DrawString("-4.0", new Font("Arial", 8), Brushes.Gray, -4.0f * scaleX - 20, 5);
            graphics.DrawString("4.0", new Font("Arial", 8), Brushes.Gray, 4.0f * scaleX + 5, 5);
            graphics.DrawString("X", new Font("Arial", 9, FontStyle.Bold), Brushes.Black, 4.0f * scaleX + 15, -15);

            graphics.DrawString("7.0", new Font("Arial", 8), Brushes.Gray, 5, -7.0f * scaleY - 15);
            graphics.DrawString("-7.0", new Font("Arial", 8), Brushes.Gray, 5, 7.0f * scaleY + 5);
            graphics.DrawString("Y", new Font("Arial", 9, FontStyle.Bold), Brushes.Black, -20, -7.0f * scaleY - 25);
        }

        private void DrawTrail(Graphics graphics, List<PointF> trail)
        {
            if (trail.Count < 2) return;

            Pen trailPen = new Pen(Color.Blue, 2);
            for (int i = 1; i < trail.Count; i++)
            {
                graphics.DrawLine(trailPen, trail[i - 1], trail[i]);
            }
        }

        private void DrawCurrentCoP(Graphics graphics, (double X, double Y, double TotalPressure) cop)
        {
            float x = (float)(cop.X * scaleX);
            float y = (float)(-cop.Y * scaleY);

            graphics.FillEllipse(Brushes.Red, x - 5, y - 5, 10, 10);
            graphics.DrawString($"X: {cop.X:F2}\nY: {cop.Y:F2}", new Font("Arial", 10), Brushes.Black, x + 8, y - 15);
        }
    }
}
