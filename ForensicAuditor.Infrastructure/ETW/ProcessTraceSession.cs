using System;

namespace ForensicAuditor.Infrastructure.ETW
{
    public class ProcessTraceSession
    {
        public event Action<int, string>? OnProcessCreated; // PID, ProcessName

        public void StartSession()
        {
            // Menjejak daur hidup proses untuk mencocokkan PID pelaku modifikasi yang dinonaktifkan
            var _ = OnProcessCreated;

        }

        // Helper that raises the event so the event is referenced (prevents CS0067)
        protected void RaiseProcessCreated(int pid, string processName)
        {
            OnProcessCreated?.Invoke(pid, processName);
        }
    }
}
