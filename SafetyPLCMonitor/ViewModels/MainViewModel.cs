// SafetyPLCMonitor/ViewModels/MainViewModel.cs
using System;
using SafetyPLCMonitor.Models;
using System.Collections.ObjectModel;
using SafetyPLCMonitor.Core.Events;
using Syncfusion.UI.Xaml.Diagram;
using SafetyPLCMonitor.Utilities;
using EasyModbus;
using SafetyPLCMonitor.Services.Data;
using Microsoft.Extensions.DependencyInjection;
using SafetyPLCMonitor.Core.Interfaces;

namespace SafetyPLCMonitor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ConnectionViewModel _connectionViewModel;
        private IOMonitorViewModel _ioMonitorViewModel;
        private LogicDiagramViewModel _logicDiagramViewModel;
        private EventHistoryViewModel _eventHistoryViewModel;
        private SettingsViewModel _settingsViewModel;
        private ViewModelBase _currentViewModel;

        public ConnectionViewModel ConnectionViewModel
        {
            get => _connectionViewModel;
            private set => SetProperty(ref _connectionViewModel, value);
        }

        public IOMonitorViewModel IOMonitorViewModel
        {
            get => _ioMonitorViewModel;
            private set => SetProperty(ref _ioMonitorViewModel, value);
        }

        public LogicDiagramViewModel LogicDiagramViewModel
        {
            get => _logicDiagramViewModel;
            private set => SetProperty(ref _logicDiagramViewModel, value);
        }

        public EventHistoryViewModel EventHistoryViewModel
        {
            get => _eventHistoryViewModel;
            private set => SetProperty(ref _eventHistoryViewModel, value);
        }

        public SettingsViewModel SettingsViewModel
        {
            get => _settingsViewModel;
            private set => SetProperty(ref _settingsViewModel, value);
        }

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        private bool _isPollingActive;
        public bool IsPollingActive
        {
            get => _isPollingActive;
            set => SetProperty(ref _isPollingActive, value);
        }

        public RelayCommand NavigateConnectionCommand { get; }
        public RelayCommand NavigateIOMonitorCommand { get; }
        public RelayCommand NavigateLogicDiagramCommand { get; }
        public RelayCommand NavigateEventHistoryCommand { get; }
        public RelayCommand NavigateSettingsCommand { get; }

        public MainViewModel(IModbusClient modbusClient, IDataPollingManager pollingManager, IONameManager ioNameManager)
        {
            // 각 화면별 ViewModel 초기화
            // 각 화면별 ViewModel 초기화
            ConnectionViewModel = new ConnectionViewModel(modbusClient, pollingManager);
            IOMonitorViewModel = new IOMonitorViewModel(modbusClient, pollingManager, ioNameManager);
            LogicDiagramViewModel = new LogicDiagramViewModel();
            EventHistoryViewModel = new EventHistoryViewModel();
            SettingsViewModel = new SettingsViewModel();

            // 초기 화면 설정
            CurrentViewModel = ConnectionViewModel;

            // 명령 초기화
            NavigateConnectionCommand = new RelayCommand(_ => NavigateToConnection());
            NavigateIOMonitorCommand = new RelayCommand(_ => NavigateToIOMonitor());
            NavigateLogicDiagramCommand = new RelayCommand(_ => NavigateToLogicDiagram());
            NavigateEventHistoryCommand = new RelayCommand(_ => NavigateToEventHistory());
            NavigateSettingsCommand = new RelayCommand(_ => NavigateToSettings());

            // 이벤트 구독
            ConnectionViewModel.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        // 기본 생성자 (디자인 시간 및 MainWindow에서 사용)
        public MainViewModel() : this(
            App.ServiceProvider.GetService<IModbusClient>(),
            App.ServiceProvider.GetService<IDataPollingManager>(),
            App.ServiceProvider.GetService<IONameManager>())
        {
        }

        private void NavigateToConnection()
        {
            CurrentViewModel = ConnectionViewModel;
        }

        private void NavigateToIOMonitor()
        {
            CurrentViewModel = IOMonitorViewModel;
        }

        private void NavigateToLogicDiagram()
        {
            CurrentViewModel = LogicDiagramViewModel;
        }

        private void NavigateToEventHistory()
        {
            CurrentViewModel = EventHistoryViewModel;
        }

        private void NavigateToSettings()
        {
            CurrentViewModel = SettingsViewModel;
        }

        private void OnConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            IsConnected = e.IsConnected;
            StatusMessage = e.IsConnected ?
                $"연결됨: {e.IpAddress}:{e.Port}" :
                "연결 안됨";
        }
    }
}