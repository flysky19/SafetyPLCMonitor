using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using NLog;
using SafetyPLCMonitor.Core.Events;
using SafetyPLCMonitor.Core.Interfaces;
using SafetyPLCMonitor.Core.Models;
using Timer = System.Timers.Timer;

namespace SafetyPLCMonitor.Communication
{
    public class DataPollingManager : IDataPollingManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IModbusClient _modbusClient;
        private readonly List<PollingTask> _pollingTasks = new List<PollingTask>();
        private readonly Dictionary<string, Timer> _taskTimers = new Dictionary<string, Timer>();
        private readonly object _lock = new object();
        private bool _isPollingActive;
        private bool _isDisposed;

        public bool IsPollingActive => _isPollingActive;
        public IReadOnlyList<PollingTask> PollingTasks => _pollingTasks.AsReadOnly();

        public event EventHandler<PollingTaskEventArgs> TaskExecuted;

        /// <summary>
        /// 데이터 폴링 관리자 생성
        /// </summary>
        /// <param name="modbusClient">Modbus 클라이언트</param>
        public DataPollingManager(IModbusClient modbusClient)
        {
            _modbusClient = modbusClient ?? throw new ArgumentNullException(nameof(modbusClient));

            _modbusClient.ConnectionStatusChanged += ModbusClient_ConnectionStatusChanged;
        }

        private void ModbusClient_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            if (e.IsConnected)
            {
                // 연결 성공 시 폴링 시작
                if (_isPollingActive)
                {
                    RestartTimers();
                }
            }
            else
            {
                // 연결 끊김 시 타이머 중지
                StopTimers();
            }
        }

        /// <summary>
        /// 폴링 시작 또는 재개
        /// </summary>
        public async Task StartPollingAsync()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DataPollingManager));
            }

            lock (_lock)
            {
                if (_isPollingActive)
                {
                    return; // 이미 활성화됨
                }

                _isPollingActive = true;

                // 모든 타이머 시작
                RestartTimers();

                Logger.Info("데이터 폴링 시작됨");
            }

            // 연결 확인
            if (!_modbusClient.IsConnected)
            {
                await _modbusClient.ConnectAsync();
            }
        }

        /// <summary>
        /// 폴링 일시 중지
        /// </summary>
        public void PausePolling()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DataPollingManager));
            }

            lock (_lock)
            {
                if (!_isPollingActive)
                {
                    return; // 이미 중지됨
                }

                _isPollingActive = false;

                // 모든 타이머 중지
                StopTimers();

                Logger.Info("데이터 폴링 일시 중지됨");
            }
        }

        /// <summary>
        /// 폴링 태스크 추가
        /// </summary>
        /// <param name="task">추가할 폴링 태스크</param>
        public void AddPollingTask(PollingTask task)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DataPollingManager));
            }

            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            lock (_lock)
            {
                // 중복 ID 검사
                if (_pollingTasks.Any(t => t.Id == task.Id))
                {
                    throw new ArgumentException($"이미 존재하는 태스크 ID: {task.Id}");
                }

                _pollingTasks.Add(task);

                Logger.Debug($"폴링 태스크 추가됨: {task}");

                // 폴링 활성화 상태라면 타이머 생성 및 시작
                if (_isPollingActive && _modbusClient.IsConnected && task.IsEnabled)
                {
                    CreateAndStartTimer(task);
                }
            }
        }

        /// <summary>
        /// 폴링 태스크 제거
        /// </summary>
        /// <param name="taskId">제거할 태스크 ID</param>
        /// <returns>제거 성공 여부</returns>
        public bool RemovePollingTask(string taskId)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DataPollingManager));
            }

            lock (_lock)
            {
                // 태스크 찾기
                var task = _pollingTasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null)
                {
                    return false;
                }

                // 타이머 중지 및 삭제
                StopAndRemoveTimer(taskId);

                // 태스크 제거
                _pollingTasks.Remove(task);

                Logger.Debug($"폴링 태스크 제거됨: {task}");

                return true;
            }
        }

        /// <summary>
        /// 모든 폴링 태스크 제거
        /// </summary>
        public void ClearPollingTasks()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DataPollingManager));
            }

            lock (_lock)
            {
                // 모든 타이머 중지 및 삭제
                StopTimers();
                _taskTimers.Clear();

                // 모든 태스크 제거
                _pollingTasks.Clear();

                Logger.Debug("모든 폴링 태스크 제거됨");
            }
        }

        /// <summary>
        /// 특정 태스크 즉시 실행
        /// </summary>
        /// <param name="taskId">실행할 태스크 ID</param>
        /// <returns>실행 성공 여부</returns>
        public async Task<bool> ExecuteTaskImmediatelyAsync(string taskId)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DataPollingManager));
            }

            PollingTask task;

            lock (_lock)
            {
                task = _pollingTasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null)
                {
                    return false;
                }
            }

            return await ExecutePollingTaskAsync(task);
        }

        private void RestartTimers()
        {
            StopTimers();

            if (!_modbusClient.IsConnected)
            {
                return;
            }

            // 활성화된 태스크에 대해 타이머 생성 및 시작
            foreach (var task in _pollingTasks.Where(t => t.IsEnabled))
            {
                CreateAndStartTimer(task);
            }
        }

        private void StopTimers()
        {
            foreach (var timer in _taskTimers.Values)
            {
                timer.Stop();
                timer.Dispose();
            }

            _taskTimers.Clear();
        }

        private void CreateAndStartTimer(PollingTask task)
        {
            // 이미 타이머가 있다면 중지 및 제거
            StopAndRemoveTimer(task.Id);

            // 새 타이머 생성
            var timer = new Timer(task.PollingInterval);
            timer.Elapsed += async (sender, e) => await OnTimerElapsed(task);
            timer.AutoReset = true;

            _taskTimers[task.Id] = timer;

            // 타이머 시작
            timer.Start();

            Logger.Trace($"폴링 타이머 시작됨: {task}");
        }

        private void StopAndRemoveTimer(string taskId)
        {
            if (_taskTimers.TryGetValue(taskId, out var timer))
            {
                timer.Stop();
                timer.Dispose();
                _taskTimers.Remove(taskId);
            }
        }

        private async Task OnTimerElapsed(PollingTask task)
        {
            if (!_isPollingActive || !_modbusClient.IsConnected)
            {
                return;
            }

            await ExecutePollingTaskAsync(task);
        }

        private async Task<bool> ExecutePollingTaskAsync(PollingTask task)
        {
            if (!_modbusClient.IsConnected)
            {
                return false;
            }

            try
            {
                Logger.Trace($"폴링 태스크 실행: {task}");

                bool success = false;
                object data = null;

                switch (task.RegisterType)
                {
                    case ModbusRegisterType.DiscreteInput:
                        bool[] discreteInputs = await _modbusClient.ReadDiscreteInputsAsync(task.StartAddress, task.Length);
                        success = discreteInputs.Length > 0;
                        data = discreteInputs;
                        break;

                    case ModbusRegisterType.Coil:
                        bool[] coils = await _modbusClient.ReadCoilsAsync(task.StartAddress, task.Length);
                        success = coils.Length > 0;
                        data = coils;
                        break;

                    case ModbusRegisterType.InputRegister:
                        int[] inputRegisters = await _modbusClient.ReadInputRegistersAsync(task.StartAddress, task.Length);
                        success = inputRegisters.Length > 0;
                        data = inputRegisters;
                        break;

                    case ModbusRegisterType.HoldingRegister:
                        int[] holdingRegisters = await _modbusClient.ReadHoldingRegistersAsync(task.StartAddress, task.Length);
                        success = holdingRegisters.Length > 0;
                        data = holdingRegisters;
                        break;
                }

                // 태스크 실행 결과 업데이트
                task.LastExecutionTime = DateTime.Now;
                task.LastExecutionSuccess = success;

                // 이벤트 발생
                OnTaskExecuted(new PollingTaskEventArgs(task, success, data));

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"폴링 태스크 실행 중 오류 발생: {task}");

                // 태스크 실행 결과 업데이트
                task.LastExecutionTime = DateTime.Now;
                task.LastExecutionSuccess = false;

                // 이벤트 발생
                OnTaskExecuted(new PollingTaskEventArgs(task, false, null, ex.Message));

                return false;
            }
        }

        private void OnTaskExecuted(PollingTaskEventArgs e)
        {
            TaskExecuted?.Invoke(this, e);
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            try
            {
                StopTimers();

                if (_modbusClient != null)
                {
                    _modbusClient.ConnectionStatusChanged -= ModbusClient_ConnectionStatusChanged;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Dispose 중 예외 발생");
            }
        }
    }

    //public class PollingTaskEventArgs : EventArgs
    //{
    //    /// <summary>
    //    /// 폴링 태스크
    //    /// </summary>
    //    public PollingTask Task { get; }

    //    /// <summary>
    //    /// 실행 성공 여부
    //    /// </summary>
    //    public bool Success { get; }

    //    /// <summary>
    //    /// 수신된 데이터
    //    /// </summary>
    //    public object Data { get; }

    //    /// <summary>
    //    /// 오류 메시지 (실패 시)
    //    /// </summary>
    //    public string ErrorMessage { get; }

    //    /// <summary>
    //    /// 이벤트 발생 시간
    //    /// </summary>
    //    public DateTime Timestamp { get; }

    //    public PollingTaskEventArgs(PollingTask task, bool success, object data, string errorMessage = null)
    //    {
    //        Task = task;
    //        Success = success;
    //        Data = data;
    //        ErrorMessage = errorMessage;
    //        Timestamp = DateTime.Now;
    //    }
    //}
}