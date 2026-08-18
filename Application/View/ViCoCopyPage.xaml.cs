using System.Windows.Controls;
using VIBN_Tools.Application.VM;

namespace VIBN_Tools.Application.View;

public partial class ViCoCopyPage : UserControl
{
    private readonly ViCoCopyPageVM _viewModel;

    public ViCoCopyPage()
    {
        InitializeComponent();
        _viewModel = ViCoFeatureBootstrapper.CreateCopyViewModel();
        DataContext = _viewModel;
        Loaded += (_, _) => _viewModel.ApplyWorkspaceSelection();
        Unloaded += (_, _) => _viewModel.Dispose();
    }
}
