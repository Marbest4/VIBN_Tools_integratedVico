using System.Windows;
using System.Windows.Controls;
using VIBN_Tools.Application.VM;

namespace VIBN_Tools.Application.View;

/// <summary>Hosts the Special Device view model and disposes its TIA bridge at application exit.</summary>
public partial class SpecialDevicePage : UserControl
{
    private readonly SpecialDevicePageVM _viewModel;
    private bool _disposed;

    public SpecialDevicePage()
    {
        InitializeComponent();
        _viewModel = ViCoFeatureBootstrapper.CreateSpecialDeviceViewModel();
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
