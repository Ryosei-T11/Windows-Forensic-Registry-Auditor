using System;
using System.Text;
using ForensicAuditor.Infrastructure.Win32;

namespace ForensicAuditor.Infrastructure.Win32
{
    public static class ProcessHelper
    {
        public static string GetProcessExecutablePath(int processId)
        {
            if (processId <= 0) return "System/Unknown";

            IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero) return "Access Denied";

            try
            {
                uint size = 1024;
                var buffer = new StringBuilder((int)size);
                uint actualSize = size;
                if (NativeMethods.QueryFullProcessImageName(hProcess, 0, buffer, ref actualSize))
                {
                    return buffer.ToString(0, (int)actualSize);
                }
            }
            finally
            {
                NativeMethods.CloseHandle(hProcess);
            }

            return "Unknown Image Path";
        }
    }
}
