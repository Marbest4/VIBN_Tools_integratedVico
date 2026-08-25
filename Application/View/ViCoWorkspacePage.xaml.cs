using System.Windows.Controls;
using VIBN_Tools.Application.VM;

namespace VIBN_Tools.Application.View;

public partial class ViCoWorkspacePage : UserControl
{
    private readonly ViCoWorkspacePageVM _viewModel;

    public ViCoWorkspacePage()
    {
        InitializeComponent();
        _viewModel = ViCoFeatureBootstrapper.CreateWorkspaceViewModel();
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync();
    }
}
