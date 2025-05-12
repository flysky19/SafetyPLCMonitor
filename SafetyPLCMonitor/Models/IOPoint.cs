// SafetyPLCMonitor/Models/IOPoint.cs
using System;

namespace SafetyPLCMonitor.Models
{
    public enum IOType
    {
        DiscreteInput,
        DiscreteOutput,
        VirtualInput,
        VirtualOutput,
        InputRegister,
        HoldingRegister
    }

    public class IOPoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DeviceId { get; set; }
        public IOType Type { get; set; }
        public int Address { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public object Value { get; set; }
        public bool HasWarning { get; set; }
        public bool HasError { get; set; }
        public string Unit { get; set; }
        public DateTime LastUpdateTime { get; set; }

        public string FormattedValue
        {
            get
            {
                if (Value == null)
                    return "N/A";

                if (Type == IOType.DiscreteInput || Type == IOType.DiscreteOutput || Type == IOType.VirtualInput || Type == IOType.VirtualOutput)
                    return IsActive ? "1" : "0";

                return Value.ToString() + (string.IsNullOrEmpty(Unit) ? "" : " " + Unit);
            }
        }
    }
}