using System;
using System.Collections.Generic;

namespace ForensicAuditor.Engine.Heuristics
{
    public class WhitelistManager
    {
        private readonly HashSet<string> _trustedPublishers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft Windows",
            "Microsoft Corporation",
            "Google LLC",
            "Advanced Micro Devices, Inc."
        };

        public bool IsProcessTrusted(string? publisher, string path)
        {
            if (path.StartsWith("C:\\Windows\\System32\\", StringComparison.OrdinalIgnoreCase) &&
                publisher != null && _trustedPublishers.Contains(publisher))
            {
                return true;
            }
            return false;
        }
    }
}