using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Security.Principal;
using Microsoft.Diagnostics.Tracing.Session;
using System.Linq;
using Serilog;

namespace ForensicAuditor.Infrastructure.ETW
{
    /// ETW tracer POC: if Microsoft.Diagnostics.Tracing.TraceEvent is available at runtime,
    /// this class may attempt to use it. Otherwise it gracefully remains disabled.
    /// Current implementation is a hardened placeholder that keeps a small in-memory mapping
    /// if external ETW wiring is provided; Start() returns false when ETW cannot be used.
    public class RegistryEtwTracer : IDisposable
    {
        private readonly ConcurrentDictionary<string, (int Pid, DateTime Timestamp)> _keyToPid = new(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource? _cts;
        private TraceEventSession? _session;
        private readonly TimeSpan _entryLifetime = TimeSpan.FromMinutes(2);
        private readonly int _maxEntries = 2000;
        private bool _started = false;

        private void HandleRegistryEvent(int pid, string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key)) return;

                string normalized = NormalizeKey(key);
                if (string.IsNullOrEmpty(normalized)) return;

                var entry = (Pid: pid, Timestamp: DateTime.UtcNow);
                _keyToPid[normalized] = entry;

                if (_keyToPid.Count > _maxEntries)
                {
                    PruneOldEntries();
                }
            }
            catch { }
        }

        private void PruneOldEntries()
        {
            try
            {
                DateTime threshold = DateTime.UtcNow - _entryLifetime;
                foreach (var kv in _keyToPid.ToArray())
                {
                    if (kv.Value.Timestamp < threshold)
                    {
                        _keyToPid.TryRemove(kv.Key, out _);
                    }
                }

                if (_keyToPid.Count <= _maxEntries) return;

                var ordered = _keyToPid.OrderBy(kv => kv.Value.Timestamp).ToArray();
                foreach (var kv in ordered)
                {
                    if (_keyToPid.Count <= _maxEntries) break;
                    _keyToPid.TryRemove(kv.Key, out _);
                }
            }
            catch { }
        }

        public bool Start()
        {
            // Only enable full ETW wiring when running elevated
            if (!IsElevated())
            {
                EtwTracerEventSource.Log.TracerStartedNotElevated();
                Log.Warning("RegistryEtwTracer not elevated; skipping ETW session start");
                return false;
            }

            try
            {
                // Ensure TraceEvent assembly is available
                var asm = Assembly.Load("Microsoft.Diagnostics.Tracing.TraceEvent");
                if (asm == null) return false;

                // Create kernel session and enable registry keyword
                _session = new TraceEventSession("ForensicAuditor_Registry_POC");

                // Enable kernel provider via reflection to avoid compile dependency issues
                try
                {
                    var enableKernel = typeof(TraceEventSession).GetMethod("EnableKernelProvider", new[] { typeof(ulong) });
                    if (enableKernel != null)
                    {
                        // Kernel registry keyword value (0x100000) varies by OS; use common Registry flag from TraceEvent
                        enableKernel.Invoke(_session, new object[] { (ulong)0x100000 });
                    }
                    else
                    {
                        // fallback: attempt to call overload with two keywords (none)
                        try
                        {
                            var method = typeof(TraceEventSession).GetMethod("EnableKernelProvider", new[] { typeof(ulong), typeof(ulong) });
                            method?.Invoke(_session, new object[] { 0UL, 0UL });
                        }
                        catch { }
                    }
                }
                catch { }

                // Subscribe to all events and heuristically extract registry-related payloads
                _session.Source.AllEvents += traceEvent =>
                {
                    try
                    {
                        int pid = (int)traceEvent.ProcessID;
                        string found = null;
                        var names = traceEvent.PayloadNames;
                        if (names != null)
                        {
                            foreach (var name in names)
                            {
                                try
                                {
                                    var payload = traceEvent.PayloadByName(name)?.ToString();
                                    if (string.IsNullOrEmpty(payload)) continue;
                                    if (payload.IndexOf("Registry", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        payload.IndexOf("SOFTWARE\\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        payload.IndexOf("SYSTEM\\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        payload.IndexOf("HKEY_", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        found = payload;
                                        break;
                                    }
                                }
                                catch { }
                            }
                        }

                        if (string.IsNullOrEmpty(found) && names != null)
                        {
                            foreach (var name in names)
                            {
                                try
                                {
                                    var payload = traceEvent.PayloadByName(name)?.ToString();
                                    if (!string.IsNullOrEmpty(payload))
                                    {
                                        found = payload;
                                        break;
                                    }
                                }
                                catch { }
                            }
                        }

                        if (!string.IsNullOrEmpty(found))
                        {
                            HandleRegistryEvent(pid, found);
                        }
                    }
                    catch { }
                };

                _cts = new CancellationTokenSource();
                Task.Run(() => _session.Source.Process(), _cts.Token);

                // Start expiration loop
                Task.Run(async () =>
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(5), _cts.Token);
                            PruneOldEntries();
                        }
                        catch (TaskCanceledException) { break; }
                        catch { }
                    }
                }, _cts.Token);

                _started = true;
                EtwTracerEventSource.Log.TracerStarted();
                EtwTracerEventSource.Log.SessionEnabled("ForensicAuditor_Registry_POC");
                Log.Information("RegistryEtwTracer started, session {Session}", "ForensicAuditor_Registry_POC");
                return true;
            }
            catch
            {
                Stop();
                _started = false;
                EtwTracerEventSource.Log.TracerFailed("Exception creating ETW session");
                Log.Error("Failed to create ETW session: {Error}", "Exception creating ETW session");
                return false;
            }
        }

        private static bool IsElevated()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        public bool TryGetProcessIdForKey(string keyPath, out int processId)
        {
            processId = -1;
            if (!_started) return false;

            if (string.IsNullOrEmpty(keyPath)) return false;

            string normalizedQuery = NormalizeKey(keyPath);
            if (string.IsNullOrEmpty(normalizedQuery)) return false;

            DateTime threshold = DateTime.UtcNow - _entryLifetime;

            if (_keyToPid.TryGetValue(normalizedQuery, out var exact) && exact.Timestamp >= threshold)
            {
                processId = exact.Pid;
                return true;
            }

            // fallback: suffix match
            foreach (var kv in _keyToPid)
            {
                if (kv.Value.Timestamp < threshold) continue;
                if (normalizedQuery.EndsWith(kv.Key, StringComparison.OrdinalIgnoreCase) || kv.Key.EndsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                {
                    processId = kv.Value.Pid;
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeKey(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            string s = raw.Trim();
            s = s.Replace("\\Registry\\Machine\\", "");
            s = s.Replace("\\REGISTRY\\MACHINE\\", "");
            s = s.Replace("\\Registry\\User\\", "");
            s = s.Replace("\\REGISTRY\\USER\\", "");
            while (s.StartsWith("\\")) s = s.Substring(1);
            s = s.Replace('/', '\\').Trim();
            return s.ToLowerInvariant();
        }

        public void Stop()
        {
            try { _cts?.Cancel(); }
            catch { }
            finally { _cts = null; _keyToPid.Clear(); _started = false; }
        }

        public void Dispose() => Stop();
    }
}

