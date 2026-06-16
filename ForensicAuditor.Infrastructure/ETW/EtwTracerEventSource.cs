using System.Diagnostics.Tracing;

namespace ForensicAuditor.Infrastructure.ETW
{
    [EventSource(Name = "ForensicAuditor-EtwTracer")]
    internal sealed class EtwTracerEventSource : EventSource
    {
        public static readonly EtwTracerEventSource Log = new EtwTracerEventSource();

        private EtwTracerEventSource() { }

        [Event(1, Level = EventLevel.Informational, Message = "Tracer started")] 
        public void TracerStarted() => WriteEvent(1);

        [Event(2, Level = EventLevel.Error, Message = "Tracer failed: {0}")]
        public void TracerFailed(string reason) => WriteEvent(2, reason);

        [Event(3, Level = EventLevel.Warning, Message = "Tracer not elevated; ETW disabled")] 
        public void TracerNotElevated() => WriteEvent(3);

        [Event(8, Level = EventLevel.Warning, Message = "Tracer started but not elevated; ETW disabled")] 
        public void TracerStartedNotElevated() => WriteEvent(8);

        [Event(4, Level = EventLevel.Informational, Message = "Session enabled: {0}")]
        public void SessionEnabled(string sessionName) => WriteEvent(4, sessionName);

        [Event(5, Level = EventLevel.Informational, Message = "Correlated registry key '{0}' to PID {1}")]
        public void Correlated(string key, int pid) => WriteEvent(5, key, pid);

        [Event(6, Level = EventLevel.Verbose, Message = "Pruned {0} entries, {1} remaining")] 
        public void Pruned(int removed, int remaining) => WriteEvent(6, removed, remaining);

        [Event(7, Level = EventLevel.Informational, Message = "Tracer stopped")] 
        public void TracerStopped() => WriteEvent(7);
    }
}

