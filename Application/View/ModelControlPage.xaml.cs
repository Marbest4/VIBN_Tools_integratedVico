using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VIBN_Tools.Application.View
{
    /// <summary>
    /// Interaction logic for ModelControlPage.xaml
    /// </summary>
    public partial class ModelControlPage : UserControl
    {
        public ModelControlPage()
        {
            InitializeComponent();
        }



        private void SelectOnFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }


        private void ObjectControl_CollapseAll(object sender, RoutedEventArgs e)
        {
            foreach (var expander in FindVisualChildren<Expander>(this))
                expander.IsExpanded = false;
        }

        private void ObjectControl_ExpandAll(object sender, RoutedEventArgs e)
        {
            foreach (var expander in FindVisualChildren<Expander>(this))
                expander.IsExpanded = true;
        }



        private void Expander_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            // Find Scrollviewer
            var parent = FindParent<UIElement>((DependencyObject)sender);
            if (parent == null)
                return;


            var eventargs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender,
            };

            parent.RaiseEvent(eventargs);

        }



        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is T))
                parent = VisualTreeHelper.GetParent(parent);

            return parent as T;
        }





        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    return t;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }


        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                    yield return typedChild;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }


    }
}
