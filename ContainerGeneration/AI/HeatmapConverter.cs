using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace VIBN_Tools.ContainerGeneration.AI
{
    /// <summary>
    /// Converter für Heatmap-Hintergründe in der Confusion-Matrix.
    /// Werte: 0 => Count, 1 => MaxCount, 2 => Actual, 3 => Predicted.
    /// Gleichheit (Actual == Predicted) → Grün-Skala
    /// Ungleichheit → Rot-Skala
    /// </summary>
    public sealed class HeatmapConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int count = SafeInt(values, 0);
            int max = SafeInt(values, 1);
            string actual = values.Length > 2 ? values[2]?.ToString() ?? "" : "";
            string predicted = values.Length > 3 ? values[3]?.ToString() ?? "" : "";

            if (max <= 0) max = 1;

            // 0 -> transparent; vermeidet "Tabellenlook"
            if (count <= 0)
                return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            double ratio = Math.Max(0, Math.Min(1, (double)count / max));

            // Mindestkontrast, damit schwache Zellen nicht "weiß" wirken
            double min = 0.20;
            double rRatio = min + (1 - min) * ratio;

            byte a = 255;
            byte r, g, b = 0;

            if (string.Equals(actual, predicted, StringComparison.OrdinalIgnoreCase))
            {
                // Diagonale: Grünverlauf (dunkler bei hohen Counts)
                r = 0;
                g = (byte)(80 + 175 * rRatio); // 80..255
            }
            else
            {
                // Fehler: Rotverlauf (dunkler bei hohen Counts)
                r = (byte)(100 + 155 * rRatio); // 100..255
                g = 0;
            }

            return new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private static int SafeInt(object[] values, int index)
        {
            if (values.Length <= index || values[index] == null) return 0;
            if (values[index] is int i) return i;
            if (int.TryParse(values[index].ToString(), out var n)) return n;
            return 0;
        }
    }
}