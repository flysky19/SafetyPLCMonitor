// SafetyPLCMonitor/Models/PlcDevice.cs
using System;

namespace SafetyPLCMonitor.Models
{
    public class PlcDevice
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; } = 502;
        public uint? SerialNumber { get; set; }
        public string ProductType { get; set; }
        public string FirmwareVersion { get; set; }
        public string ProjectName { get; set; }
        public DateTime LastConnected { get; set; }
        public bool IsConnected { get; set; }
        public bool IsPollingActive { get; set; }
    }
}