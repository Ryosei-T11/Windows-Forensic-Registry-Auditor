using System;

namespace ForensicAuditor.Core.Models
{
    public enum SeverityLevel
    {
        Informational,
        Low,
        Medium,
        High,
        Critical
    }

    public class RegistryEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Hive { get; set; } = string.Empty;
        public string SubKeyPath { get; set; } = string.Empty;
        public string? ValueName { get; set; }
        public object? NewValue { get; set; }
        public string Action { get; set; } = "Modified";
        public int ProcessId { get; set; } = -1;
        public string ProcessName { get; set; } = "Unknown";
        public string ProcessPath { get; set; } = "Unknown";
        public bool IsProcessSigned { get; set; } = false;
        public string? ProcessSigner { get; set; }
        public double RiskScore { get; set; } = 0.0;
        public SeverityLevel Severity { get; set; } = SeverityLevel.Informational;
        public string DetectionRule { get; set; } = "None";
    }
}