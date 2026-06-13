using ForensicAuditor.Core.Models;

namespace ForensicAuditor.Core.Interfaces
{
    public interface IHeuristicEngine
    {
        RegistryEvent AnalyzeRegistryChange(RegistryEvent rawEvent);
        FileEvent AnalyzeFileChange(FileEvent rawEvent);
    }
}
