using System.Windows.Controls;
using System.Windows;
using VIBN_Tools.Application.VM;

namespace VIBN_Tools.Application.View;

/// <summary>
/// Hosts the safe VIBN-to-workplace synchronization and the optional manual
/// Kanbanize card workflow.
/// </summary>
public partial class KanbanizeCardPage : UserControl
{
    private readonly KanbanizeCardPageVM _viewModel;

    public KanbanizeCardPage()
    {
        InitializeComponent();
        _viewModel = ViCoFeatureBootstrapper.CreateKanbanizeCardViewModel();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        if (System.Windows.Application.Current is not null)
            System.Windows.Application.Current.Exit += OnApplicationExit;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync();
    }

    private void OnApplicationExit(object sender, ExitEventArgs e)
    {
        if (System.Windows.Application.Current is not null)
            System.Windows.Application.Current.Exit -= OnApplicationExit;
        _viewModel.Dispose();
    }
}
