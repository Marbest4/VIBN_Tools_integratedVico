using System.Windows.Controls;
using System.Windows;
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
        if (System.Windows.Application.Current is not null)
            System.Windows.Application.Current.Exit += OnApplicationExit;
    }

    private async void OnApplicationExit(object sender, ExitEventArgs e)
    {
        if (_disposed)
            return;

        _disposed = true;
        if (System.Windows.Application.Current is not null)
            System.Windows.Application.Current.Exit -= OnApplicationExit;
        await _viewModel.DisposeAsync();
    }
}
