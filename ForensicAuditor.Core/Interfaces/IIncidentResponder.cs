using ForensicAuditor.Core.Models;

namespace ForensicAuditor.Core.Interfaces
{
    public interface IIncidentResponder
    {
        bool ExecuteRollback(RegistryEvent regEvent);
        bool IsolateProcess(int processId);
    }
}