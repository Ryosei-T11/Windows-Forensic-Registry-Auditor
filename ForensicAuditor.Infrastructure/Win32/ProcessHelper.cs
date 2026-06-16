using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ForensicAuditor.Infrastructure.Win32
{
    public static class ProcessHelper
    {
        /// Mengambil absolute executable path dari Process ID (PID) pelaku modifikasi Registry.
        public static string GetProcessExecutablePath(int processId)
        {
            if (processId <= 0) return "System/Unknown";

            IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero) return "Access Denied";

            try
            {
                uint size = 1024;
                StringBuilder buffer = new StringBuilder((int)size);
                if (NativeMethods.QueryFullProcessImageName(hProcess, 0, buffer, ref size))
                {
                    return buffer.ToString();
                }
            }
            finally
            {
                NativeMethods.CloseHandle(hProcess);
            }

            return "Unknown Image Path";
        }

        /// Menghitung nilai SHA-256 hash dari berkas executable pelaku secara aman.
        /// Menggunakan FileShare.ReadWrite agar tidak error saat berkas sedang dijalankan/dikunci OS.
        public static string CalculateSha256(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) ||
                filePath == "Unknown" ||
                filePath == "Access Denied" ||
                filePath == "Unknown Image Path" ||
                !File.Exists(filePath))
            {
                return "N/A";
            }

            try
            {
                // Gunakan FileShare.ReadWrite untuk menghindari locked-file exceptions
                using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using SHA256 sha256 = SHA256.Create();

                byte[] hashBytes = sha256.ComputeHash(stream);

                StringBuilder sb = new();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString().ToUpper();
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}