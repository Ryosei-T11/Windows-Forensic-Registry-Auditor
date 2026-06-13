using System;
using System.Threading.Tasks;
using ForensicAuditor.Core.Models;

namespace ForensicAuditor.Infrastructure.ETW
{
    public class RegistryTraceSession
    {
        #pragma warning disable CS0067
        public event Action<RegistryEvent>? OnKernelRegistryActivity;
#pragma warning restore CS0067
        private bool _isRunning;

        public void Start()
        {
            _isRunning = true;
            Task.Run(() =>
            {
                while (_isRunning)
                {
                    Task.Delay(1000).Wait();
                }
            });
        }

        public void Stop()
        {
            _isRunning = false;
        }
    }
}
