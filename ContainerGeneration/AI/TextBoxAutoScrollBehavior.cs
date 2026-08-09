using System.Windows;
using System.Windows.Controls;

namespace VIBN_Tools.ContainerGeneration.AI
{
    /// <summary>
    /// Attached Behavior: ScrollToEnd bei Textänderung der TextBox.
    /// </summary>
    public static class TextBoxAutoScrollBehavior
    {
        public static readonly DependencyProperty AutoScrollToEndProperty =
            DependencyProperty.RegisterAttached(
                "AutoScrollToEnd",
                typeof(bool),
                typeof(TextBoxAutoScrollBehavior),
                new PropertyMetadata(false, OnAutoScrollToEndChanged));

        public static bool GetAutoScrollToEnd(DependencyObject obj)
            => (bool)obj.GetValue(AutoScrollToEndProperty);

        public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
            => obj.SetValue(AutoScrollToEndProperty, value);

        private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox tb) return;

            if ((bool)e.NewValue)
                tb.TextChanged += Tb_TextChanged;
            else
                tb.TextChanged -= Tb_TextChanged;
        }

        private static void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.ScrollToEnd();
        }
    }
}