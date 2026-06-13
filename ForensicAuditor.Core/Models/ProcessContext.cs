namespace ForensicAuditor.Core.Models
{
    public class ProcessContext
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "Unknown";
        public string ImagePath { get; set; } = "Unknown";
        public string CommandLine { get; set; } = string.Empty;
        public string? FilePublisher { get; set; }
        public bool IsTrusted { get; set; } = false;
        public string Sha256Hash { get; set; } = string.Empty;
    }
}
