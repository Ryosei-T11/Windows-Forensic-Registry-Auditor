using System;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

namespace ForensicAuditor.Infrastructure.Tracing
{
    public static class OpenTelemetryConfig
    {
        public static TracerProvider Configure(string serviceName = "ForensicAuditor", string otlpEndpoint = "http://localhost:4317")
        {
            var resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName: serviceName);

            var builder = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddSource("ForensicAuditor")
                .AddConsoleExporter();

            // Optionally add OTLP exporter if endpoint provided
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                builder.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
            }

            return builder.Build();
        }
    }
}
