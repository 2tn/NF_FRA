using System.Windows;
using System.IO.Ports;

namespace NF_FRA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public static SerialPort port;
        public MainWindow()
        {
            InitializeComponent();
            var vm = DataContext as MainWindowViewModel;
            vm.chartA = new SquaredChart(vm, chartA_view);
            vm.chartB = new CombinationChart(vm, chartB_view);
        }
    }
}
