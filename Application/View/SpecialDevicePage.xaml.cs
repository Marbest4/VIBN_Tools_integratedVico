using System.Windows.Controls;
using System.Windows.Input;

namespace VIBN_Tools.Application.View
{
    /// <summary>
    /// Interaction logic for SpecialDevicePage.xaml
    /// </summary>
    public partial class SpecialDevicePage : UserControl
    {
        public SpecialDevicePage()
        {
            InitializeComponent();
        }

        private void SelectOnFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void SelectOnFocus(object sender, MouseEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }
    }
}
