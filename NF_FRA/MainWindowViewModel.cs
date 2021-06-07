using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;
using static NF_FRA.CA5351;
using static NF_FRA.FRA51615;
using static NF_FRA.ButtonCommands;
using System.Reflection;

namespace NF_FRA
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public FRA51615 fra51615;
        public CA5351 ca5351;
        public SquaredChart chartA;
        public CombinationChart chartB;

        public ObservableCollection<String> PortItems { get; set; }
        public MainWindowViewModel()
        {
            PortItems = new ObservableCollection<string>();
            foreach (var p in SerialPort.GetPortNames())
                PortItems.Add(p);
            fra51615 = new FRA51615();
            ca5351 = new CA5351();
            FRA51615Connect = new FRA51615.ConnectCommand(this);
            CA5351Connect = new CA5351.ConnectCommand(this);
            SettingCommand = new SettingCommand(this);
            ACDCCommand = new ACDCCommand(this);
            ZeroCheckCommand = new ZeroCheckCommand(this);
            UpCommand = new UpCommand(this);
            DownCommand = new DownCommand(this);
            AbortCommand = new AbortCommand(this);
            SelectFolderCommand = new SelectFolderCommand(this);
        }

        public string WindowTitle { get { return $"NF_FRA Version: {Assembly.GetExecutingAssembly().GetName().Version.ToString(2)}"; } }

        private double minFreq;
        public double MinFreq { get { return minFreq; } set { minFreq = value; } }

        private double maxFreq;
        public double MaxFreq { get { return maxFreq; } set { maxFreq = value; } }

        private int points;
        public int Points { get { return points; } set { points = value; } }

        private int accumulation;
        public int Accumulation { get { return accumulation; } set { accumulation = value; } }

        private string savePath;
        public string SavePath
        {
            get { return savePath; }
            set
            {
                savePath = value;
                OnPropertyChanged(nameof(SavePath));
                FileList.Clear();
                var fileList = Directory.GetFiles(SavePath, "*.csv");
                Array.Sort(fileList, (x, y) =>
                {
                    try
                    {
                        int xl = x.Split('.').Length;
                        int yl = y.Split('.').Length;
                        if (xl >= 2) x = x.Split('.')[xl - 2];
                        if (yl >= 2) y = y.Split('.')[yl - 2];
                        int xnum = 0, ynum = 0, xcurr = x.Length - 1, ycurr = y.Length - 1;
                        while (xcurr >= 0 && x[xcurr] >= '0' && x[xcurr] <= '9') { xcurr--; }
                        if (xcurr + 1 != x.Length) xnum = int.Parse(x.Substring(xcurr + 1));
                        while (ycurr >= 0 && y[ycurr] >= '0' && y[ycurr] <= '9') { ycurr--; }
                        if (ycurr + 1 != y.Length) ynum = int.Parse(y.Substring(ycurr + 1));
                        int namecomp = y.Substring(0, ycurr + 1).CompareTo(x.Substring(0, xcurr + 1));
                        return namecomp != 0 ? namecomp : ynum.CompareTo(xnum);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        return 0;
                    }
                });
                foreach (var path in fileList)
                    FileList.Add(new CSVFile(path.Split("\\")[^1], File.GetLastWriteTime(path)));
                RefreshCumulativeNumber();
            }
        }

        private string seriesName = DateTime.Now.ToString("yyyy_MM_dd");
        public string SeriesName { get { return seriesName; } set { seriesName = value; OnPropertyChanged(nameof(SeriesName)); } }

        private int cumulativeNumber = 0;
        public int CumulativeNumber { get { return cumulativeNumber; } set { cumulativeNumber = value; OnPropertyChanged(nameof(CumulativeNumber)); } }

        private int gain;
        public int Gain { get { return gain; } set { gain = value; } }

        private string fra51615SelectedItem;
        public string FRA51615SelectedItem
        { get { return fra51615SelectedItem; } set { fra51615SelectedItem = value; fra51615.PortName = fra51615SelectedItem; } }

        private string fra51615StatusText = "接続";
        public string FRA51615StatusText
        { get { return fra51615StatusText; } set { fra51615StatusText = value; OnPropertyChanged(nameof(FRA51615StatusText)); } }

        public class CSVFile { public CSVFile(string fileName, DateTime dateTime) { FileName = fileName; DateTime = dateTime; } public string FileName { get; set; } public DateTime DateTime { get; set; } }
        private ObservableCollection<CSVFile> fileList = new ObservableCollection<CSVFile>();
        public ObservableCollection<CSVFile> FileList { get { return fileList; } set { if (fileList != value) fileList = value; } }

        private CSVFile selectedFile;
        public CSVFile SelectedFile
        {
            get { return selectedFile; }
            set
            {
                selectedFile = value;
                OnPropertyChanged(nameof(SelectedFile));
                if (SelectedFile != null) DrawGraph(SelectedFile.FileName);
            }
        }

        public FRA51615.ConnectCommand FRA51615Connect
        { get; private set; }

        private string ca5351SelectedItem;
        public string CA5351SelectedItem
        { get { return ca5351SelectedItem; } set { ca5351SelectedItem = value; ca5351.PortName = ca5351SelectedItem; } }

        private string ca5351StatusText = "接続";
        public string CA5351StatusText
        { get { return ca5351StatusText; } set { ca5351StatusText = value; OnPropertyChanged(nameof(CA5351StatusText)); } }

        public CA5351.ConnectCommand CA5351Connect
        { get; private set; }
        public SettingCommand SettingCommand
        { get; private set; }
        public SelectFolderCommand SelectFolderCommand
        { get; private set; }
        public ZeroCheckCommand ZeroCheckCommand
        { get; private set; }
        public ACDCCommand ACDCCommand
        { get; private set; }
        public DownCommand DownCommand
        { get; private set; }
        public UpCommand UpCommand
        { get; private set; }
        public AbortCommand AbortCommand
        { get; private set; }

        private bool acdcBackground = false;
        public bool ACDCBackground
        { get { return acdcBackground; } set { acdcBackground = value; OnPropertyChanged(nameof(ACDCBackground)); } }

        private bool zeroCheckBackground = false;
        public bool ZeroCheckBackground
        { get { return zeroCheckBackground; } set { zeroCheckBackground = value; OnPropertyChanged(nameof(ZeroCheckBackground)); } }

        private bool upBackground = false;
        public bool UpBackground
        { get { return upBackground; } set { upBackground = value; OnPropertyChanged(nameof(UpBackground)); } }

        private bool downBackground = false;
        public bool DownBackground
        { get { return downBackground; } set { downBackground = value; OnPropertyChanged(nameof(DownBackground)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string info)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info)); }

        private void DrawGraph(string filename)
        {
            string path = $@"{SavePath}\{filename}";
            if (File.Exists(path))
            {
                chartA.Clear(); chartB.Clear();
                try
                {
                    using (var sr = new StreamReader(path))
                    {
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            string[] values = line.Split(',');
                            if (values.Length >= 5)
                            {
                                double f = 0, r = 0, x = 0, z = 0, theta = 0;
                                bool res = double.TryParse(values[0], out f);
                                if (res) res = double.TryParse(values[1], out r);
                                if (res) res = double.TryParse(values[2], out x);
                                if (res) res = double.TryParse(values[3], out z);
                                if (res) res = double.TryParse(values[4], out theta);
                                if (res) { chartA.Add(r, x); chartB.Add(chartB.series1, f, z); chartB.Add(chartB.series2, f, theta); }
                            }
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "エラー"); }
            }
        }

        public void RefreshCumulativeNumber()
        {
            int existingMaxCumNum = -1;
            foreach (var name in Directory.GetFiles(SavePath).Where(path => new Regex(@$"{SeriesName}_\d+.csv").IsMatch(path.Split("\\")[^1])).ToList())
            {
                int num = int.Parse(new Regex(@$"{SeriesName}_(?<name>\d+?).csv").Match(name).Groups["name"].Value);
                if (num > existingMaxCumNum) existingMaxCumNum = num;
            }
            CumulativeNumber = existingMaxCumNum + 1;
        }
    }
}
