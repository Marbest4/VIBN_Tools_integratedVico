using VIBN_Tools.Application.VM;
using static VIBN_Tools.GlobalClasses.Services;

namespace VIBN_Tools.Application.View
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            DataContext = new SettingsPageVM(
                ProjectSettings,
                Connection,
                ViCoFeatureBootstrapper.WorkstationDirectory,
                log: ApplicationLogService.Instance);
        }
    }
}
