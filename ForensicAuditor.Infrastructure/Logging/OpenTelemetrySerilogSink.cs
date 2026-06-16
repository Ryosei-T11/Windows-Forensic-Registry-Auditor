using System;
using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;
using ForensicAuditor.Infrastructure.Tracing;

namespace ForensicAuditor.Infrastructure.Logging
{
    /// A Serilog sink that emits a short Activity for each log event so OpenTelemetry TracerProvider
    /// can export logs via OTLP as spans. This is a pragmatic POC: logs are represented as Activities.
    public class OpenTelemetrySerilogSink : ILogEventSink, IDisposable
    {
        private readonly IFormatProvider _formatProvider;

        public OpenTelemetrySerilogSink(IFormatProvider? formatProvider = null)
        {
            _formatProvider = formatProvider ?? null;
        }

        public void Emit(LogEvent logEvent)
        {
            try
            {
                using var act = ForensicAuditor.Infrastructure.Tracing.Tracing.StartActivity("log", ActivityKind.Internal);
                if (act == null) return;

                act.SetTag("log.severity", logEvent.Level.ToString());
                act.SetTag("log.message", logEvent.RenderMessage(_formatProvider));
                act.SetTag("log.timestamp", logEvent.Timestamp.ToUniversalTime().ToString("o"));
                if (logEvent.Exception != null)
                {
                    act.SetTag("log.exception.type", logEvent.Exception.GetType().FullName);
                    act.SetTag("log.exception.message", logEvent.Exception.Message);
                }

                // attach properties
                foreach (var prop in logEvent.Properties)
                {
                    try { act.SetTag($"log.prop.{prop.Key}", prop.Value.ToString()); }
                    catch { }
                }
            }
            catch { }
        }

        public void Dispose()
        {
        }
    }
}
