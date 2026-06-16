using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace ForensicAuditor.Infrastructure.Logging
{
    /// Custom Serilog enricher that adds trace/span identifiers to log events using
    /// consistent property names: trace_id, span_id, parent_id.
    public class ActivityEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            try
            {
                var activity = Activity.Current;
                if (activity == null) return;

                if (!string.IsNullOrEmpty(activity.TraceId.ToString()))
                {
                    var traceProp = propertyFactory.CreateProperty("trace_id", activity.TraceId.ToString());
                    logEvent.AddPropertyIfAbsent(traceProp);
                }

                if (!string.IsNullOrEmpty(activity.SpanId.ToString()))
                {
                    var spanProp = propertyFactory.CreateProperty("span_id", activity.SpanId.ToString());
                    logEvent.AddPropertyIfAbsent(spanProp);
                }

                if (!string.IsNullOrEmpty(activity.ParentSpanId.ToString()))
                {
                    var parentProp = propertyFactory.CreateProperty("parent_id", activity.ParentSpanId.ToString());
                    logEvent.AddPropertyIfAbsent(parentProp);
                }
            }
            catch
            {
                // never throw from enricher
            }
        }
    }
}
