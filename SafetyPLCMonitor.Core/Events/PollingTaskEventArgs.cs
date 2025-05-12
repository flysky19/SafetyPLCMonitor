using System;
using SafetyPLCMonitor.Core.Models;

namespace SafetyPLCMonitor.Core.Events
{
    public class PollingTaskEventArgs : EventArgs
    {
        /// <summary>
        /// 폴링 태스크
        /// </summary>
        public PollingTask Task { get; }

        /// <summary>
        /// 실행 성공 여부
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// 수신된 데이터
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// 오류 메시지 (실패 시)
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// 이벤트 발생 시간
        /// </summary>
        public DateTime Timestamp { get; }

        public PollingTaskEventArgs(PollingTask task, bool success, object data, string errorMessage = null)
        {
            Task = task;
            Success = success;
            Data = data;
            ErrorMessage = errorMessage;
            Timestamp = DateTime.Now;
        }
    }
}