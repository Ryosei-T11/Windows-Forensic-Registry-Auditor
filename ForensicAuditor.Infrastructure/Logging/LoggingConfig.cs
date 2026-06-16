using Serilog;
using Serilog.Enrichers.Span;
using System;

namespace ForensicAuditor.Infrastructure.Logging
{
    public static class LoggingConfig
    {
        public static void Configure(string? logDirectory = null)
        {
            if (string.IsNullOrEmpty(logDirectory))
            {
                logDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            var logFile = System.IO.Path.Combine(logDirectory, "forensicauditor.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .Enrich.With(new ActivityEnricher())
                .WriteTo.Console()
                .WriteTo.File(logFile, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                // Forward to Seq when OTEL_SEQ_URL env var is set
                .WriteTo.Conditional(
                    e => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SEQ_URL")),
                    wt => wt.Seq(Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341"))
                // Write to OTLP by emitting Activities via custom sink which creates short-lived spans representing logs
                .WriteTo.Sink(new OpenTelemetrySerilogSink())
                .CreateLogger();
        }
    }
}
