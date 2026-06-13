using System;
using ForensicAuditor.Core.Models;

namespace ForensicAuditor.Core.Interfaces
{
    public interface IRegistryMonitor
    {
        event Action<RegistryEvent> OnRegistryChanged;
        void StartMonitoring();
        void StopMonitoring();
    }
}
