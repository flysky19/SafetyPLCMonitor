// SafetyPLCMonitor/ViewModels/IOMonitorViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.Input;
using EasyModbus;
using Microsoft.Extensions.DependencyInjection;
using SafetyPLCMonitor.Core.Events;
using SafetyPLCMonitor.Core.Interfaces;
using SafetyPLCMonitor.Core.Models;
using SafetyPLCMonitor.Models;
using SafetyPLCMonitor.Services.Data;
using SafetyPLCMonitor.Utilities;
using SafetyPLCMonitor.Utilities.Extensions;
using SafetyPLCMonitor.Views.Dialogs;
using RelayCommand = SafetyPLCMonitor.Utilities.RelayCommand;

namespace SafetyPLCMonitor.ViewModels
{
    public class IOMonitorViewModel : ViewModelBase
    {
        private readonly IModbusClient _modbusClient;
        private readonly IDataPollingManager _pollingManager;
        private readonly IONameManager _ioNameManager;

        public ObservableCollection<IOPoint> IOPoints { get; } = new ObservableCollection<IOPoint>();

        

        private string _filterText = string.Empty;
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    FilterIOPoints();
                }
            }
        }

        private IOType _selectedIOType = IOType.DiscreteInput;
        public IOType SelectedIOType
        {
            get => _selectedIOType;
            set
            {
                if (SetProperty(ref _selectedIOType, value))
                {
                    LoadIOPoints();
                }
            }
        }

        private IOPoint _selectedIOPoint;
        public IOPoint SelectedIOPoint
        {
            get => _selectedIOPoint;
            set => SetProperty(ref _selectedIOPoint, value);
        }

        private bool _showOnlyActive;
        public bool ShowOnlyActive
        {
            get => _showOnlyActive;
            set
            {
                if (SetProperty(ref _showOnlyActive, value))
                {
                    FilterIOPoints();
                }
            }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand<IOPoint> EditIONameCommand { get; }
        public RelayCommand ExportCommand { get; }

        public RelayCommand ImportCommand { get; }

        private readonly object _ioPointsLock = new object();
        private CollectionViewSource _ioPointsViewSource;
        public ICollectionView IOPointsView => _ioPointsViewSource.View;

        public IOMonitorViewModel(IModbusClient modbusClient, IDataPollingManager pollingManager, IONameManager ioNameManager)
        {
            _modbusClient = modbusClient;
            _pollingManager = pollingManager;
            _ioNameManager = ioNameManager;

            // 컬렉션 뷰 소스 초기화
            _ioPointsViewSource = new CollectionViewSource { Source = IOPoints };

            // 명령 초기화
            RefreshCommand = new RelayCommand(_ => LoadIOPoints());
            EditIONameCommand = new RelayCommand<IOPoint>(point => EditIOName(point));
            ExportCommand = new RelayCommand(_ => ExportIOPoints());

            ImportCommand = new RelayCommand(_ => ImportIOPoints());

            _ioNameManager.IONamesChanged += (s, e) => LoadIOPoints();

            // 이벤트 구독
            if (_pollingManager != null)
            {
                _pollingManager.TaskExecuted += PollingManager_TaskExecuted;
            }

            // 초기 데이터 로드
            LoadIOPoints();
        }

        // 기본 생성자 (디자인 시간 지원용)
        public IOMonitorViewModel() : this(
            App.ServiceProvider.GetService<IModbusClient>(),
            App.ServiceProvider.GetService<IDataPollingManager>(),
            App.ServiceProvider.GetService<IONameManager>())
        {
        }

        private void LoadIOPoints()
        {
            lock (_ioPointsLock)
            {
                IOPoints.Clear();

                // 선택된 I/O 타입에 따라 I/O 포인트 생성
                switch (SelectedIOType)
                {
                    case IOType.DiscreteInput:
                        LoadDiscreteInputs();
                        break;
                    case IOType.DiscreteOutput:
                        LoadDiscreteOutputs();
                        break;
                    case IOType.VirtualInput:
                        LoadVirtualInputs();
                        break;
                    case IOType.VirtualOutput:
                        LoadVirtualOutputs();
                        break;
                    case IOType.InputRegister:
                        LoadInputRegisters();
                        break;
                    case IOType.HoldingRegister:
                        LoadHoldingRegisters();
                        break;
                }

                FilterIOPoints();
            }
        }

        private void LoadDiscreteInputs()
        {
            // 기본적으로 16개의 디스크릿 입력 추가
            for (int i = 0; i < 16; i++)
            {
                var point = new IOPoint
                {
                    Type = IOType.DiscreteInput,
                    Address = i,
                    Name = _ioNameManager.GetIOName(IOType.DiscreteInput, i) ?? $"Input {i}",
                    Description = "디스크릿 입력",
                    IsActive = false,
                    LastUpdateTime = DateTime.Now
                };

                IOPoints.Add(point);
            }
        }

        private void LoadDiscreteOutputs()
        {
            // 기본적으로 16개의 디스크릿 출력 추가
            for (int i = 0; i < 16; i++)
            {
                var point = new IOPoint
                {
                    Type = IOType.DiscreteOutput,
                    Address = i,
                    Name = _ioNameManager.GetIOName(IOType.DiscreteOutput, i) ?? $"Output {i}",
                    Description = "디스크릿 출력",
                    IsActive = false,
                    LastUpdateTime = DateTime.Now
                };

                IOPoints.Add(point);
            }
        }

        private void LoadVirtualInputs()
        {
            // 가상 입력 추가
            for (int i = 0; i < 32; i++)
            {
                var point = new IOPoint
                {
                    Type = IOType.VirtualInput,
                    Address = i,
                    Name = _ioNameManager.GetIOName(IOType.VirtualInput, i) ?? $"VInput {i}",
                    Description = "가상 입력",
                    IsActive = false,
                    LastUpdateTime = DateTime.Now
                };

                IOPoints.Add(point);
            }
        }

        private void LoadVirtualOutputs()
        {
            // 가상 출력 추가
            for (int i = 0; i < 32; i++)
            {
                var point = new IOPoint
                {
                    Type = IOType.VirtualOutput,
                    Address = i,
                    Name = _ioNameManager.GetIOName(IOType.VirtualOutput, i) ?? $"VOutput {i}",
                    Description = "가상 출력",
                    IsActive = false,
                    LastUpdateTime = DateTime.Now
                };

                IOPoints.Add(point);
            }
        }

        private void LoadInputRegisters()
        {
            // 입력 레지스터 추가
            for (int i = 0; i < 32; i++)
            {
                var point = new IOPoint
                {
                    Type = IOType.InputRegister,
                    Address = i,
                    Name = _ioNameManager.GetIOName(IOType.InputRegister, i) ?? $"InReg {i}",
                    Description = "입력 레지스터",
                    Value = 0,
                    LastUpdateTime = DateTime.Now
                };

                IOPoints.Add(point);
            }
        }

        private void LoadHoldingRegisters()
        {
            // 보유 레지스터 추가
            for (int i = 0; i < 32; i++)
            {
                var point = new IOPoint
                {
                    Type = IOType.HoldingRegister,
                    Address = i,
                    Name = _ioNameManager.GetIOName(IOType.HoldingRegister, i) ?? $"HoldReg {i}",
                    Description = "보유 레지스터",
                    Value = 0,
                    LastUpdateTime = DateTime.Now
                };

                IOPoints.Add(point);
            }
        }

        private void PollingManager_TaskExecuted(object sender, PollingTaskEventArgs e)
        {
            if (!e.Success || e.Data == null)
                return;

            // 폴링된 데이터로 I/O 포인트 업데이트
            bool updateNeeded = false;

            lock (_ioPointsLock)
            {
                if (e.Task.RegisterType == ModbusRegisterType.DiscreteInput && SelectedIOType == IOType.DiscreteInput)
                {
                    if (e.Data is bool[] discreteInputs)
                    {
                        updateNeeded = UpdateDiscreteIOPoints(IOType.DiscreteInput, e.Task.StartAddress, discreteInputs);
                    }
                }
                else if (e.Task.RegisterType == ModbusRegisterType.Coil && SelectedIOType == IOType.DiscreteOutput)
                {
                    if (e.Data is bool[] coils)
                    {
                        updateNeeded = UpdateDiscreteIOPoints(IOType.DiscreteOutput, e.Task.StartAddress, coils);
                    }
                }
                else if (e.Task.RegisterType == ModbusRegisterType.InputRegister && SelectedIOType == IOType.InputRegister)
                {
                    if (e.Data is int[] inputRegisters)
                    {
                        updateNeeded = UpdateRegisterIOPoints(IOType.InputRegister, e.Task.StartAddress, inputRegisters);
                    }
                }
                else if (e.Task.RegisterType == ModbusRegisterType.HoldingRegister && SelectedIOType == IOType.HoldingRegister)
                {
                    if (e.Data is int[] holdingRegisters)
                    {
                        updateNeeded = UpdateRegisterIOPoints(IOType.HoldingRegister, e.Task.StartAddress, holdingRegisters);
                    }
                }
            }

            if (updateNeeded)
            {
                FilterIOPoints();
            }
        }

        private bool UpdateDiscreteIOPoints(IOType type, int startAddress, bool[] values)
        {
            bool updated = false;

            for (int i = 0; i < values.Length; i++)
            {
                int address = startAddress + i;
                var point = IOPoints.FirstOrDefault(p => p.Type == type && p.Address == address);

                if (point != null)
                {
                    if (point.IsActive != values[i])
                    {
                        point.IsActive = values[i];
                        point.LastUpdateTime = DateTime.Now;
                        updated = true;
                    }
                }
            }

            return updated;
        }

        private bool UpdateRegisterIOPoints(IOType type, int startAddress, int[] values)
        {
            bool updated = false;

            for (int i = 0; i < values.Length; i++)
            {
                int address = startAddress + i;
                var point = IOPoints.FirstOrDefault(p => p.Type == type && p.Address == address);

                if (point != null)
                {
                    if (!Equals(point.Value, values[i]))
                    {
                        point.Value = values[i];
                        point.LastUpdateTime = DateTime.Now;
                        updated = true;
                    }
                }
            }

            return updated;
        }

        private void FilterIOPoints()
        {
            _ioPointsViewSource.View.Refresh();

            _ioPointsViewSource.View.Filter = item =>
            {
                if (!(item is IOPoint point))
                    return false;

                // 활성 상태 필터
                if (ShowOnlyActive)
                {
                    if (!point.IsActive && (point.Type == IOType.DiscreteInput ||
                                           point.Type == IOType.DiscreteOutput ||
                                           point.Type == IOType.VirtualInput ||
                                           point.Type == IOType.VirtualOutput))
                    {
                        return false;
                    }
                }

                // 텍스트 필터
                if (!string.IsNullOrEmpty(FilterText))
                {
                    if (!point.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) &&
                        !point.Description.Contains(FilterText, StringComparison.OrdinalIgnoreCase) &&
                        !point.Address.ToString().Contains(FilterText))
                    {
                        return false;
                    }
                }

                return true;
            };
        }

        private void EditIOName(IOPoint point)
        {
            if (point == null)
                return;

            // 다이얼로그를 통해 이름 편집
            if (IOConfigDialog.ShowDialog(Application.Current.MainWindow, point, out string name, out string description))
            {
                if (!string.IsNullOrEmpty(name))
                {
                    point.Name = name;
                    _ioNameManager.SetIOName(point.Type, point.Address, name);
                }

                if (!string.IsNullOrEmpty(description))
                {
                    point.Description = description;
                    _ioNameManager.SetIODescription(point.Type, point.Address, description);
                }
            }
        }

        // IOMonitorViewModel.cs에 내보내기/가져오기 메서드 추가
        private void ExportIOPoints()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "io_config",
                DefaultExt = ".json",
                Filter = "JSON 파일|*.json"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                _ioNameManager.ExportIONames(dialog.FileName);
            }
        }

        private void ImportIOPoints()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".json",
                Filter = "JSON 파일|*.json"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                _ioNameManager.ImportIONames(dialog.FileName);
                LoadIOPoints(); // 목록 다시 로드
            }
        }
    }
}