using System;

namespace SafetyPLCMonitor.Core.Events
{
    public enum ModbusDataType
    {
        DiscreteInput,
        Coil,
        InputRegister,
        HoldingRegister
    }

    public class ModbusDataEventArgs : EventArgs
    {
        /// <summary>
        /// 데이터 타입
        /// </summary>
        public ModbusDataType DataType { get; }

        /// <summary>
        /// 시작 주소
        /// </summary>
        public int StartAddress { get; }

        /// <summary>
        /// 레지스터/코일 개수
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 수신 시간
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 레지스터 값 데이터 (레지스터인 경우)
        /// </summary>
        public int[] RegisterData { get; }

        /// <summary>
        /// 비트 값 데이터 (코일/입력인 경우)
        /// </summary>
        public bool[] BitData { get; }

        public ModbusDataEventArgs(ModbusDataType dataType, int startAddress, int[] registerData)
        {
            DataType = dataType;
            StartAddress = startAddress;
            RegisterData = registerData;
            Count = registerData.Length;
            Timestamp = DateTime.Now;
        }

        public ModbusDataEventArgs(ModbusDataType dataType, int startAddress, bool[] bitData)
        {
            DataType = dataType;
            StartAddress = startAddress;
            BitData = bitData;
            Count = bitData.Length;
            Timestamp = DateTime.Now;
        }
    }
}