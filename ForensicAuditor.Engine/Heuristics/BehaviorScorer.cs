using System;
using System.Diagnostics;
using ForensicAuditor.Core.Models;
using Serilog;
using ForensicAuditor.Infrastructure.Tracing;

namespace ForensicAuditor.Engine.Heuristics
{
    public class BehaviorScorer
    {
        public double AdjustScore(RegistryEvent ev)
        {
            using var act = ForensicAuditor.Infrastructure.Tracing.Tracing.StartActivity("BehaviorScorer.AdjustScore");
            act?.SetTag("event.id", ev.EventId);
            act?.SetTag("event.correlation", ev.CorrelationId);

            double baseScore = ev.RiskScore;

            // Penalti skor jika ditransfer dari direktori tidak aman (Heuristic behavior)
            if (ev.ProcessPath.Contains("AppData\\Local\\Temp") || ev.ProcessPath.Contains("Users\\Public"))
            {
                baseScore += 2.0;
            }

            // Penalti jika biner tidak ditandatangani secara digital (Unsigned exe)
            if (!ev.IsProcessSigned)
            {
                baseScore += 1.5;
            }

            double final = Math.Min(10.0, baseScore); // Batas maksimal skor 10.0
            Log.Debug("BehaviorScorer adjusted score for {EventId}: {Final}", ev.EventId, final);
            act?.SetTag("risk.score.final", final);
            return final;
        }
    }
}