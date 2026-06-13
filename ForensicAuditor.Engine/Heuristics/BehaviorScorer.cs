using System;
using ForensicAuditor.Core.Models;

namespace ForensicAuditor.Engine.Heuristics
{
    public class BehaviorScorer
    {
        public double AdjustScore(RegistryEvent ev)
        {
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

            return Math.Min(10.0, baseScore); // Batas maksimal skor 10.0
        }
    }
}