using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ForensicAuditor.Infrastructure.Mitigation
{
    public class ProcessQuarantine
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSuspendProcess(IntPtr processHandle);

        /// <summary>
        /// Mengisolasi proses secara instan dengan membekukan (suspend) seluruh thread proses sebelum diterminasi.
        /// </summary>
        public bool SuspendThreatProcess(int processId)
        {
            try
            {
                using Process proc = Process.GetProcessById(processId);
                // Bekukan proses agar malware tidak mengeksekusi instruksi penghancuran sandi/enkripsi files
                IntPtr hProc = proc.Handle;
                NtSuspendProcess(hProc);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void TerminateProcess(int processId)
        {
            try
            {
                using Process proc = Process.GetProcessById(processId);
                proc.Kill(true);
            }
            catch { }
        }
    }
}