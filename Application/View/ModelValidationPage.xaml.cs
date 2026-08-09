using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.View
{
    /// <summary>
    /// Interaction logic for ModelValidationPage.xaml
    /// </summary>
    public partial class ModelValidationPage : UserControl
    {
        public ModelValidationPage()
        {
            InitializeComponent();
        }


        private void BubbleMouseWheelToScrollViewer(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled)
                return;

            e.Handled = true;

            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            };

            var scrollViewer = VisualTreeHelpers.FindAncestor<ScrollViewer>(sender as DependencyObject);
            scrollViewer?.RaiseEvent(eventArg);
        }



        

        private void AcknowledgeButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var button = (Button)sender;

            if (button.Command != null)
            {
                var param = button.CommandParameter;
                if (button.Command.CanExecute(param))
                    button.Command.Execute(param);
            }

            e.Handled = true; // verhindert, dass das DataGrid die Zeile selektiert
        }








        

    }






    
}
