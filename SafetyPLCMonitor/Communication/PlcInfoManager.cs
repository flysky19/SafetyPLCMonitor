using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using SafetyPLCMonitor.Core.Interfaces;
using SafetyPLCMonitor.Core.Models;
using System.Collections.Generic;

namespace SafetyPLCMonitor.Communication
{
    public class PlcInfoManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IModbusClient _modbusClient;

        public PlcInfoManager(IModbusClient modbusClient)
        {
            _modbusClient = modbusClient ?? throw new ArgumentNullException(nameof(modbusClient));
        }

        /// <summary>
        /// PLC 장치 정보 읽기
        /// </summary>
        /// <returns>장치 정보</returns>
        public async Task<DeviceInfo> ReadDeviceInfoAsync()
        {
            if (!_modbusClient.IsConnected)
            {
                Logger.Warn("장치 정보 읽기 실패: 연결되지 않음");
                return null;
            }

            try
            {
                Logger.Debug("장치 정보 읽기 시작");

                // 시리얼 번호 읽기
                int[] serialData = await _modbusClient.ReadInputRegistersAsync(
                    PnozModbusMap.DeviceInfo.SerialNumberStart, 2);

                if (serialData.Length < 2)
                {
                    Logger.Warn("장치 정보 읽기 실패: 시리얼 번호 데이터 부족");
                    return null;
                }

                uint serialNumber = (uint)((serialData[0] << 16) | serialData[1]);

                // 제품 코드 읽기
                int[] productData = await _modbusClient.ReadInputRegistersAsync(
                    PnozModbusMap.DeviceInfo.ProductCodeStart, 2);

                if (productData.Length < 2)
                {
                    Logger.Warn("장치 정보 읽기 실패: 제품 코드 데이터 부족");
                    return null;
                }

                uint productCode = (uint)((productData[0] << 16) | productData[1]);

                // 하드웨어 버전 읽기
                int[] hwData = await _modbusClient.ReadInputRegistersAsync(
                    PnozModbusMap.DeviceInfo.HardwareVersionStart, 2);

                if (hwData.Length < 2)
                {
                    Logger.Warn("장치 정보 읽기 실패: 하드웨어 버전 데이터 부족");
                    return null;
                }

                string hardwareVersion = $"{(hwData[0] >> 8) & 0xFF}.{hwData[0] & 0xFF}.{(hwData[1] >> 8) & 0xFF}.{hwData[1] & 0xFF}";

                // 펌웨어 버전 읽기
                int[] fwData = await _modbusClient.ReadInputRegistersAsync(
                    PnozModbusMap.DeviceInfo.FirmwareVersionStart, 2);

                if (fwData.Length < 2)
                {
                    Logger.Warn("장치 정보 읽기 실패: 펌웨어 버전 데이터 부족");
                    return null;
                }

                string firmwareVersion = $"{(fwData[0] >> 8) & 0xFF}.{fwData[0] & 0xFF}.{(fwData[1] >> 8) & 0xFF}.{fwData[1] & 0xFF}";

                // 프로젝트 CRC 읽기
                int[] crcData = await _modbusClient.ReadInputRegistersAsync(
                    PnozModbusMap.DeviceInfo.ProjectCrcStart, 2);

                if (crcData.Length < 2)
                {
                    Logger.Warn("장치 정보 읽기 실패: 프로젝트 CRC 데이터 부족");
                    return null;
                }

                uint projectCrc = (uint)((crcData[0] << 16) | crcData[1]);

                // 장치 정보 생성
                var deviceInfo = new DeviceInfo
                {
                    SerialNumber = serialNumber,
                    ProductCode = productCode,
                    HardwareVersion = hardwareVersion,
                    FirmwareVersion = firmwareVersion,
                    ProjectCrc = projectCrc,
                    Timestamp = DateTime.Now
                };

                Logger.Info($"장치 정보 읽기 성공: {deviceInfo}");

                return deviceInfo;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "장치 정보 읽기 중 오류 발생");
                return null;
            }
        }

        /// <summary>
        /// 시스템 상태 읽기
        /// </summary>
        /// <returns>시스템 상태 정보</returns>
        public async Task<SystemStatusInfo> ReadSystemStatusAsync()
        {
            if (!_modbusClient.IsConnected)
            {
                Logger.Warn("시스템 상태 읽기 실패: 연결되지 않음");
                return null;
            }

            try
            {
                Logger.Debug("시스템 상태 읽기 시작");

                // 시스템 상태 레지스터 읽기
                int[] statusData = await _modbusClient.ReadInputRegistersAsync(
                    PnozModbusMap.SystemStatus.StatusRegister, 1);

                if (statusData.Length < 1)
                {
                    Logger.Warn("시스템 상태 읽기 실패: 상태 데이터 부족");
                    return null;
                }

                // 에러 코드 읽기
                int[] errorData = await _modbusClient.ReadInputRegistersAsync(
                    PnozModbusMap.SystemStatus.ErrorCodeStart, 3);

                if (errorData.Length < 3)
                {
                    Logger.Warn("시스템 상태 읽기 실패: 에러 코드 데이터 부족");
                    return null;
                }

                // 운영 시간 읽기
                int[] timeData = await _modbusClient.ReadInputRegistersAsync(
                    PnozModbusMap.SystemStatus.OperationTimeStart, 2);

                if (timeData.Length < 2)
                {
                    Logger.Warn("시스템 상태 읽기 실패: 운영 시간 데이터 부족");
                    return null;
                }

                // 시스템 상태 정보 생성
                var statusInfo = new SystemStatusInfo
                {
                    StatusRegister = statusData[0],
                    ErrorCode1 = errorData[0],
                    ErrorCode2 = errorData[1],
                    ErrorCode3 = errorData[2],
                    OperationTime = ((long)timeData[0] << 16) | timeData[1],
                    Timestamp = DateTime.Now
                };

                // 상태 비트 분석
                for (int i = 0; i < 16; i++)
                {
                    bool bitValue = ((statusData[0] >> i) & 1) == 1;

                    if (bitValue && PnozModbusMap.StatusBits.TryGetValue(i, out string bitName))
                    {
                        statusInfo.ActiveStatusBits.Add(bitName);
                    }
                }

                // 에러 코드 분석
                if (errorData[0] != 0 && PnozModbusMap.ErrorCodes.TryGetValue(errorData[0], out string errorDesc))
                {
                    statusInfo.ErrorDescription = errorDesc;
                }
                else
                {
                    statusInfo.ErrorDescription = "정상";
                }

                Logger.Info($"시스템 상태 읽기 성공: 상태={statusInfo.StatusRegister:X4}, 에러={statusInfo.ErrorCode1}, 운영시간={statusInfo.OperationTimeFormatted}");

                return statusInfo;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "시스템 상태 읽기 중 오류 발생");
                return null;
            }
        }
    }

    public class SystemStatusInfo
    {
        /// <summary>
        /// 시스템 상태 레지스터
        /// </summary>
        public int StatusRegister { get; set; }

        /// <summary>
        /// 활성화된 상태 비트 목록
        /// </summary>
        public List<string> ActiveStatusBits { get; set; } = new List<string>();

        /// <summary>
        /// 에러 코드 1
        /// </summary>
        public int ErrorCode1 { get; set; }

        /// <summary>
        /// 에러 코드 2
        /// </summary>
        public int ErrorCode2 { get; set; }

        /// <summary>
        /// 에러 코드 3
        /// </summary>
        public int ErrorCode3 { get; set; }

        /// <summary>
        /// 에러 설명
        /// </summary>
        public string ErrorDescription { get; set; }

        /// <summary>
        /// 운영 시간 (초)
        /// </summary>
        public long OperationTime { get; set; }

        /// <summary>
        /// 정보 조회 시간
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 포맷된 운영 시간
        /// </summary>
        public string OperationTimeFormatted
        {
            get
            {
                TimeSpan span = TimeSpan.FromSeconds(OperationTime);
                return $"{(int)span.TotalDays}일 {span.Hours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
            }
        }
    }
}