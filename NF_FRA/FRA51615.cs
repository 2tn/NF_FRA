using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace NF_FRA
{
    public class FRA51615
    {
        private SerialPort port;
        public FRA51615()
        {
            port = new SerialPort();
            port.BaudRate = 115200;
        }


        private string portName;
        public string PortName
        {
            get { return portName; }
            set
            {
                portName = value;
            }
        }
        public bool IsOpen { get { return port.IsOpen; } }

        public void Open()
        {
            try
            {
                if (PortName is null)
                {
                    MessageBox.Show("ポートを選択してください。", "エラー");
                    return;
                }
                if (port.IsOpen == false)
                {
                    port.PortName = PortName;
                    port.Open();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "エラー");
            }
        }

        public void Close()
        {
            try
            {
                if (PortName is null)
                {
                    MessageBox.Show("ポートを選択してください。", "エラー");
                    return;
                }
                if (port.IsOpen == true)
                {
                    port.Close();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "エラー");
            }
        }

        public bool getIdentification()
        {
            if (port.IsOpen)
            {
                Write("*IDN?\n");
                var result = ReceiveData(1);
                bool ret = false;
                if (result.Split(",").Length > 2)
                    ret = result.Split(",")[1] == "FRA51615";
                return ret;
            }
            return false;
        }

        public double getMinFreq()
        {
            if (port.IsOpen)
            {
                Write(":SOUR:FREQ:STAR?\n");
                var result = ReceiveData();
                double ret = 0.0;
                double.TryParse(result, out ret);
                return ret;
            }
            return 0.0;
        }

        public double setMinFreq(double value)
        {
            if (port.IsOpen)
            {
                if (value >= 0.00001 && value <= 15000000)
                {
                    Write($":SOUR:FREQ:STAR {value.ToString("F5")}HZ\n");
                    return getMinFreq();
                }
            }
            return 0.0;
        }

        public double getMaxFreq()
        {
            if (port.IsOpen)
            {
                Write(":SOUR:FREQ:STOP?\n");
                var result = ReceiveData();
                double ret = 0.0;
                double.TryParse(result, out ret);
                return ret;
            }
            return 0.0;
        }

        public double setMaxFreq(double value)
        {
            if (port.IsOpen)
            {
                if (value >= 0.00001 && value <= 15000000)
                {
                    Write($":SOUR:FREQ:STOP {value.ToString("F5")}HZ\n");
                    return getMaxFreq();
                }
            }
            return 0.0;
        }

        public int setPoints(int value)
        {
            if (port.IsOpen)
            {
                if (value >= 3 && value <= 20000)
                {
                    Write($":SOUR:SWE:POIN {value}\n");
                    return getPoints();
                }
            }
            return 0;
        }

        public int getPoints()
        {
            if (port.IsOpen)
            {
                Write(":SOUR:SWE:POIN?\n");
                var result = ReceiveData();
                int ret;
                int.TryParse(result, out ret);
                return ret;
            }
            return 0;
        }

        public int setAccumulation(int value)
        {
            if (port.IsOpen)
            {
                if (value >= 1 && value <= 9999)
                {
                    Write($":SENS:AVER:COUN {value},CYCL\n");
                    return getAccumulation();
                }
            }
            return 0;
        }

        public int getAccumulation()
        {
            if (port.IsOpen)
            {
                Write(":SENS:AVER:COUN? CYCL\n");
                var result = ReceiveData();
                int ret;
                int.TryParse(result, out ret);
                return ret;
            }
            return 0;
        }

        public bool getACDC()
        {
            if (port.IsOpen)
            {
                Write(":OUTP:STAT?\n");
                var result = ReceiveData();
                Debug.WriteLine(result.Replace("\n", "_"));
                return result == "ON" ? true : false;
            }
            return false;
        }

        public bool setACDC(bool value)
        {
            if (port.IsOpen)
            {
                Write($":OUTP:STAT {(value ? "ON" : "OFF")}\n");
                int count = 0;
                while (getACDC() != value) { if (count > 10) break; count++; }
                return getACDC();
            }
            return false;
        }

        public void setXY()
        {
            if (port.IsOpen)
                Write(":CALC:FORM R,MX,NONE\n");
        }

        public long getGain()
        {
            if (port.IsOpen)
            {
                Write(":INP:GAIN?\n");
                var result = ReceiveData();
                decimal param1 = 0; bool b = false;
                if (result.Split(",").Length > 1) b = decimal.TryParse(result.Split(",")[0], System.Globalization.NumberStyles.Float, new CultureInfo(""), out param1);
                return b ? (long)param1 : 0;
            }
            return 0;
        }

        public long setGain(long value)
        {
            if (port.IsOpen)
            {
                Write($":INP:GAIN {value},1\n");
                int count = 0;
                while (getGain() != value) { if (count > 10) break; count++; }
                return getGain();
            }
            return 0;
        }

        public void sendAbort()
        {
            if (port.IsOpen)
                Write(":TRIG:ABOR\n");
        }

        public void sendDown()
        {
            if (port.IsOpen)
                Write(":TRIG:IMM DOWN\n");
        }

        public void sendUp()
        {
            if (port.IsOpen)
                Write(":TRIG:IMM UP\n");
        }

        public bool getStatus()
        {
            if (port.IsOpen)
            {
                Write(":STAT:OPER:COND?\n");
                var result = ReceiveData();
                Debug.WriteLine(result);
                return (int.Parse(result) & 0b10) == 2;
            }
            return false;
        }

        public List<FRAData> getData(int start, int end)
        {
            if (port.IsOpen)
            {
                Debug.WriteLine($":DATA? MEAS,{start},{end}\n");
                Write($":DATA? MEAS,{start},{end}\n");
                var result = ReceiveData();
                string[] strArray = result.Split(',');
                List<FRAData> list = new List<FRAData>();
                for (int i = 0; i < strArray.Length / 3; i++)
                {
                    if (i * 3 + 2 <= strArray.Length)
                    {
                        double freq = 0, x = 0, y1 = 0;
                        bool res = double.TryParse(strArray[i * 3], out freq);
                        if (res) res = double.TryParse(strArray[i * 3 + 1], out x);
                        if (res) res = double.TryParse(strArray[i * 3 + 2], out y1);
                        if (res) list.Add(new FRAData(freq, x, y1));
                    }
                }
                return list;
            }
            return null;
        }

        public int getDataCount()
        {
            if (port.IsOpen)
            {
                Write(":DATA:POIN? MEAS\n");
                var result = ReceiveData();
                Debug.WriteLine(result);
                return int.Parse(result);
            }
            return 0;
        }

        public string ReceiveData(int timeout = int.MaxValue)
        {
            byte[] buffer = new byte[1];
            string ret = string.Empty;

            int deftimeout = port.ReadTimeout;
            if (timeout != int.MaxValue) port.ReadTimeout = timeout * 1000;
            while (true)
            {
                try { port.BaseStream.Read(buffer, 0, 1); }
                catch (TimeoutException) { break; }
                ret += port.Encoding.GetString(buffer);

                if (ret.EndsWith(port.NewLine))
                {
                    // Truncate the line ending
                    port.ReadTimeout = deftimeout;
                    return ret.Substring(0, ret.Length - port.NewLine.Length);
                }
            }
            port.ReadTimeout = deftimeout;
            return string.Empty;
        }

        public void Write(string str)
        {
            var buffer = Encoding.UTF8.GetBytes(str);
            port.BaseStream.Write(buffer, 0, buffer.Length);
        }

        public class ConnectCommand : ICommand
        {
            public event EventHandler CanExecuteChanged;
            private MainWindowViewModel vm;
            public ConnectCommand(MainWindowViewModel viewModel)
            {
                vm = viewModel;
            }

            public bool CanExecute(object parameter) { return true; }

            public void Execute(object parameter)
            {
                FRA51615 fra51615 = vm.fra51615;
                if (fra51615.PortName != null)
                {
                    if (fra51615.IsOpen)
                    {
                        fra51615.Close();
                        if (!fra51615.IsOpen)
                        {
                            vm.FRA51615StatusText = "接続";
                            vm.MinFreq = 0;
                            vm.OnPropertyChanged(nameof(vm.MinFreq));
                            vm.MaxFreq = 0;
                            vm.OnPropertyChanged(nameof(vm.MaxFreq));
                            vm.Points = 0;
                            vm.OnPropertyChanged(nameof(vm.Points));
                            vm.Accumulation = 0;
                            vm.OnPropertyChanged(nameof(vm.Accumulation));
                            vm.ACDCBackground = false;
                            vm.OnPropertyChanged(nameof(vm.ACDCBackground));
                        }
                    }
                    else
                    {
                        fra51615.Open();
                        if (fra51615.IsOpen && fra51615.getIdentification())
                        {
                            vm.FRA51615StatusText = "解放";
                            try
                            {
                                vm.MinFreq = fra51615.getMinFreq();
                                vm.OnPropertyChanged(nameof(vm.MinFreq));
                                vm.MaxFreq = fra51615.getMaxFreq();
                                vm.OnPropertyChanged(nameof(vm.MaxFreq));
                                vm.Points = fra51615.getPoints();
                                vm.OnPropertyChanged(nameof(vm.Points));
                                vm.Accumulation = fra51615.getAccumulation();
                                vm.OnPropertyChanged(nameof(vm.Accumulation));
                                vm.ACDCBackground = fra51615.getACDC();
                                vm.OnPropertyChanged(nameof(vm.ACDCBackground));
                            }
                            catch (Exception) { }
                        }
                        else
                        {
                            fra51615.Close();
                            MessageBox.Show("ポートが間違っています。", "エラー");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("ポートを選択してください。", "エラー");
                }
            }
        }

        public class ACDCCommand : ICommand
        {
            public event EventHandler CanExecuteChanged;
            private MainWindowViewModel vm;
            public ACDCCommand(MainWindowViewModel viewModel)
            {
                vm = viewModel;
            }

            public bool CanExecute(object parameter) { return true; }

            public void Execute(object parameter)
            {
                FRA51615 fra51615 = vm.fra51615;
                if (fra51615.port.IsOpen)
                {
                    bool status = fra51615.getACDC();
                    var res = fra51615.setACDC(!status);
                    vm.ACDCBackground = res;
                }
                else
                    MessageBox.Show("接続されていません。", "エラー");

            }
        }

        public class UpCommand : ICommand
        {
            public event EventHandler CanExecuteChanged;
            private MainWindowViewModel vm;
            public UpCommand(MainWindowViewModel viewModel)
            {
                vm = viewModel;
            }

            public bool CanExecute(object parameter) { return true; }

            public void Execute(object parameter)
            {
                FRA51615 fra51615 = vm.fra51615;
                if (fra51615.port.IsOpen)
                {
                    if (string.IsNullOrEmpty(vm.SavePath))
                    {
                        MessageBox.Show("フォルダを選択してください", "エラー");
                        return;
                    }
                    _ = Measure(vm, MeasurementDirection.UP);
                }
                else
                    MessageBox.Show("接続されていません。", "エラー");
            }
        }

        public class DownCommand : ICommand
        {
            public event EventHandler CanExecuteChanged;
            private MainWindowViewModel vm;
            public DownCommand(MainWindowViewModel viewModel)
            {
                vm = viewModel;
            }

            public bool CanExecute(object parameter) { return true; }

            public void Execute(object parameter)
            {
                FRA51615 fra51615 = vm.fra51615;
                if (fra51615.port.IsOpen)
                {
                    if (string.IsNullOrEmpty(vm.SavePath))
                    {
                        MessageBox.Show("フォルダを選択してください", "エラー");
                        return;
                    }
                    _ = Measure(vm, MeasurementDirection.DOWN);
                }
                else
                    MessageBox.Show("接続されていません。", "エラー");
            }
        }

        public class AbortCommand : ICommand
        {
            public event EventHandler CanExecuteChanged;
            private MainWindowViewModel vm;
            public AbortCommand(MainWindowViewModel viewModel)
            {
                vm = viewModel;
            }

            public bool CanExecute(object parameter) { return true; }

            public void Execute(object parameter)
            {
                FRA51615 fra51615 = vm.fra51615;
                if (fra51615.port.IsOpen)
                    fra51615.sendAbort();
                else
                    MessageBox.Show("接続されていません。", "エラー");
            }
        }

        public enum MeasurementDirection { UP, DOWN }
        public static async Task Measure(MainWindowViewModel vm, MeasurementDirection dirction)
        {
            FRA51615 fra51615 = vm.fra51615;
            int count = 0, endNum = 0, nextEndNum;
            if (!fra51615.getStatus())
            {
                // グラフ表示をR,-Xに変更
                fra51615.setXY();
                // 測定コマンドを送信
                if (dirction == MeasurementDirection.DOWN) fra51615.sendDown(); else fra51615.sendUp();
                // 測定開始まで待機
                while (!fra51615.getStatus()) { if (count > 10) break; count++; }
                // 測定開始できた場合
                if (fra51615.getStatus())
                {
                    List<FRAData> data;
                    // 初期処理
                    if (dirction == MeasurementDirection.DOWN) vm.DownBackground = true; else vm.UpBackground = true;
                    // CSVヘッダー書き換え
                    string name = $"{vm.SeriesName}_{vm.CumulativeNumber}";
                    string path = $"{vm.SavePath}\\{name}.csv";
                    string filename = $"{name}.csv";
                    string csvStr = "Frequency,R,X,Z,Theta\r\n";
                    try { File.AppendAllText(path, csvStr); }
                    catch (Exception) { throw; }
                    // ファイルリスト更新・ファイル選択
                    vm.SavePath = vm.SavePath;
                    foreach (var file in vm.FileList)
                        if (file.FileName == filename) vm.SelectedFile = file;
                    // データ取得が終わるまでループ
                    while ((nextEndNum = fra51615.getDataCount()) != endNum || fra51615.getStatus())
                    {
                        // 新規取得データがあった場合
                        if (endNum != nextEndNum)
                        {
                            // データ処理
                            data = fra51615.getData(endNum, nextEndNum - endNum);
                            Debug.WriteLine($"Write {endNum} {nextEndNum - endNum}");
                            endNum = nextEndNum;
                            // 書き込み処理
                            csvStr = string.Empty;
                            foreach (var d in data)
                                csvStr += $"{d.F},{d.R},{d.X},{d.Z},{d.Theta}\r\n";
                            try { File.AppendAllText(path, csvStr); }
                            catch (Exception) { throw; }
                            // グラフ更新
                            if (vm.SelectedFile?.FileName == filename)
                                vm.SelectedFile = vm.SelectedFile;
                        }
                        // 待機
                        await Task.Delay(500);
                    }
                    // 終了処理
                    vm.chartA.getScreenShot(name);
                    vm.chartB.getScreenShot(name);
                    vm.RefreshCumulativeNumber();
                    vm.DownBackground = false;
                    vm.UpBackground = false;
                }
            }
        }
    }
}
