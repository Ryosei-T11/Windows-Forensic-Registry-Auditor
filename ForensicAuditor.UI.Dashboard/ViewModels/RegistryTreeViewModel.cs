using System.Collections.ObjectModel;

namespace ForensicAuditor.UI.Dashboard.ViewModels
{
    public class RegistryNode
    {
        public string Name { get; set; } = string.Empty;
        public ObservableCollection<RegistryNode> Children { get; } = new();
    }

    public class RegistryTreeViewModel : BaseViewModel
    {
        public ObservableCollection<RegistryNode> RootNodes { get; } = new();
    }
}
