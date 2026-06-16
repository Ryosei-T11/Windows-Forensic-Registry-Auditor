using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using ForensicAuditor.Core.StateMachine;
using System.Diagnostics;
using ForensicAuditor.Engine.Heuristics;
using ForensicAuditor.Engine.Monitors;
using ForensicAuditor.Infrastructure.Win32;
using ForensicAuditor.Infrastructure.Mitigation;
using ForensicAuditor.Infrastructure.Reputation;
using ForensicAuditor.UI.Dashboard.ViewModels;

namespace ForensicAuditor.UI.Dashboard.Views
{
    public partial class MainWindow : Window
    {
        private readonly SystemStateMachine _stateMachine;
        private readonly RuleEvaluator _ruleEvaluator;
        private readonly BehaviorScorer _scorer;
        private readonly EventOrchestrator _orchestrator;
        private readonly RegNotifyWatcher _registryWatcher;
        private readonly ForensicAuditor.Infrastructure.ETW.RegistryEtwTracer _etwTracer;
        private readonly MainViewModel _viewModel;
        private readonly VirusTotalClient _vtClient;

        public MainWindow()
        {
            // Configure logging early
            ForensicAuditor.Infrastructure.Logging.LoggingConfig.Configure();

            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            var hklmNode = new RegistryNode { Name = "HKEY_LOCAL_MACHINE" };
            hklmNode.Children.Add(new RegistryNode { Name = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run" });
            hklmNode.Children.Add(new RegistryNode { Name = "SYSTEM\\CurrentControlSet\\Services" });
            var hkcuNode = new RegistryNode { Name = "HKEY_CURRENT_USER" };
            hkcuNode.Children.Add(new RegistryNode { Name = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run" });
            _viewModel.RootNodes.Add(hklmNode);
            _viewModel.RootNodes.Add(hkcuNode);

            _stateMachine = new SystemStateMachine();
            _ruleEvaluator = new RuleEvaluator();
            _scorer = new BehaviorScorer();
            _orchestrator = new EventOrchestrator(_ruleEvaluator, _scorer, _stateMachine);
            // Initialize lightweight ETW tracer for correlation (POC).
            _etwTracer = new ForensicAuditor.Infrastructure.ETW.RegistryEtwTracer();
            bool etwStarted = _etwTracer.Start();
            _vtClient = new VirusTotalClient(apiKey: "");

            string rulesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Rules");
            if (!Directory.Exists(rulesPath))
            {
                Directory.CreateDirectory(rulesPath);
            }
            _ruleEvaluator.LoadRulesFromDirectory(rulesPath);

            // Initialize OpenTelemetry tracing (console + optional OTLP) using env/config
            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
            var _tracerProvider = ForensicAuditor.Infrastructure.Tracing.OpenTelemetryIntegration.RegisterOpenTelemetry(config);

            _stateMachine.OnStateChanged += (oldState, newState, reason) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    _viewModel.CurrentState = newState;
                });
            };

            _registryWatcher = new RegNotifyWatcher(
                new IntPtr(-2147483646),
                "HKEY_LOCAL_MACHINE",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                _stateMachine
            );

            _registryWatcher.OnRegistryChanged += (regEvent) =>
            {
                // Propagate current Activity context into the background task
                var capturedActivity = Activity.Current;
                ForensicAuditor.Infrastructure.Async.ActivityTask.Run(async () =>
                {
                    // Try to correlate with ETW tracer if available
                    if (_etwTracer.TryGetProcessIdForKey(regEvent.SubKeyPath, out int pid) && pid > 0)
                    {
                        regEvent.ProcessId = pid;
                    }

                    regEvent.ProcessPath = ProcessHelper.GetProcessExecutablePath(regEvent.ProcessId);
                    regEvent.Sha256Hash = ProcessHelper.CalculateSha256(regEvent.ProcessPath);
                    regEvent.ReputationResult = await _vtClient.CheckHashReputationAsync(regEvent.Sha256Hash);
                    _orchestrator.ProcessRawRegistryEvent(regEvent);
                });
            };

            _orchestrator.OnOrchestratedEvent += (evaluated) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    _viewModel.AddNewAlert(evaluated);

                    // Mitigasi Otomatis Terhadap Ransomware/Tamper Aktif
                    if (evaluated.RiskScore >= 8.0)
                    {
                        var rollback = new RegistryRollbackEngine();
                        rollback.PerformRollback(evaluated);

                        var quarantine = new ProcessQuarantine();
                        quarantine.SuspendThreatProcess(evaluated.ProcessId);
                    }
                });
            };

            _registryWatcher.StartMonitoring();
        }

        private void SimulateSafe_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                var mockEvent = new ForensicAuditor.Core.Models.RegistryEvent
                {
                    Hive = "HKEY_LOCAL_MACHINE",
                    SubKeyPath = @"\Registry\Machine\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                    ValueName = "OneDriveLauncher",
                    NewValue = "C:\\Windows\\System32\\OneDrive.exe",
                    ProcessId = 1204,
                    ProcessName = "explorer.exe",
                    ProcessPath = "C:\\Windows\\System32\\explorer.exe",
                    IsProcessSigned = true,
                    ProcessSigner = "Microsoft Windows Publisher"
                };

                mockEvent.Sha256Hash = "8A4C3E59B32E16D7130FCE8E711A4F4F3D6C11234F98E12A34B22C3D3E2F4A5E";
                mockEvent.ReputationResult = await _vtClient.CheckHashReputationAsync(mockEvent.Sha256Hash);
                _orchestrator.ProcessRawRegistryEvent(mockEvent);
            });
        }

        private void SimulateSuspect_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                var mockEvent = new ForensicAuditor.Core.Models.RegistryEvent
                {
                    Hive = "HKEY_LOCAL_MACHINE",
                    SubKeyPath = @"\Registry\Machine\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                    ValueName = "UnknownUpdater",
                    NewValue = "C:\\Users\\Public\\updater.exe",
                    ProcessId = 5540,
                    ProcessName = "updater.exe",
                    ProcessPath = "C:\\Users\\Public\\updater.exe",
                    IsProcessSigned = false,
                    ProcessSigner = null
                };

                mockEvent.Sha256Hash = "FB4C3E59B32E16D7130FCE8E711A4F4F3D6C11234F98E12A34B22C3D3E2F4A5E";
                mockEvent.ReputationResult = await _vtClient.CheckHashReputationAsync(mockEvent.Sha256Hash);
                _orchestrator.ProcessRawRegistryEvent(mockEvent);
            });
        }

        private void SimulateAttack_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                var mockEvent = new ForensicAuditor.Core.Models.RegistryEvent
                {
                    Hive = "HKEY_LOCAL_MACHINE",
                    SubKeyPath = @"\Registry\Machine\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                    ValueName = "MaliciousPersistence",
                    NewValue = "C:\\Users\\HP\\AppData\\Local\\Temp\\crypto_miner.exe",
                    ProcessId = 8812,
                    ProcessName = "crypto_miner.exe",
                    ProcessPath = "C:\\Users\\HP\\AppData\\Local\\Temp\\crypto_miner.exe",
                    IsProcessSigned = false,
                    ProcessSigner = null
                };

                mockEvent.Sha256Hash = "D24C3E59B32E16D7130FCE8E711A4F4F3D6C11234F98E12A34B22C3D3E2F4A5E";
                mockEvent.ReputationResult = await _vtClient.CheckHashReputationAsync(mockEvent.Sha256Hash);
                _orchestrator.ProcessRawRegistryEvent(mockEvent);
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _registryWatcher.StopMonitoring();
            _etwTracer?.Dispose();
            base.OnClosed(e);
        }
    }
}