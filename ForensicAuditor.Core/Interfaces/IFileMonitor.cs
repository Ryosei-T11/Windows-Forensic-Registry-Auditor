using System;
using ForensicAuditor.Core.Models;

namespace ForensicAuditor.Core.Interfaces
{
    public interface IFileMonitor
    {
        event Action<FileEvent> OnFileChanged;
        void StartMonitoring();
        void StopMonitoring();
    }
}