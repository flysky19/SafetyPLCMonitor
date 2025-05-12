using System;
using System.Threading.Tasks;
using SafetyPLCMonitor.Core.Events;

namespace SafetyPLCMonitor.Core.Interfaces
{
    public interface IModbusClient : IDisposable
    {
        /// <summary>
        /// PLC 연결 상태
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// PLC IP 주소
        /// </summary>
        string IpAddress { get; }

        /// <summary>
        /// PLC 포트
        /// </summary>
        int Port { get; }

        /// <summary>
        /// 연결 상태 변경 이벤트
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// 데이터 수신 이벤트
        /// </summary>
        event EventHandler<ModbusDataEventArgs> DataReceived;

        /// <summary>
        /// PLC에 연결합니다.
        /// </summary>
        /// <returns>연결 성공 여부</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// PLC 연결을 해제합니다.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 입력 레지스터를 읽습니다.
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="length">레지스터 개수</param>
        /// <returns>레지스터 값 배열</returns>
        Task<int[]> ReadInputRegistersAsync(int startAddress, int length);

        /// <summary>
        /// 보유 레지스터를 읽습니다.
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="length">레지스터 개수</param>
        /// <returns>레지스터 값 배열</returns>
        Task<int[]> ReadHoldingRegistersAsync(int startAddress, int length);

        /// <summary>
        /// 디스크릿 입력을 읽습니다.
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="length">입력 개수</param>
        /// <returns>입력 상태 배열</returns>
        Task<bool[]> ReadDiscreteInputsAsync(int startAddress, int length);

        /// <summary>
        /// 코일 상태를 읽습니다.
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="length">코일 개수</param>
        /// <returns>코일 상태 배열</returns>
        Task<bool[]> ReadCoilsAsync(int startAddress, int length);

        /// <summary>
        /// 코일 상태를 씁니다.
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="values">코일 상태 배열</param>
        Task WriteCoilsAsync(int startAddress, bool[] values);

        /// <summary>
        /// 보유 레지스터 값을 씁니다.
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="values">레지스터 값 배열</param>
        Task WriteHoldingRegistersAsync(int startAddress, int[] values);


        void UpdateConnectionParameters(string ipAddress, int port);
    }
}