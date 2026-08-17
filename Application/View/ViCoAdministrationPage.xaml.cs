using System.Windows.Controls;
using VIBN_Tools.Application.VM;

namespace VIBN_Tools.Application.View;

public partial class ViCoAdministrationPage : UserControl
{
    private readonly ViCoAdministrationPageVM _viewModel;

    public ViCoAdministrationPage()
    {
        InitializeComponent();
        _viewModel = ViCoFeatureBootstrapper.CreateAdministrationViewModel();
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync();
    }
}
