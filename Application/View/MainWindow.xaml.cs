using System.Windows;
using System.Windows.Controls;
using VIBN_Tools.Application.VM;

namespace VIBN_Tools.Application.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainWindowVM();
            DataContext = vm;

            _ = vm.InitializeAsync();

            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.CanResize;
        }

        // Event for functions that are triggerd by selecting a new TabItem
        private void NavigationBarSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

    }
}
