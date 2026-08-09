using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using VIBN_Tools.Application.VM;

namespace VIBN_Tools.Application.View
{
    /// <summary>
    /// Interaction logic for ContainerGenerationPage.xaml
    /// </summary>
    public partial class ContainerGenerationPage : UserControl
    {
        public ContainerGenerationPage()
        {
            InitializeComponent();

            Loaded += View_Loaded;
        }

        private void View_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ContainerGenerationPageVM vm)
            {
                vm.OnViewLoaded();
            }
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            ToggleButton toggleButton = comboBox.Template.FindName("toggleButton", comboBox) as ToggleButton;
            if (toggleButton != null)
            {
                toggleButton.BorderThickness = new Thickness(0, 0, 0, 0);
                Border border = toggleButton.Template.FindName("templateRoot", toggleButton) as Border;
                if (border != null)
                {
                    border.Background = comboBox.Background;
                }

            }
        }


    }
}
