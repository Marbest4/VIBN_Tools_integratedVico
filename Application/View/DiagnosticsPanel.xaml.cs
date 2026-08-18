using System.Windows.Controls;
using VIBN_Tools.Application.VM;

namespace VIBN_Tools.Application.View;

public partial class DiagnosticsPanel : UserControl
{
    public DiagnosticsPanel()
    {
        InitializeComponent();
        DataContext = new DiagnosticsPanelVM();
    }
}
