using System;

namespace ForensicAuditor.Core.Models
{
    public class FileEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Action { get; set; } = "Modified"; // Created, Modified, Deleted, Renamed
        public string? OldFilePath { get; set; } // Khusus untuk aksi Rename
        public int ProcessId { get; set; } = -1;
        public double EntropyValue { get; set; } = 0.0;
        public double RiskScore { get; set; } = 0.0;
        public SeverityLevel Severity { get; set; } = SeverityLevel.Informational;
    }
}