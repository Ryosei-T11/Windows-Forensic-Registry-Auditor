using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Serilog;

namespace ForensicAuditor.Infrastructure.Mitigation
{
    public class ProcessQuarantine
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSuspendProcess(IntPtr processHandle);

        /// Mengisolasi proses secara instan dengan membekukan (suspend) seluruh thread proses sebelum diterminasi.
        public bool SuspendThreatProcess(int processId)
        {
            try
            {
                using Process proc = Process.GetProcessById(processId);
                // Bekukan proses agar malware tidak mengeksekusi instruksi penghancuran sandi/enkripsi files
                IntPtr hProc = proc.Handle;
                NtSuspendProcess(hProc);
                Log.Information("Suspended process {Pid} ({Name}) - triggered by Correlation={Correlation} EventId={EventId}", processId, proc.ProcessName, "{correlation}", "{eventid}");
                return true;
            }
            catch
            {
                Log.Warning("Failed to suspend process {Pid} - triggered by Correlation={Correlation} EventId={EventId}", processId, "{correlation}", "{eventid}");
                return false;
            }
        }

        public void TerminateProcess(int processId)
        {
            try
            {
                using Process proc = Process.GetProcessById(processId);
                proc.Kill(true);
                Log.Information("Terminated process {Pid} ({Name}) - triggered by Correlation={Correlation} EventId={EventId}", processId, proc.ProcessName, "{correlation}", "{eventid}");
            }
            catch { }
        }
    }
}