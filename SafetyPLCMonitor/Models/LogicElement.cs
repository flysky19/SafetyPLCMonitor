// SafetyPLCMonitor/Models/LogicElement.cs
using System;
using System.Collections.Generic;
using System.Windows;

namespace SafetyPLCMonitor.Models
{
    public enum LogicElementType
    {
        Input,
        Output,
        AndGate,
        OrGate,
        NotGate,
        Timer,
        Counter,
        Monitor,
        TwoHandControl,
        EmergencyStop,
        GuardMonitoring,
        OperatingMode
    }

    public enum LogicElementState
    {
        Inactive,
        Active,
        Error,
        Unknown
    }

    public class LogicElement
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public LogicElementType Type { get; set; }
        public LogicElementState State { get; set; }
        public Point Position { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public List<int> InputConnections { get; set; } = new List<int>();
        public List<int> OutputConnections { get; set; } = new List<int>();
    }

    public class LogicConnection
    {
        public int Id { get; set; }
        public int SourceId { get; set; }
        public int TargetId { get; set; }
        public bool State { get; set; }
    }

    public class LogicDiagram
    {
        public string Name { get; set; }
        public List<LogicElement> Elements { get; set; } = new List<LogicElement>();
        public List<LogicConnection> Connections { get; set; } = new List<LogicConnection>();
    }
}