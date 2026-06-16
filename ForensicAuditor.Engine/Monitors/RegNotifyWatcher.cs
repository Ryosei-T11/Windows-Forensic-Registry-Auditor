using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ForensicAuditor.Core.Interfaces;
using ForensicAuditor.Core.Models;
using ForensicAuditor.Core.StateMachine;

namespace ForensicAuditor.Infrastructure.Win32
{
    public class RegNotifyWatcher : IRegistryMonitor, IDisposable
    {
        private readonly IntPtr _rootHive;
        private readonly string _subKeyPath;
        private readonly string _hiveName;
        private readonly SystemStateMachine _stateMachine;

        private CancellationTokenSource? _cts;
        private bool _isDisposed;

        public event Action<RegistryEvent>? OnRegistryChanged;

        public RegNotifyWatcher(IntPtr rootHive, string hiveName, string subKeyPath, SystemStateMachine stateMachine)
        {
            _rootHive = rootHive;
            _hiveName = hiveName;
            _subKeyPath = subKeyPath;
            _stateMachine = stateMachine;
        }

        public void StartMonitoring()
        {
            if (_cts != null) return;
            Log.Information("RegNotifyWatcher starting for {Hive} {SubKey}", _hiveName, _subKeyPath);
            _cts = new CancellationTokenSource();
            Task.Run(() => MonitorLoop(_cts.Token));
        }

        public void StopMonitoring()
        {
            Log.Information("RegNotifyWatcher stopping for {Hive} {SubKey}", _hiveName, _subKeyPath);
            _cts?.Cancel();
            _cts = null;
        }

        private void MonitorLoop(CancellationToken token)
        {
            int result = NativeMethods.RegOpenKeyEx(_rootHive, _subKeyPath, 0, NativeMethods.KEY_READ, out IntPtr hKey);
            if (result != 0)
            {
                Log.Error("RegOpenKeyEx failed for {Hive} {SubKey} with code {Result}", _hiveName, _subKeyPath, result);
                return;
            }

            using AutoResetEvent notifyEvent = new(false);
            IntPtr eventHandle = notifyEvent.SafeWaitHandle.DangerousGetHandle();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    result = NativeMethods.RegNotifyChangeKeyValue(
                        hKey,
                        bWatchSubtree: true,
                        dwNotifyFilter: NativeMethods.REG_NOTIFY_CHANGE_NAME | NativeMethods.REG_NOTIFY_CHANGE_LAST_SET,
                        hEvent: eventHandle,
                        fAsynchronous: true);

                    if (result != 0)
                    {
                        Log.Error("RegNotifyChangeKeyValue returned error {Result} for {Hive} {SubKey}", result, _hiveName, _subKeyPath);
                        break;
                    }

                    int waitResult = WaitHandle.WaitAny(new[] { notifyEvent, token.WaitHandle });

                    if (waitResult == 0)
                    {
                        var regEvent = new RegistryEvent
                        {
                            Hive = _hiveName,
                            SubKeyPath = _subKeyPath,
                            Timestamp = DateTime.Now
                        };
                        Log.Information("Registry change detected: EventId={EventId} Correlation={Correlation} {Hive} {SubKey} at {Timestamp}", regEvent.EventId, regEvent.CorrelationId, _hiveName, _subKeyPath, regEvent.Timestamp);
                        OnRegistryChanged?.Invoke(regEvent);
                    }
                }
            }
            finally
            {
                NativeMethods.RegCloseKey(hKey);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            StopMonitoring();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}