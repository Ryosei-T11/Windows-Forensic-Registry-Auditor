using System.Collections.ObjectModel;
using ForensicAuditor.Core.Models;
using ForensicAuditor.Core.StateMachine;

namespace ForensicAuditor.UI.Dashboard.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private SystemState _currentState = SystemState.Safe;
        private double _threatLevel = 0.0;

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

        public void AddNewAlert(RegistryEvent ev)
        {
            if (App.Current == null) return;
            App.Current.Dispatcher.Invoke(() =>
            {
                Alerts.Insert(0, ev);
                ThreatLevel = ev.RiskScore * 10;
            });
        }
    }
}