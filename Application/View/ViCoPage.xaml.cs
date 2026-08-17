using System.Windows.Controls;
using VIBN_Tools.Application;
using VIBN_Tools.Application.VM;

namespace VIBN_Tools.Application.View;

public partial class ViCoPage : UserControl
{
    private readonly ViCoPageVM _viewModel;

    public ViCoPage()
    {
        InitializeComponent();
        _viewModel = ViCoFeatureBootstrapper.CreateViewModel();
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync();
    }
}
