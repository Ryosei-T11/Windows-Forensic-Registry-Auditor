using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using ForensicAuditor.Core.Models;
using Serilog;
using ForensicAuditor.Infrastructure.Tracing;

namespace ForensicAuditor.Engine.Heuristics
{
    public class RuleEvaluator
    {
        private readonly List<HeuristicRule> _loadedRules = new();

        public void LoadRulesFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            foreach (var file in Directory.GetFiles(directoryPath, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var rule = JsonSerializer.Deserialize<HeuristicRule>(json);
                    if (rule != null) _loadedRules.Add(rule);
                }
                catch { /* Abaikan rule yang malformed untuk keamanan engine */ }
            }
        }

        public RegistryEvent EvaluateRegistry(RegistryEvent regEvent)
        {
            using var act = ForensicAuditor.Infrastructure.Tracing.Tracing.StartActivity("RuleEvaluator.EvaluateRegistry");
            if (act != null)
            {
                act.SetTag("event.id", regEvent.EventId);
                act.SetTag("event.correlation", regEvent.CorrelationId);
                act.SetTag("registry.hive", regEvent.Hive);
                act.SetTag("registry.key", regEvent.SubKeyPath);
            }

            Log.Debug("Evaluating registry event {EventId} Correlation={Correlation} Key={Key}", regEvent.EventId, regEvent.CorrelationId, regEvent.SubKeyPath);

            foreach (var rule in _loadedRules)
            {
                foreach (var targetKey in rule.TargetRegistryKeys)
                {
                    // Melakukan wildcard matching (misalnya pencocokan dengan *)
                    if (IsMatch(regEvent.SubKeyPath, targetKey))
                    {
                        regEvent.RiskScore = CalculateRisk(rule);
                        regEvent.DetectionRule = rule.RuleName;
                        regEvent.Severity = Enum.Parse<SeverityLevel>(rule.Severity);
                        return regEvent;
                    }
                }
            }
            return regEvent;
        }

        private static bool IsMatch(string path, string pattern)
        {
            if (pattern.Contains('*'))
            {
                string regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                return System.Text.RegularExpressions.Regex.IsMatch(path, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            return string.Equals(path, pattern, StringComparison.OrdinalIgnoreCase);
        }

        private static double CalculateRisk(HeuristicRule rule) => rule.Severity switch
        {
            "Critical" => 9.5,
            "High" => 7.5,
            "Medium" => 5.0,
            _ => 2.0
        };
    }
}