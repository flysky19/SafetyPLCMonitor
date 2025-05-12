// SafetyPLCMonitor/ViewModels/ConnectionViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SafetyPLCMonitor.Communication;
using SafetyPLCMonitor.Core.Events;
using SafetyPLCMonitor.Core.Interfaces;
using SafetyPLCMonitor.Core.Models;
using SafetyPLCMonitor.Models;
using SafetyPLCMonitor.Communication;
using SafetyPLCMonitor.Utilities;
using RelayCommand = SafetyPLCMonitor.Utilities.RelayCommand;
using Microsoft.Extensions.DependencyInjection;

namespace SafetyPLCMonitor.ViewModels
{
    public class ConnectionViewModel : ViewModelBase
    {
        private readonly IModbusClient _modbusClient;
        private readonly IDataPollingManager _pollingManager;
        private readonly PlcInfoManager _plcInfoManager;

        private string _ipAddress = "127.0.0.1";
        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        private string _port = "502";
        public string Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
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

        private string _connectionStatus = "연결 안됨";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        private PlcDevice _currentDevice;
        public PlcDevice CurrentDevice
        {
            get => _currentDevice;
            set => SetProperty(ref _currentDevice, value);
        }

        public ObservableCollection<PollingTask> PollingTasks { get; } = new ObservableCollection<PollingTask>();

        public RelayCommand ConnectCommand { get; }
        public RelayCommand DisconnectCommand { get; }
        public RelayCommand StartPollingCommand { get; }
        public RelayCommand StopPollingCommand { get; }
        public RelayCommand AddPollingTaskCommand { get; }
        public RelayCommand<string> RemovePollingTaskCommand { get; }

        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        public ConnectionViewModel(IModbusClient modbusClient, IDataPollingManager pollingManager)
        {
            _modbusClient = modbusClient ?? throw new ArgumentNullException(nameof(modbusClient));
            _pollingManager = pollingManager ?? throw new ArgumentNullException(nameof(pollingManager));
            _plcInfoManager = new PlcInfoManager(_modbusClient);

            // 현재 장치 초기화
            CurrentDevice = new PlcDevice
            {
                Name = "PNOZmulti",
                IpAddress = IpAddress,
                Port = int.Parse(Port)
            };

            // 이벤트 구독
            _modbusClient.ConnectionStatusChanged += ModbusClient_ConnectionStatusChanged;
            _pollingManager.TaskExecuted += PollingManager_TaskExecuted;

            // 명령 초기화
            ConnectCommand = new RelayCommand(_ => ConnectAsync(), _ => !IsConnected);
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsConnected);
            StartPollingCommand = new RelayCommand(_ => StartPollingAsync(), _ => IsConnected && !IsPollingActive);
            StopPollingCommand = new RelayCommand(_ => StopPolling(), _ => IsConnected && IsPollingActive);
            AddPollingTaskCommand = new RelayCommand(_ => AddPollingTask());
            RemovePollingTaskCommand = new RelayCommand<string>(taskId => RemovePollingTask(taskId));

            // 샘플 폴링 태스크 추가
            AddSamplePollingTasks();
        }

        // 기본 생성자 (디자인 시간 지원용)
        public ConnectionViewModel() : this(
            App.ServiceProvider.GetService<IModbusClient>(),
            App.ServiceProvider.GetService<IDataPollingManager>())
        {
        }

        private async Task ConnectAsync()
        {
            try
            {
                // IP 주소 및 포트 업데이트
                CurrentDevice.IpAddress = IpAddress;
                CurrentDevice.Port = int.Parse(Port);

                // Modbus 클라이언트 설정 업데이트
                _modbusClient.Disconnect();
                _modbusClient.UpdateConnectionParameters(IpAddress, int.Parse(Port));

                // 연결 시도
                ConnectionStatus = "연결 중...";
                bool connected = await _modbusClient.ConnectAsync();

                if (connected)
                {
                    IsConnected = true;
                    ConnectionStatus = $"연결됨: {IpAddress}:{Port}";

                    // 장치 정보 읽기
                    var deviceInfo = await _plcInfoManager.ReadDeviceInfoAsync();
                    if (deviceInfo != null)
                    {
                        CurrentDevice.SerialNumber = deviceInfo.SerialNumber;
                        CurrentDevice.ProductType = deviceInfo.ProductCode.ToString();
                        CurrentDevice.FirmwareVersion = deviceInfo.FirmwareVersion;
                        CurrentDevice.LastConnected = DateTime.Now;
                    }

                    // 폴링 태스크 등록
                    _pollingManager.ClearPollingTasks();
                    foreach (var task in PollingTasks)
                    {
                        _pollingManager.AddPollingTask(task);
                    }
                }
                else
                {
                    IsConnected = false;
                    ConnectionStatus = "연결 실패";
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ConnectionStatus = $"오류: {ex.Message}";
            }

            // 명령 상태 갱신
            UpdateCommandStates();
        }

        private void Disconnect()
        {
            try
            {
                // 폴링 중지
                if (IsPollingActive)
                {
                    StopPolling();
                }

                // 연결 해제
                _modbusClient.Disconnect();
                IsConnected = false;
                ConnectionStatus = "연결 안됨";
                CurrentDevice.IsConnected = false;

                // 명령 상태 갱신
                UpdateCommandStates();
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"연결 해제 오류: {ex.Message}";
            }
        }

        private async Task StartPollingAsync()
        {
            try
            {
                if (!IsConnected)
                {
                    return;
                }

                await _pollingManager.StartPollingAsync();
                IsPollingActive = true;
                CurrentDevice.IsPollingActive = true;

                // 명령 상태 갱신
                UpdateCommandStates();
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"폴링 시작 오류: {ex.Message}";
            }
        }

        private void StopPolling()
        {
            try
            {
                _pollingManager.PausePolling();
                IsPollingActive = false;
                CurrentDevice.IsPollingActive = false;

                // 명령 상태 갱신
                UpdateCommandStates();
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"폴링 중지 오류: {ex.Message}";
            }
        }

        private void AddPollingTask()
        {
            // 간단한 구현을 위해 고정된 태스크 추가
            var task = new PollingTask
            {
                Name = $"Task {PollingTasks.Count + 1}",
                RegisterType = ModbusRegisterType.HoldingRegister,
                StartAddress = 100,
                Length = 10,
                PollingInterval = 1000
            };

            PollingTasks.Add(task);

            if (_pollingManager != null && IsConnected)
            {
                _pollingManager.AddPollingTask(task);
            }
        }

        private void RemovePollingTask(string taskId)
        {
            var task = PollingTasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                PollingTasks.Remove(task);

                if (_pollingManager != null)
                {
                    _pollingManager.RemovePollingTask(taskId);
                }
            }
        }

        private void AddSamplePollingTasks()
        {
            PollingTasks.Add(new PollingTask
            {
                Name = "디스크릿 입력",
                RegisterType = ModbusRegisterType.DiscreteInput,
                StartAddress = 0,
                Length = 8,
                PollingInterval = 1000
            });

            PollingTasks.Add(new PollingTask
            {
                Name = "입력 레지스터",
                RegisterType = ModbusRegisterType.InputRegister,
                StartAddress = 0,
                Length = 10,
                PollingInterval = 2000
            });

            PollingTasks.Add(new PollingTask
            {
                Name = "보유 레지스터",
                RegisterType = ModbusRegisterType.HoldingRegister,
                StartAddress = 0,
                Length = 5,
                PollingInterval = 3000
            });
        }

        private void ModbusClient_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            IsConnected = e.IsConnected;
            ConnectionStatus = e.IsConnected ?
                $"연결됨: {e.IpAddress}:{e.Port}" :
                "연결 안됨";

            if (CurrentDevice != null)
            {
                CurrentDevice.IsConnected = e.IsConnected;
                if (e.IsConnected)
                {
                    CurrentDevice.LastConnected = DateTime.Now;
                }
            }

            UpdateCommandStates();

            // 이벤트 발생
            ConnectionStatusChanged?.Invoke(this, e);
        }

        private void PollingManager_TaskExecuted(object sender, PollingTaskEventArgs e)
        {
            // UI 업데이트
            var task = PollingTasks.FirstOrDefault(t => t.Id == e.Task.Id);
            if (task != null)
            {
                task.LastExecutionTime = e.Task.LastExecutionTime;
                task.LastExecutionSuccess = e.Task.LastExecutionSuccess;
            }
        }

        private void UpdateCommandStates()
        {
            ConnectCommand.RaiseCanExecuteChanged();
            DisconnectCommand.RaiseCanExecuteChanged();
            StartPollingCommand.RaiseCanExecuteChanged();
            StopPollingCommand.RaiseCanExecuteChanged();
        }
    }
}