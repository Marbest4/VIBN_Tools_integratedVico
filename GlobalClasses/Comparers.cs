using System.Collections;
using System.Text.RegularExpressions;

namespace VIBN_Tools.GlobalClasses
{
    // Class for Comparing string naturally, e.g. File1, File2, File10
    public class NaturalStringComparer : IComparer
    {
        public int Compare(object? x, object? y)
        {
            string xStr = x?.ToString() ?? string.Empty;
            string yStr = y?.ToString() ?? string.Empty;

            var regex = new Regex(@"\d+|\D+");

            var xParts = regex.Matches(xStr);
            var yParts = regex.Matches(yStr);

            int count = Math.Min(xParts.Count, yParts.Count);
            for (int i = 0; i < count; i++)
            {
                string xPart = xParts[i].Value;
                string yPart = yParts[i].Value;

                // compare numerical if both values are numbers
                if (int.TryParse(xPart, out int xNum) && int.TryParse(yPart, out int yNum))
                {
                    int cmp = xNum.CompareTo(yNum);
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                }
                else
                {
                    int cmp = string.Compare(xPart, yPart, StringComparison.OrdinalIgnoreCase);
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                }
            }

            // If String is longer
            return xParts.Count.CompareTo(yParts.Count);
        }

    }


    public class NaturalStringComparer<T> : IComparer<T>
    {
        private readonly Func<T, string> _selector;

        public NaturalStringComparer(Func<T, string> selector)
        {
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        }

        public int Compare(T x, T y)
        {
            string xStr = _selector(x) ?? string.Empty;
            string yStr = _selector(y) ?? string.Empty;

            var regex = new Regex(@"\d+|\D+");

            var xParts = regex.Matches(xStr);
            var yParts = regex.Matches(yStr);

            int count = Math.Min(xParts.Count, yParts.Count);
            for (int i = 0; i < count; i++)
            {
                string xPart = xParts[i].Value;
                string yPart = yParts[i].Value;

                if (int.TryParse(xPart, out int xNum) && int.TryParse(yPart, out int yNum))
                {
                    int cmp = xNum.CompareTo(yNum);
                    if (cmp != 0)
                        return cmp;
                }
                else
                {
                    int cmp = string.Compare(xPart, yPart, StringComparison.OrdinalIgnoreCase);
                    if (cmp != 0)
                        return cmp;
                }
            }

            return xParts.Count.CompareTo(yParts.Count);
        }
    }



}
