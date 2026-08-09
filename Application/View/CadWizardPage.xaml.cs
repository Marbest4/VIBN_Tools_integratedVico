using VIBN_Tools.Application.VM;
using static VIBN_Tools.GlobalClasses.Services;

namespace VIBN_Tools.Application.View
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class CadWizardPage
    {
        public CadWizardPage()
        {
            InitializeComponent();
            DataContext = new CadWizardPageVM(ProjectSettings);
        }
    }
}
