using System;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using Cursor = System.Windows.Forms.Cursor;
using Cursors = System.Windows.Forms.Cursors;

namespace NF_FRA
{
    public class CA5351
    {
        private SerialPort port;
        public CA5351()
        {
            port = new SerialPort();
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
                    ret = result.Split(",")[1] == "CA5351";
                return ret;
            }
            return false;
        }

        public bool getZeroCheck()
        {
            if (port.IsOpen)
            {
                Write(":INP:STAT?\n");
                var result = ReceiveData();
                return result == "1" ? true : false;
            }
            return false;
        }

        public bool setZeroCheck(bool value)
        {
            if (port.IsOpen)
                Write($":INP:STAT {(value ? "1" : "0")}\n");
            return getZeroCheck();
        }

        public int getGain()
        {
            if (port.IsOpen)
            {
                port.Write(":INP:GAIN?\n");
                var result = ReceiveData();
                int value;
                if (int.TryParse(result, out value))
                    return value + 2;
            }
            return 0;
        }

        public int setGain(int value)
        {
            if (port.IsOpen && value >= 3 && value <= 10)
            {
                port.Write($":INP:GAIN {value - 2}\n");
            }
            return getGain();
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
                Cursor cursor = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                CA5351 ca5351 = vm.ca5351;
                if (ca5351.PortName != null)
                {
                    if (ca5351.IsOpen)
                    {
                        ca5351.Close();
                        if (!ca5351.IsOpen)
                        {
                            vm.CA5351StatusText = "接続";
                            vm.Gain = 0;
                            vm.OnPropertyChanged(nameof(vm.Gain));
                            vm.ZeroCheckBackground = false;
                            vm.OnPropertyChanged(nameof(vm.ZeroCheckBackground));
                        }
                    }
                    else
                    {
                        ca5351.Open();
                        if (ca5351.IsOpen && ca5351.getIdentification())
                        {
                            vm.CA5351StatusText = "解放";
                            try
                            {
                                vm.Gain = ca5351.getGain();
                                vm.OnPropertyChanged(nameof(vm.Gain));
                                vm.ZeroCheckBackground = ca5351.getZeroCheck();
                                vm.OnPropertyChanged(nameof(vm.ZeroCheckBackground));
                            }
                            catch (Exception) { }
                        }
                        else
                        {
                            ca5351.Close();
                            MessageBox.Show("ポートが間違っています。", "エラー");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("ポートを選択してください。", "エラー");
                }
                Cursor.Current = cursor;
            }
        }

        public class ZeroCheckCommand : ICommand
        {
            public event EventHandler CanExecuteChanged;
            private MainWindowViewModel vm;
            public ZeroCheckCommand(MainWindowViewModel viewModel)
            {
                vm = viewModel;
            }

            public bool CanExecute(object parameter) { return true; }

            public void Execute(object parameter)
            {
                Cursor cursor = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                CA5351 ca5351 = vm.ca5351;
                if (ca5351.IsOpen)
                {
                    bool status = ca5351.getZeroCheck();
                    var res = ca5351.setZeroCheck(!status);
                    vm.ZeroCheckBackground = res;
                }
                else
                    MessageBox.Show("接続されていません。", "エラー");
                Cursor.Current = cursor;
            }
        }
    }
}
