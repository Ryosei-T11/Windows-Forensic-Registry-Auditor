using System;
using System.IO;
using System.Windows;
using ForensicAuditor.Core.StateMachine;
using ForensicAuditor.Engine.Heuristics;
using ForensicAuditor.Engine.Monitors;
using ForensicAuditor.Infrastructure.Win32;
using ForensicAuditor.Infrastructure.Mitigation;
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
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // 1. Setup VM
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // 2. Setup Core/Engine Components
            _stateMachine = new SystemStateMachine();
            _ruleEvaluator = new RuleEvaluator();
            _scorer = new BehaviorScorer();
            _orchestrator = new EventOrchestrator(_ruleEvaluator, _scorer, _stateMachine);

            // Load rules secara asinkron dari folder program
            string rulesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Rules");
            if (!Directory.Exists(rulesPath))
            {
                Directory.CreateDirectory(rulesPath);
            }
            _ruleEvaluator.LoadRulesFromDirectory(rulesPath);

            // 3. Pasangkan Transisi State
            _stateMachine.OnStateChanged += (oldState, newState, reason) =>
            {
                _viewModel.CurrentState = newState;
            };

            // 4. Inisialisasi Monitor (Infrastructure)
            _registryWatcher = new RegNotifyWatcher(
                NativeMethods.HKEY_LOCAL_MACHINE,
                "HKEY_LOCAL_MACHINE",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                _stateMachine
            );

            // 5. Saluran Pipeline Audit
            _registryWatcher.OnRegistryChanged += (regEvent) =>
            {
                regEvent.ProcessPath = ProcessHelper.GetProcessExecutablePath(regEvent.ProcessId);
                _orchestrator.ProcessRawRegistryEvent(regEvent);
            };

            _orchestrator.OnOrchestratedEvent += (evaluated) =>
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
            };

            // 6. Jalankan Tracker
            _registryWatcher.StartMonitoring();
        }

        protected override void OnClosed(EventArgs e)
        {
            _registryWatcher.StopMonitoring();
            base.OnClosed(e);
        }
    }
}