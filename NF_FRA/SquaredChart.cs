using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms.DataVisualization.Charting;

namespace NF_FRA
{
    public class SquaredChart
    {
        private MainWindowViewModel vm;
        private Chart chart;
        Series series, xAxis, yAxis;
        Font labelFont = new Font("Arial", 10);
        Font titleFont = new Font("Arial", 12);
        public SquaredChart(MainWindowViewModel viewModel, Chart chart)
        {
            vm = viewModel;
            this.chart = chart;
            chart.ChartAreas.Add("ChartArea1");
            chart.ChartAreas[0].AxisX.Title = "R [Ω]";
            chart.ChartAreas[0].AxisX.TitleFont = titleFont;
            chart.ChartAreas[0].AxisX.LabelStyle.Font = labelFont;
            chart.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.DarkGray;
            chart.ChartAreas[0].AxisY.Title = "−X [Ω]";
            chart.ChartAreas[0].AxisY.TitleFont = titleFont;
            chart.ChartAreas[0].AxisY.LabelStyle.Font = labelFont;
            chart.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.DarkGray;

            series = new Series();
            series.ChartType = SeriesChartType.Line;
            series.MarkerStyle = MarkerStyle.Circle;
            series.MarkerColor = Color.Red;
            series.BorderWidth = 0;

            xAxis = new Series();
            xAxis.ChartType = SeriesChartType.Line;
            xAxis.MarkerStyle = MarkerStyle.None;
            xAxis.Color = Color.Black;
            xAxis.BorderWidth = 1;

            yAxis = new Series();
            yAxis.ChartType = SeriesChartType.Line;
            yAxis.MarkerStyle = MarkerStyle.None;
            yAxis.Color = Color.Black;
            yAxis.BorderWidth = 1;

            chart.Series.Add(series);
            chart.Series.Add(xAxis);
            chart.Series.Add(yAxis);
            chart.FormatNumber += (sender, e) => { e.LocalizedValue = e.Value.ToString("G2"); if (e.Value == 0) e.LocalizedValue = "0"; };
        }

        public void Add(double x, double y)
        {
            series.Points.AddXY(x, y);
            double minValue = int.MaxValue, maxValue = int.MinValue, interval = 0, intMinor = 0, minTmp = 0, maxTmp = 0, inttmpA = 0, inttmpB = 0;
            foreach (var point in series.Points)
            {
                if (point.XValue < minValue) minValue = point.XValue;
                if (point.YValues[0] < minValue) minValue = point.YValues[0];
                if (point.XValue > maxValue) maxValue = point.XValue;
                if (point.YValues[0] > maxValue) maxValue = point.YValues[0];
            }
            if (minValue > 0) minValue = 0;
            if (maxValue < 0) maxValue = 0;
            if (minValue != maxValue)
            {
                if (minValue < 0 && maxValue > 0)
                {
                    minTmp = minValue; maxTmp = maxValue;
                    GetNiceRoundNumbers(ref minTmp, ref maxTmp, ref inttmpA, ref intMinor);
                    GetNiceRoundNumbers(ref minTmp, ref maxTmp, ref inttmpB, ref intMinor);
                    interval = Math.Max(inttmpA, inttmpB);
                    maxValue = Math.Ceiling(maxValue / interval) * interval;
                    minValue = Math.Floor(minValue / interval) * interval;
                }
                else
                {
                    GetNiceRoundNumbers(ref minValue, ref maxValue, ref interval, ref intMinor);
                }

                chart.ChartAreas[0].AxisX.Minimum = minValue;
                chart.ChartAreas[0].AxisX.Maximum = maxValue;
                chart.ChartAreas[0].AxisX.Interval = interval;
                chart.ChartAreas[0].AxisY.Minimum = minValue;
                chart.ChartAreas[0].AxisY.Maximum = maxValue;
                chart.ChartAreas[0].AxisY.Interval = interval;
                if (xAxis.Points.Count == 0)
                {
                    xAxis.Points.AddXY(minValue, 0);
                    xAxis.Points.AddXY(maxValue, 0);
                    yAxis.Points.AddXY(0, minValue);
                    yAxis.Points.AddXY(0, maxValue);
                }
                else
                {
                    xAxis.Points[0].XValue = minValue;
                    xAxis.Points[1].XValue = maxValue;
                    yAxis.Points[0].YValues[0] = minValue;
                    yAxis.Points[1].YValues[0] = maxValue;
                }
            }
        }

        public void Add(List<FRAData> list)
        {
            foreach (var data in list)
                Add(data.R, data.X);
        }

        public void Clear()
        {
            series.Points.Clear();
            xAxis.Points.Clear();
            yAxis.Points.Clear();
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
                    g.DrawString($"Cole-Cole plot    {name}.csv", new Font("Arial", 12), Brushes.Black, rectf, sf);
                }

                bmp.Save($"{vm.SavePath}\\{name}_A.png", System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private void GetNiceRoundNumbers(ref double minValue, ref double maxValue, ref double interval, ref double intMinor)
        {
            double min = Math.Min(minValue, maxValue);
            double max = Math.Max(minValue, maxValue);
            double delta = max - min; //The full range
                                      //Special handling for zero full range
            if (delta == 0)
            {
                //When min == max == 0, choose arbitrary range of 0 - 1
                if (min == 0)
                {
                    minValue = 0;
                    maxValue = 1;
                    interval = 0.2;
                    intMinor = 0.5;
                    return;
                }
                //min == max, but not zero, so set one to zero
                if (min < 0)
                    max = 0; //min-max are -|min| to 0
                else
                    min = 0; //min-max are 0 to +|max|
                delta = max - min;
            }

            double logDel = Math.Log10(delta);
            int N = Convert.ToInt32(Math.Floor(logDel));
            double tenToN = Math.Pow(10, N);
            double A = delta / tenToN;
            //At this point maxValue = A x 10^N, where
            // 1.0 <= A < 10.0 and N = integer exponent value
            //Now, based on A select a nice round interval and maximum value
            for (int i = 0; i < roundMantissa.Length; i++)
                if (A <= roundMantissa[i])
                {
                    interval = roundInterval[i] * tenToN;
                    intMinor = roundIntMinor[i] * tenToN;
                    break;
                }
            minValue = interval * Math.Floor(min / interval);
            maxValue = interval * Math.Ceiling(max / interval);
        }
        private double[] roundMantissa = { 1.00d, 1.20d, 1.40d, 1.60d, 1.80d, 2.00d,
                           2.50d, 3.00d, 4.00d, 5.00d, 6.00d, 8.00d, 10.00d };
        private double[] roundInterval = { 0.20d, 0.20d, 0.20d, 0.20d, 0.20d, 0.50d,
                           0.50d, 0.50d, 0.50d, 1.00d, 1.00d, 2.00d, 2.00d };
        private double[] roundIntMinor = { 0.05d, 0.05d, 0.05d, 0.05d, 0.05d, 0.10d, 0.10d,
                           0.10d, 0.10d, 0.20d, 0.20d, 0.50d, 0.50d };
    }
}
