using System.Windows.Controls;
using VIBN_Tools.Application;
using VIBN_Tools.Application.VM;

namespace VIBN_Tools.Application.View;

public partial class TiaPortalPage : UserControl
{
    private readonly TiaPortalPageVM _viewModel;
    private bool _disposed;

    public TiaPortalPage()
    {
        InitializeComponent();
        _viewModel = ViCoFeatureBootstrapper.CreateTiaPortalViewModel();
        DataContext = _viewModel;
        Unloaded += OnUnloaded;
    }

    private async void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_disposed)
            return;

        _disposed = true;
        await _viewModel.DisposeAsync();
    }
}
