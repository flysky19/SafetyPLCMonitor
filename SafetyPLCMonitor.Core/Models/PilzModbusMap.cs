using System.Collections.Generic;

namespace SafetyPLCMonitor.Core.Models
{
    /// <summary>
    /// PNOZmulti Safety PLC의 Modbus 레지스터 맵 정의
    /// </summary>
    public static class PnozModbusMap
    {
        // 장치 정보
        public static class DeviceInfo
        {
            public const int SerialNumberStart = 787;   // 시리얼 번호 (2 레지스터)
            public const int ProductCodeStart = 789;    // 제품 코드 (2 레지스터)
            public const int HardwareVersionStart = 791; // 하드웨어 버전 (2 레지스터)
            public const int FirmwareVersionStart = 793; // 펌웨어 버전 (2 레지스터)
            public const int ProjectCrcStart = 795;      // 프로젝트 CRC (2 레지스터)
        }

        // 디지털 입력
        public static class DigitalInputs
        {
            public const int StatusStart = 0;        // 디지털 입력 상태 (16 레지스터)
            public const int DiagnosticStart = 256;  // 진단 정보 (16 레지스터)
        }

        // 디지털 출력
        public static class DigitalOutputs
        {
            public const int StatusStart = 16;       // 디지털 출력 상태 (16 레지스터)
            public const int DiagnosticStart = 272;  // 진단 정보 (16 레지스터)
        }

        // 가상 입력
        public static class VirtualInputs
        {
            public const int StatusStart = 32;      // 가상 입력 상태 (16 레지스터)
        }

        // 시스템 상태
        public static class SystemStatus
        {
            public const int StatusRegister = 512;   // 시스템 상태 레지스터
            public const int ErrorCodeStart = 513;   // 에러 코드 (3 레지스터)
            public const int OperationTimeStart = 516; // 운영 시간 (2 레지스터)
        }

        // 상태 및 에러 코드 매핑
        public static readonly Dictionary<int, string> ErrorCodes = new Dictionary<int, string>
        {
            { 0, "정상" },
            { 1, "내부 오류" },
            { 2, "통신 오류" },
            { 3, "입력 오류" },
            { 4, "출력 오류" },
            { 5, "설정 오류" },
            { 6, "전원 오류" },
            { 7, "온도 오류" },
            { 8, "모듈 오류" },
            // 추가 오류 코드...
        };

        // 시스템 상태 비트 정의
        public static readonly Dictionary<int, string> StatusBits = new Dictionary<int, string>
        {
            { 0, "Run" },
            { 1, "Stop" },
            { 2, "Fault" },
            { 3, "Safety Function Active" },
            { 4, "Diagnostic Active" },
            { 5, "Configuration Mode" },
            { 6, "Force Mode" },
            { 7, "Communications Operational" },
            { 8, "Input Test Active" },
            { 9, "Output Test Active" },
            { 10, "External Power Supply Error" },
            { 11, "Internal Power Supply Error" },
            { 12, "Temperature Warning" },
            { 13, "Flash Memory Error" },
            { 14, "RAM Error" },
            { 15, "CPU Error" }
        };

        // 입력/출력 비트 정의
        public static readonly Dictionary<int, string> DefaultIOLabels = new Dictionary<int, string>
        {
            // 디지털 입력
            { 0, "I0" },
            { 1, "I1" },
            { 2, "I2" },
            { 3, "I3" },
            { 4, "I4" },
            { 5, "I5" },
            { 6, "I6" },
            { 7, "I7" },
            { 8, "I8" },
            { 9, "I9" },
            { 10, "I10" },
            { 11, "I11" },
            { 12, "I12" },
            { 13, "I13" },
            { 14, "I14" },
            { 15, "I15" },
            
            // 디지털 출력
            { 16, "O0" },
            { 17, "O1" },
            { 18, "O2" },
            { 19, "O3" },
            { 20, "O4" },
            { 21, "O5" },
            { 22, "O6" },
            { 23, "O7" },
            { 24, "O8" },
            { 25, "O9" },
            { 26, "O10" },
            { 27, "O11" },
            { 28, "O12" },
            { 29, "O13" },
            { 30, "O14" },
            { 31, "O15" },
            
            // 가상 입력
            { 32, "VI0" },
            { 33, "VI1" },
            { 34, "VI2" },
            { 35, "VI3" },
            { 36, "VI4" },
            { 37, "VI5" },
            { 38, "VI6" },
            { 39, "VI7" },
            { 40, "VI8" },
            { 41, "VI9" },
            { 42, "VI10" },
            { 43, "VI11" },
            { 44, "VI12" },
            { 45, "VI13" },
            { 46, "VI14" },
            { 47, "VI15" }
        };
    }
}