// SafetyPLCMonitor/MainWindow.xaml.cs
using System;
using System.Windows;
using System.Windows.Threading;
using SafetyPLCMonitor.ViewModels;

namespace SafetyPLCMonitor
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            // ViewModel 설정
            DataContext = new MainViewModel();

            // 시간 표시 타이머
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // 초기 시간 표시
            UpdateDateTime();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDateTime();
        }

        private void UpdateDateTime()
        {
            txtDateTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 리소스 해제
            _timer.Stop();

            // ViewModel의 Cleanup 메서드 호출
            if (DataContext is MainViewModel viewModel)
            {
                // 정리 작업
            }
        }
    }
}