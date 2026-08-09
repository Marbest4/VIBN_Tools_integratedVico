using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace VIBN_Tools.GlobalClasses
{
    public static class ListBoxSelectedItemsBehavior
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(IList),
                typeof(ListBoxSelectedItemsBehavior),
                new PropertyMetadata(null, OnSelectedItemsChanged));

        public static void SetSelectedItems(DependencyObject obj, IList value)
            => obj.SetValue(SelectedItemsProperty, value);

        public static IList GetSelectedItems(DependencyObject obj)
            => (IList)obj.GetValue(SelectedItemsProperty);

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                listBox.SelectionChanged -= ListBox_SelectionChanged;
                listBox.SelectionChanged += ListBox_SelectionChanged;
            }
        }

        private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                var boundList = GetSelectedItems(listBox);
                if (boundList == null) return;

                boundList.Clear();
                foreach (var item in listBox.SelectedItems)
                    boundList.Add(item);
            }
        }
    }







    public static class SlotValidationHelper
    {
        public enum SlotValueCompare
        {
            Equal,
            Different,
            Greater,
            GreaterEqual,
            Smaller,
            SmallerEqual
        }


        public static async Task<bool> CheckSlotValueNotFalse(Guid guid, string slot1)
        {
            var value = Services.ApiInstance.XmlHelper.ConvertToString(await Services.ApiInstance.Object.GetSlotValueAsync(guid, slot1));

            return bool.TryParse(value, out var result) && result;
        }

        public static async Task<bool> CheckSlotValueNotZero(Guid guid, string slot1)
        {
            var value = Services.ApiInstance.XmlHelper.ConvertToString(await Services.ApiInstance.Object.GetSlotValueAsync(guid, slot1));

            if (int.TryParse(value, out var intResult))
                return intResult != 0;

            if (float.TryParse(value, out var floatResult))
                return Math.Abs(floatResult) > float.Epsilon;

            return true; // no number value -> not zero
        }

        public static async Task<bool> CompareSlotValues(Guid guid, string slot1, string slot2, SlotValueCompare compare)
        {
            var value1 = Services.ApiInstance.XmlHelper.ConvertToString(await Services.ApiInstance.Object.GetSlotValueAsync(guid, slot1));
            var value2 = Services.ApiInstance.XmlHelper.ConvertToString(await Services.ApiInstance.Object.GetSlotValueAsync(guid, slot2));

            if (compare == SlotValueCompare.Equal) return value1 == value2;
            if (compare == SlotValueCompare.Different) return value1 != value2;

            if (int.TryParse(value1, out var n1) && int.TryParse(value2, out var n2))
            {
                return compare switch
                {
                    SlotValueCompare.Greater => n1 > n2,
                    SlotValueCompare.GreaterEqual => n1 >= n2,
                    SlotValueCompare.Smaller => n1 < n2,
                    SlotValueCompare.SmallerEqual => n1 <= n2,
                    _ => false
                };
            }

            return false;
        }
    }






    public class ViewElementTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextBlockTemplate { get; set; }
        public DataTemplate TextBoxTemplate { get; set; }
        public DataTemplate ComboBoxTemplate { get; set; }
        public DataTemplate CheckBoxTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            System.Diagnostics.Debug.WriteLine($"SelectTemplate called. Item: {item?.GetType().Name ?? "null"}");
            if (item is OptionsViewModelBase field)
            {
                return field.ViewElement switch
                {
                    ViewElement.Textblock => TextBlockTemplate,
                    ViewElement.Textbox => TextBoxTemplate,
                    ViewElement.Combobox => ComboBoxTemplate,
                    ViewElement.Checkbox => CheckBoxTemplate,
                    _ => base.SelectTemplate(item, container)
                };
            }

            return base.SelectTemplate(item, container);
        }
    }




}
