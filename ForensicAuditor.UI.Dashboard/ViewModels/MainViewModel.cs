using System.Collections.ObjectModel;
using ForensicAuditor.Core.Models;
using ForensicAuditor.Core.StateMachine;

namespace ForensicAuditor.UI.Dashboard.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private SystemState _currentState = SystemState.Safe;
        private double _threatLevel = 0.0;
        private string _searchText = string.Empty;
        private string _selectedSeverity = "All";

        public System.Collections.ObjectModel.ObservableCollection<string> SeverityOptions { get; } = new()
        {
            "All",
            "Critical",
            "High",
            "Medium",
            "Low",
            "Informational",
            "Suspicious"
        };

        private System.ComponentModel.ICollectionView? _alertsView;
        public System.ComponentModel.ICollectionView? AlertsView
        {
            get => _alertsView;
            private set { _alertsView = value; OnPropertyChanged(); }
        }

        public SystemState CurrentState
        {
            get => _currentState;
            set { _currentState = value; OnPropertyChanged(); }
        }

        public double ThreatLevel
        {
            get => _threatLevel;
            set { _threatLevel = value; OnPropertyChanged(); }
        }

        public ObservableCollection<RegistryEvent> Alerts { get; } = new();

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); AlertsView?.Refresh(); }
        }

        public string SelectedSeverity
        {
            get => _selectedSeverity;
            set { _selectedSeverity = value; OnPropertyChanged(); AlertsView?.Refresh(); }
        }

        // Registry tree root nodes used by MainWindow
        public ObservableCollection<ForensicAuditor.UI.Dashboard.ViewModels.RegistryNode> RootNodes { get; } = new();

        public void AddNewAlert(RegistryEvent ev)
        {
            if (App.Current == null) return;
            App.Current.Dispatcher.Invoke(() =>
            {
                Alerts.Insert(0, ev);
                ThreatLevel = ev.RiskScore * 10;
            });
        }

        public MainViewModel()
        {
            // Setup a filtered view over Alerts
            AlertsView = System.Windows.Data.CollectionViewSource.GetDefaultView(Alerts);
            if (AlertsView != null)
            {
                AlertsView.Filter = FilterAlert;
            }

            // Refresh when collection changes
            Alerts.CollectionChanged += (s, e) => AlertsView?.Refresh();
        }

        private bool FilterAlert(object obj)
        {
            if (obj is not RegistryEvent ev) return false;

            // Severity filter
            if (!string.IsNullOrEmpty(SelectedSeverity) && SelectedSeverity != "All")
            {
                if (SelectedSeverity == "Suspicious")
                {
                    if (!(ev.Severity == Core.Models.SeverityLevel.Medium || ev.Severity == Core.Models.SeverityLevel.High))
                        return false;
                }
                else if (System.Enum.TryParse<Core.Models.SeverityLevel>(SelectedSeverity, out var sev))
                {
                    if (ev.Severity != sev) return false;
                }
            }

            // Search filter (process name, hash, registry path)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var q = SearchText.ToLowerInvariant();
                bool match = (ev.ProcessName?.ToLowerInvariant().Contains(q) ?? false)
                    || (ev.Sha256Hash?.ToLowerInvariant().Contains(q) ?? false)
                    || (ev.SubKeyPath?.ToLowerInvariant().Contains(q) ?? false);
                if (!match) return false;
            }

            return true;
        }
    }
}