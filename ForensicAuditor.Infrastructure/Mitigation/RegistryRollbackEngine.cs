using System;
using System.Runtime.Versioning;
using Microsoft.Win32;
using ForensicAuditor.Core.Models;

namespace ForensicAuditor.Infrastructure.Mitigation
{
    public class RegistryRollbackEngine
    {
        [SupportedOSPlatform("windows")]
        public bool PerformRollback(RegistryEvent regEvent)
        {
            if (!OperatingSystem.IsWindows())
                return false;

            try
            {
                // Mengembalikan nilai ke keadaan kosong/state sebelumnya jika dirusak malware
                if (regEvent.Hive == "HKEY_LOCAL_MACHINE")
                {
                    using var key = Registry.LocalMachine.OpenSubKey(regEvent.SubKeyPath, true);
                    if (key != null && regEvent.ValueName != null)
                    {
                        key.DeleteValue(regEvent.ValueName, false);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false; 
            }
        }
    }
}