using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

public class PressureDebugWindow : Form
{
    private Timer updateTimer;
    private Chart pressureChart;
    private FlowLayoutPanel legendPanel;
    private double[] _pressures = Array.Empty<double>();
    private DateTime lastUpdate = DateTime.MinValue;
    private int timeCounter = 0;

    private readonly string[] sensorNames = { "S1", "S2", "S3", "S4" };


    private readonly System.Drawing.Color[] sensorColors =
    {
        System.Drawing.Color.Red,
        System.Drawing.Color.Green,
        System.Drawing.Color.Blue,
        System.Drawing.Color.Orange
    };

    public PressureDebugWindow()
    {
        this.Size = new System.Drawing.Size(800, 500);
        this.Text = "Pressure Debug Graph";

        pressureChart = new Chart
        {
            Dock = DockStyle.Fill
        };

        var chartArea = new ChartArea("MainArea")
        {
            AxisX = { Title = "Time", Minimum = 0, Maximum = 100, Interval = 10 },
            AxisY = { Title = "Pressure", Minimum = 0, Maximum = 500, Interval = 25 }
        };
        chartArea.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
        chartArea.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
        pressureChart.ChartAreas.Add(chartArea);

        for (int i = 0; i < 4; i++)
        {
            var series = new Series(sensorNames[i])
            {
                ChartType = SeriesChartType.Line,
                Color = sensorColors[i],
                BorderWidth = 2,
                ChartArea = "MainArea",
                LegendText = sensorNames[i]
            };
            pressureChart.Series.Add(series);
        }

        pressureChart.Legends.Add(new Legend("Sensors")
        {
            Docking = Docking.Top,
            Alignment = System.Drawing.StringAlignment.Center
        });

        legendPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Bottom,
            Height = 40
        };

        foreach (var series in pressureChart.Series)
        {
            var checkBox = new CheckBox
            {
                Text = series.Name,
                Checked = true,
                AutoSize = true
            };
            checkBox.CheckedChanged += (s, e) =>
            {
                series.Enabled = checkBox.Checked;
            };
            legendPanel.Controls.Add(checkBox);
        }

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.Controls.Add(pressureChart, 0, 0);
        layout.Controls.Add(legendPanel, 0, 1);

        this.Controls.Add(layout);

        updateTimer = new Timer { Interval = 100 };
        updateTimer.Tick += (s, e) => RefreshChart();
        updateTimer.Start();
    }

    public void UpdatePressures(double[] pressures)
    {
        _pressures = pressures ?? Array.Empty<double>();
        lastUpdate = DateTime.Now;
    }

    private void RefreshChart()
    {
        if ((DateTime.Now - lastUpdate).TotalMilliseconds > 2000)
            return;

        timeCounter++;

        for (int i = 0; i < 4; i++)
        {
            if (_pressures.Length > i)
            {
                var series = pressureChart.Series[sensorNames[i]];
                series.Points.AddXY(timeCounter, _pressures[i]);

                if (series.Points.Count > 100)
                    series.Points.RemoveAt(0);
            }
        }

        if (timeCounter > 100)
        {
            var area = pressureChart.ChartAreas["MainArea"];
            area.AxisX.Minimum = timeCounter - 100;
            area.AxisX.Maximum = timeCounter;
        }
    }
}
