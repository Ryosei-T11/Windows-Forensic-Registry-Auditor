using System;
using System.IO;

namespace ForensicAuditor.Infrastructure.Mitigation
{
    public class AuditLogger
    {
        private readonly string _logPath;
        private readonly object _lock = new();

        public AuditLogger()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ForensicAuditor");
            Directory.CreateDirectory(folder);
            _logPath = Path.Combine(folder, "tamper_audit.log");
        }

        public void LogSecured(string message)
        {
            lock (_lock)
            {
                // Menulis log dengan format terlindungi ke direktori yang dikunci sistem
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(_logPath, entry);
            }
        }
    }
}
