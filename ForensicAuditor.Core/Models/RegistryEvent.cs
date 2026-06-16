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

    /// Model data yang diperluas dengan kolom Hash SHA-256 dan Reputasi Cloud untuk mendukung Fase 1 Roadmap.
    public class RegistryEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        // CorrelationId can be used to group related events across components / tracing systems
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Hive { get; set; } = string.Empty;
        public string SubKeyPath { get; set; } = string.Empty;
        public string? ValueName { get; set; }
        public object? NewValue { get; set; }
        public string Action { get; set; } = "Modified";

        // Informasi Proses Pelaku (Process Blaming Context)
        public int ProcessId { get; set; } = -1;
        public string ProcessName { get; set; } = "Unknown";
        public string ProcessPath { get; set; } = "Unknown";
        public bool IsProcessSigned { get; set; } = false;
        public string? ProcessSigner { get; set; }

        // Kolom Tambahan untuk Integritas Berkas dan Reputasi Cloud
        public string Sha256Hash { get; set; } = "Calculating...";
        public string ReputationResult { get; set; } = "Not Scanned";

        // Penilaian Risiko oleh Heuristic Engine
        public double RiskScore { get; set; } = 0.0;
        public SeverityLevel Severity { get; set; } = SeverityLevel.Informational;
        public string DetectionRule { get; set; } = "None";
    }
}