using System;

namespace SafetyPLCMonitor.Core.Models
{
    /// <summary>
    /// PLC 장치 정보 클래스
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// 시리얼 번호
        /// </summary>
        public uint SerialNumber { get; set; }

        /// <summary>
        /// 제품 코드
        /// </summary>
        public uint ProductCode { get; set; }

        /// <summary>
        /// 하드웨어 버전
        /// </summary>
        public string HardwareVersion { get; set; }

        /// <summary>
        /// 펌웨어 버전
        /// </summary>
        public string FirmwareVersion { get; set; }

        /// <summary>
        /// 프로젝트 CRC
        /// </summary>
        public uint ProjectCrc { get; set; }

        /// <summary>
        /// 정보 조회 시간
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 장치 정보를 문자열로 반환
        /// </summary>
        public override string ToString()
        {
            return $"SerialNumber: {SerialNumber}, ProductCode: {ProductCode}, " +
                   $"HW: {HardwareVersion}, FW: {FirmwareVersion}, ProjectCRC: 0x{ProjectCrc:X8}";
        }
    }
}