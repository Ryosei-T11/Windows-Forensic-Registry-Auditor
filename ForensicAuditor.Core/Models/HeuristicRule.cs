using System.Collections.Generic;

namespace ForensicAuditor.Core.Models
{
    public class HeuristicRule
    {
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Low";
        public List<string> TargetRegistryKeys { get; set; } = new();
        public Dictionary<string, object> BehavioralTriggers { get; set; } = new();
        public Dictionary<string, bool> AutomaticMitigation { get; set; } = new();
    }
}