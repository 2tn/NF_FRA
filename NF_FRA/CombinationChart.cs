using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms.DataVisualization.Charting;

namespace NF_FRA
{
    public class CombinationChart
    {
        private MainWindowViewModel vm;
        private Chart chart;
        public Series series1, series2;
        Font labelFont = new Font("Arial", 10);
        Font titleFont = new Font("Arial", 12);
        public CombinationChart(MainWindowViewModel viewModel, Chart chart)
        {
            vm = viewModel;
            this.chart = chart;
            chart.ChartAreas.Add("ChartArea1");
            chart.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.DarkGray;
            chart.ChartAreas[0].AxisX.Title = "Frequency [Hz]";
            chart.ChartAreas[0].AxisX.TitleFont = titleFont;
            chart.ChartAreas[0].AxisX.LabelStyle.Font = labelFont;
            chart.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Pink;
            chart.ChartAreas[0].AxisY.LabelStyle.ForeColor = Color.Red;
            chart.ChartAreas[0].AxisY.Minimum = 0;
            chart.ChartAreas[0].AxisY.Title = "Z [Ω]";
            chart.ChartAreas[0].AxisY.TitleFont = titleFont;
            chart.ChartAreas[0].AxisY.LabelStyle.Font = labelFont;
            chart.ChartAreas[0].AxisY2.MajorGrid.LineWidth = 0;
            chart.ChartAreas[0].AxisY2.LabelStyle.Enabled = true;
            chart.ChartAreas[0].AxisY2.LabelStyle.ForeColor = Color.Blue;
            chart.ChartAreas[0].AxisY2.Minimum = -180;
            chart.ChartAreas[0].AxisY2.Maximum = 180;
            chart.ChartAreas[0].AxisY2.Interval = 45;
            chart.ChartAreas[0].AxisY2.Title = "θ [deg]";
            chart.ChartAreas[0].AxisY2.TitleFont = titleFont;
            chart.ChartAreas[0].AxisY2.LabelStyle.Font = labelFont;

            series1 = new Series();
            series1.ChartType = SeriesChartType.Line;
            series1.MarkerStyle = MarkerStyle.Circle;
            series1.MarkerColor = Color.Red;
            series1.Color = Color.DeepPink;

            series2 = new Series();
            series2.ChartType = SeriesChartType.Line;
            series2.MarkerStyle = MarkerStyle.Circle;
            series2.MarkerColor = Color.Blue;
            series2.Color = Color.DeepSkyBlue;
            series2.YAxisType = AxisType.Secondary;

            chart.Series.Add(series1);
            chart.Series.Add(series2);

            chart.FormatNumber += (sender, e) => { if (sender as Axis != chart.ChartAreas[0].AxisY2) e.LocalizedValue = e.Value.ToString("G2"); if (e.Value == 0) e.LocalizedValue = "0"; };
        }
        int Pow10(int x)
        {
            int ret = 1;
            for (int i = 0; i < x; i++)
                ret *= 10;
            return ret;
        }

        public void Add(Series series, double x, double y)
        {
            series.Points.AddXY(x, y);
            double xmin = double.MaxValue, xmax = double.MinValue;
            foreach (var p in series.Points)
            {
                xmin = Math.Min(xmin, p.XValue);
                xmax = Math.Max(xmax, p.XValue);
            }
            chart.ChartAreas[0].AxisX.Minimum = Pow10((int)Math.Floor(Math.Log10(xmin)));
            chart.ChartAreas[0].AxisX.Maximum = Pow10((int)Math.Ceiling(Math.Log10(xmax)));
            chart.ChartAreas[0].AxisX.IsLogarithmic = true;
        }

        public void Add(List<FRAData> list)
        {
            foreach (var data in list)
            {
                Add(series1, data.F, data.Z);
                Add(series2, data.F, data.Theta);
            }
        }
        public void Clear()
        {
            chart.ChartAreas[0].AxisX.IsLogarithmic = false;
            series1.Points.Clear();
            series2.Points.Clear();
        }

        public void getScreenShot(string name)
        {
            int w = chart.Size.Width;
            int h = chart.Size.Height;
            using (Bitmap bmp = new Bitmap(w, h))
            {
                chart.DrawToBitmap(bmp, new Rectangle(0, 0, w, h));
                var rectf = new RectangleF(0, 0, w - 100, h);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Far;
                    sf.LineAlignment = StringAlignment.Near;
                    g.DrawString($"Z-θ plot    {name}.csv", new Font("Arial", 12), Brushes.Black, rectf, sf);
                }

                bmp.Save($"{vm.SavePath}\\{name}_B.png", System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
