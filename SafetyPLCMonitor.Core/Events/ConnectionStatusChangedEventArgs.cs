using System;

namespace SafetyPLCMonitor.Core.Events
{
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 연결 상태
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// IP 주소
        /// </summary>
        public string IpAddress { get; }

        /// <summary>
        /// 포트
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// 연결 변경 시간
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 오류 메시지 (있는 경우)
        /// </summary>
        public string ErrorMessage { get; }

        public ConnectionStatusChangedEventArgs(bool isConnected, string ipAddress, int port, string errorMessage = null)
        {
            IsConnected = isConnected;
            IpAddress = ipAddress;
            Port = port;
            Timestamp = DateTime.Now;
            ErrorMessage = errorMessage;
        }
    }
}