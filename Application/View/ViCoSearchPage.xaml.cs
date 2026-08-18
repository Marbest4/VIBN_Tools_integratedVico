using System.Windows.Controls;
using System.Windows;
using VIBN_Tools.Application.VM;

namespace VIBN_Tools.Application.View;

public partial class ViCoSearchPage : UserControl
{
    private readonly ViCoSearchPageVM _viewModel;

    public ViCoSearchPage()
    {
        InitializeComponent();
        _viewModel = ViCoFeatureBootstrapper.CreateSearchViewModel();
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
