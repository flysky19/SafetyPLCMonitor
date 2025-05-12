using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using EasyModbus;
using NLog;
using SafetyPLCMonitor.Core.Events;
using SafetyPLCMonitor.Core.Interfaces;
using System.Collections.Generic; // 이 줄 추가

namespace SafetyPLCMonitor.Communication
{
    public class ModbusTcpClient : IModbusClient
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private string _ipAddress;
        private int _port;
        private readonly ModbusClient _modbusClient;
        private readonly object _lock = new object();
        private bool _isConnected;
        private Timer _connectionCheckTimer;
        private const int ConnectionCheckInterval = 5000; // 5초마다 연결 상태 확인
        private const int DefaultTimeout = 3000; // 3초 타임아웃

        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
        public event EventHandler<ModbusDataEventArgs> DataReceived;

        public bool IsConnected => _isConnected;
        public string IpAddress => _ipAddress;
        public int Port => _port;

        /// <summary>
        /// Modbus TCP 클라이언트 생성
        /// </summary>
        /// <param name="ipAddress">PLC IP 주소</param>
        /// <param name="port">PLC 포트 (기본값: 502)</param>
        public ModbusTcpClient(string ipAddress, int port = 502)
        {
            _ipAddress = ipAddress;
            _port = port;

            _modbusClient = new ModbusClient
            {
                IPAddress = ipAddress,
                Port = port,
                ConnectionTimeout = DefaultTimeout
            };

            // 자동 재연결 비활성화 (직접 관리)
            _modbusClient.LogFileFilename = null; // 로그는 NLog로 관리

            InitializeConnectionCheckTimer();
        }

        private void InitializeConnectionCheckTimer()
        {
            _connectionCheckTimer = new Timer(ConnectionCheckInterval);
            _connectionCheckTimer.Elapsed += ConnectionCheckTimer_Elapsed;
            _connectionCheckTimer.AutoReset = true;
        }

        private void ConnectionCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_isConnected)
            {
                return; // 이미 연결이 끊어진 상태면 확인 불필요
            }

            try
            {
                lock (_lock)
                {
                    if (_modbusClient.Connected)
                    {
                        // 연결 상태를 실제로 확인하기 위해 간단한 요청 시도
                        try
                        {
                            _modbusClient.ReadHoldingRegisters(0, 1);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(ex, "연결 확인 중 오류 발생, 연결 해제됨으로 상태 변경");
                            HandleConnectionLost();
                        }
                    }
                    else
                    {
                        HandleConnectionLost();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "연결 상태 확인 중 예외 발생");
                HandleConnectionLost();
            }
        }

        private void HandleConnectionLost()
        {
            if (_isConnected)
            {
                _isConnected = false;

                try
                {
                    // 클라이언트 내부 상태 재설정
                    _modbusClient.Disconnect();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "연결 해제 중 오류 발생");
                }

                // 이벤트 발생
                OnConnectionStatusChanged(false, "연결이 끊어졌습니다.");
            }
        }

        private void OnConnectionStatusChanged(bool isConnected, string errorMessage = null)
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(
                isConnected, _ipAddress, _port, errorMessage));

            Logger.Info($"연결 상태 변경: {(isConnected ? "연결됨" : "연결 해제됨")} - {_ipAddress}:{_port} {(errorMessage != null ? $"- {errorMessage}" : "")}");
        }

        private void OnDataReceived(ModbusDataEventArgs e)
        {
            DataReceived?.Invoke(this, e);
        }

        /// <summary>
        /// PLC에 연결합니다.
        /// </summary>
        /// <returns>연결 성공 여부</returns>
        public async Task<bool> ConnectAsync()
        {
            if (_isConnected)
            {
                return true; // 이미 연결됨
            }

            return await Task.Run(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        Logger.Debug($"PLC 연결 시도: {_ipAddress}:{_port}");

                        // 기존 연결이 있으면 해제
                        try
                        {
                            if (_modbusClient.Connected)
                            {
                                _modbusClient.Disconnect();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(ex, "기존 연결 해제 중 오류 발생");
                        }

                        // 새 연결 시도
                        _modbusClient.Connect();

                        _isConnected = _modbusClient.Connected;

                        if (_isConnected)
                        {
                            // 연결 성공 - 타이머 시작
                            if (!_connectionCheckTimer.Enabled)
                            {
                                _connectionCheckTimer.Start();
                            }

                            OnConnectionStatusChanged(true);
                        }
                        else
                        {
                            OnConnectionStatusChanged(false, "연결에 실패했습니다.");
                        }

                        return _isConnected;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"PLC 연결 중 오류 발생: {_ipAddress}:{_port}");
                    OnConnectionStatusChanged(false, ex.Message);
                    return false;
                }
            });
        }

        /// <summary>
        /// PLC 연결을 해제합니다.
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected)
            {
                return; // 이미 연결 해제됨
            }

            try
            {
                lock (_lock)
                {
                    if (_connectionCheckTimer.Enabled)
                    {
                        _connectionCheckTimer.Stop();
                    }

                    Logger.Debug($"PLC 연결 해제: {_ipAddress}:{_port}");
                    _modbusClient.Disconnect();
                    _isConnected = false;

                    OnConnectionStatusChanged(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"PLC 연결 해제 중 오류 발생: {_ipAddress}:{_port}");
                _isConnected = false;
                OnConnectionStatusChanged(false, ex.Message);
            }
        }

        /// <summary>
        /// 입력 레지스터를 읽습니다.
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="length">레지스터 개수</param>
        /// <returns>레지스터 값 배열</returns>
        public async Task<int[]> ReadInputRegistersAsync(int startAddress, int length)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        if (!EnsureConnected())
                        {
                            return new int[0];
                        }

                        Logger.Trace($"입력 레지스터 읽기: 시작 주소={startAddress}, 길이={length}");

                        int[] result = _modbusClient.ReadInputRegisters(startAddress, length);

                        OnDataReceived(new ModbusDataEventArgs(ModbusDataType.InputRegister, startAddress, result));

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex, $"입력 레지스터 읽기 실패: 시작 주소={startAddress}, 길이={length}");
                    return new int[0];
                }
            });
        }

        /// <summary>
        /// 보유 레지스터를 읽습니다.
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="length">레지스터 개수</param>
        /// <returns>레지스터 값 배열</returns>
        public async Task<int[]> ReadHoldingRegistersAsync(int startAddress, int length)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        if (!EnsureConnected())
                        {
                            return new int[0];
                        }

                        Logger.Trace($"보유 레지스터 읽기: 시작 주소={startAddress}, 길이={length}");

                        int[] result = _modbusClient.ReadHoldingRegisters(startAddress, length);

                        OnDataReceived(new ModbusDataEventArgs(ModbusDataType.HoldingRegister, startAddress, result));

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex, $"보유 레지스터 읽기 실패: 시작 주소={startAddress}, 길이={length}");
                    return new int[0];
                }
            });
        }

        /// <summary>
        /// 디스크릿 입력을 읽습니다.
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="length">입력 개수</param>
        /// <returns>입력 상태 배열</returns>
        public async Task<bool[]> ReadDiscreteInputsAsync(int startAddress, int length)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        if (!EnsureConnected())
                        {
                            return new bool[0];
                        }

                        Logger.Trace($"디스크릿 입력 읽기: 시작 주소={startAddress}, 길이={length}");

                        bool[] result = _modbusClient.ReadDiscreteInputs(startAddress, length);

                        OnDataReceived(new ModbusDataEventArgs(ModbusDataType.DiscreteInput, startAddress, result));

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex, $"디스크릿 입력 읽기 실패: 시작 주소={startAddress}, 길이={length}");
                    return new bool[0];
                }
            });
        }

        /// <summary>
        /// 코일 상태를 읽습니다.
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="length">코일 개수</param>
        /// <returns>코일 상태 배열</returns>
        public async Task<bool[]> ReadCoilsAsync(int startAddress, int length)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        if (!EnsureConnected())
                        {
                            return new bool[0];
                        }

                        Logger.Trace($"코일 읽기: 시작 주소={startAddress}, 길이={length}");

                        bool[] result = _modbusClient.ReadCoils(startAddress, length);

                        OnDataReceived(new ModbusDataEventArgs(ModbusDataType.Coil, startAddress, result));

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex, $"코일 읽기 실패: 시작 주소={startAddress}, 길이={length}");
                    return new bool[0];
                }
            });
        }

        /// <summary>
        /// 코일 상태를 씁니다.
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="values">코일 상태 배열</param>
        public async Task WriteCoilsAsync(int startAddress, bool[] values)
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        if (!EnsureConnected())
                        {
                            return;
                        }

                        Logger.Debug($"코일 쓰기: 시작 주소={startAddress}, 길이={values.Length}");

                        if (values.Length == 1)
                        {
                            _modbusClient.WriteSingleCoil(startAddress, values[0]);
                        }
                        else
                        {
                            _modbusClient.WriteMultipleCoils(startAddress, values);
                        }
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex, $"코일 쓰기 실패: 시작 주소={startAddress}, 길이={values.Length}");
                }
            });
        }

        /// <summary>
        /// 연결 파라미터(IP 주소, 포트)를 업데이트합니다.
        /// </summary>
        /// <param name="ipAddress">새 IP 주소</param>
        /// <param name="port">새 포트</param>
        public void UpdateConnectionParameters(string ipAddress, int port)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("연결이 활성화된 상태에서는 연결 파라미터를 변경할 수 없습니다. 먼저 연결을 해제하세요.");
            }

            _ipAddress = ipAddress;
            _port = port;

            // EasyModbus 클라이언트 설정 업데이트
            _modbusClient.IPAddress = ipAddress;
            _modbusClient.Port = port;

            Logger.Debug($"연결 파라미터 업데이트: IP={ipAddress}, Port={port}");
        }

        /// <summary>
        /// 보유 레지스터 값을 씁니다.
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="values">레지스터 값 배열</param>
        public async Task WriteHoldingRegistersAsync(int startAddress, int[] values)
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        if (!EnsureConnected())
                        {
                            return;
                        }

                        Logger.Debug($"보유 레지스터 쓰기: 시작 주소={startAddress}, 길이={values.Length}");

                        if (values.Length == 1)
                        {
                            _modbusClient.WriteSingleRegister(startAddress, values[0]);
                        }
                        else
                        {
                            _modbusClient.WriteMultipleRegisters(startAddress, values);
                        }
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex, $"보유 레지스터 쓰기 실패: 시작 주소={startAddress}, 길이={values.Length}");
                }
            });
        }

        private bool EnsureConnected()
        {
            if (!_isConnected)
            {
                Logger.Warn("PLC에 연결되어 있지 않습니다. 작업을 수행할 수 없습니다.");
                return false;
            }

            return true;
        }

        private void HandleException(Exception ex, string message)
        {
            if (ex is SocketException || ex is TimeoutException || ex is InvalidOperationException)
            {
                Logger.Error(ex, $"{message} - 연결 문제 감지됨");
                HandleConnectionLost();
            }
            else
            {
                Logger.Error(ex, message);
            }
        }

        /// <summary>
        /// 리소스를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_connectionCheckTimer != null)
                {
                    _connectionCheckTimer.Stop();
                    _connectionCheckTimer.Elapsed -= ConnectionCheckTimer_Elapsed;
                    _connectionCheckTimer.Dispose();
                    _connectionCheckTimer = null;
                }

                if (_modbusClient != null)
                {
                    if (_modbusClient.Connected)
                    {
                        _modbusClient.Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Dispose 중 예외 발생");
            }
        }
    }
}