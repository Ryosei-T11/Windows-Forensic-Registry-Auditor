using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ForensicAuditor.Infrastructure.Async
{
    public static class ActivityTask
    {
        /// Runs the given async function on the thread-pool while propagating the current Activity context.
        /// Usage: await ActivityTask.Run(() => DoWorkAsync());
        public static Task Run(Func<Task> func)
        {
            var captured = Activity.Current;
            return Task.Run(async () =>
            {
                var previous = Activity.Current;
                try
                {
                    Activity.Current = captured;
                    await func().ConfigureAwait(false);
                }
                finally
                {
                    Activity.Current = previous;
                }
            });
        }

        public static Task<T> Run<T>(Func<Task<T>> func)
        {
            var captured = Activity.Current;
            return Task.Run(async () =>
            {
                var previous = Activity.Current;
                try
                {
                    Activity.Current = captured;
                    return await func().ConfigureAwait(false);
                }
                finally
                {
                    Activity.Current = previous;
                }
            });
        }
    }
}
