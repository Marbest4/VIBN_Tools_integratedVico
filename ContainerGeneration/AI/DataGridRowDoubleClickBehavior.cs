using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VIBN_Tools.ContainerGeneration.AI
{
    /// <summary>
    /// Attached Behavior: Macht eine DataGridRow doppelklickbar via ICommand.
    /// </summary>
    public static class DataGridRowDoubleClickBehavior
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached(
                "Command",
                typeof(ICommand),
                typeof(DataGridRowDoubleClickBehavior),
                new PropertyMetadata(null, OnCommandChanged));

        public static ICommand GetCommand(DependencyObject obj)
            => (ICommand)obj.GetValue(CommandProperty);

        public static void SetCommand(DependencyObject obj, ICommand value)
            => obj.SetValue(CommandProperty, value);

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGridRow row) return;

            if (e.NewValue is ICommand)
                row.MouseDoubleClick += Row_MouseDoubleClick;
            else
                row.MouseDoubleClick -= Row_MouseDoubleClick;
        }

        private static void Row_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGridRow row) return;

            var cmd = GetCommand(row);
            if (cmd == null) return;

            var parameter = row.Item;

            if (cmd.CanExecute(parameter))
                cmd.Execute(parameter);
        }
    }
}