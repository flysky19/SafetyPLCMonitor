using System;
using SafetyPLCMonitor.Core.Events;

namespace SafetyPLCMonitor.Core.Models
{
    /// <summary>
    /// Modbus 레지스터 타입
    /// </summary>
    public enum ModbusRegisterType
    {
        DiscreteInput,
        Coil,
        InputRegister,
        HoldingRegister
    }

    /// <summary>
    /// 데이터 폴링 태스크 정의
    /// </summary>
    public class PollingTask
    {
        /// <summary>
        /// 태스크 ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 태스크 이름
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 레지스터 타입
        /// </summary>
        public ModbusRegisterType RegisterType { get; set; }

        /// <summary>
        /// 시작 주소
        /// </summary>
        public int StartAddress { get; set; }

        /// <summary>
        /// 레지스터/코일 개수
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 폴링 주기 (밀리초)
        /// </summary>
        public int PollingInterval { get; set; }

        /// <summary>
        /// 마지막 실행 시간
        /// </summary>
        public DateTime? LastExecutionTime { get; set; }

        /// <summary>
        /// 마지막 실행 성공 여부
        /// </summary>
        public bool LastExecutionSuccess { get; set; }

        /// <summary>
        /// 활성화 여부
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 우선순위
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 새 폴링 태스크 생성
        /// </summary>
        public PollingTask()
        {
            Id = Guid.NewGuid().ToString();
            IsEnabled = true;
            Priority = 0;
            PollingInterval = 1000; // 기본 1초
        }

        /// <summary>
        /// 태스크 설명 반환
        /// </summary>
        public override string ToString()
        {
            return $"{Name} ({RegisterType}, 주소: {StartAddress}, 길이: {Length}, 주기: {PollingInterval}ms)";
        }
    }
}