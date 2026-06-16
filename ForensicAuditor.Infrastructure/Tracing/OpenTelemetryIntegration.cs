using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

namespace ForensicAuditor.Infrastructure.Tracing
{
    public static class OpenTelemetryIntegration
    {
        public static TracerProvider RegisterOpenTelemetry(IConfiguration config)
        {
            var serviceName = config["OTEL_SERVICE_NAME"] ?? Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "ForensicAuditor";
            var otlpEndpoint = config["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? string.Empty;

            var resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName: serviceName);

            var builder = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddSource("ForensicAuditor")
                .AddConsoleExporter();

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                builder.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
            }

            // Basic metrics
            Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddConsoleExporter();

            return builder.Build();
        }
    }
}
