using System.Diagnostics;

namespace ForensicAuditor.Infrastructure.Tracing
{
    public static class Tracing
    {
        public static readonly ActivitySource ActivitySource = new("ForensicAuditor");

        public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        {
            try
            {
                return ActivitySource.StartActivity(name, kind);
            }
            catch
            {
                return null;
            }
        }
    }
}
