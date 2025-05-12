// SafetyPLCMonitor/Models/PlcStatus.cs
using System;
using System.Collections.Generic;

namespace SafetyPLCMonitor.Models
{
    public class PlcStatus
    {
        public string DeviceId { get; set; }
        public bool IsRunning { get; set; }
        public bool HasFault { get; set; }
        public bool HasWarning { get; set; }
        public bool IsDiagnosticActive { get; set; }
        public int StatusRegister { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> ActiveStatusBits { get; set; } = new List<string>();
        public long OperationTime { get; set; } // 초 단위
        public DateTime LastUpdateTime { get; set; }

        public string FormattedOperationTime
        {
            get
            {
                TimeSpan span = TimeSpan.FromSeconds(OperationTime);
                return $"{(int)span.TotalDays}일 {span.Hours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
            }
        }
    }
}