using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DriverTool.Library.CmUi;

namespace DriverTool.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
            _timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 1) };
            _timer.Tick += TimerOnTick;
            _timer.Start();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _timer?.Stop();
        }

        private void TimerOnTick(object sender, EventArgs e)
        {
            if (this.DataContext is CmPackagesViewModel viewModel)
            {
                viewModel.RaiseCanExecuteChanged();
            }
        }
    }
}
