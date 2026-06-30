using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UndertaleModTool
{
    /// <summary>
    /// compares two numbers and yields <see cref="TrueValue"/>/<see cref="FalseValue"/> (booleans bound to
    /// <c>IsVisible</c>, defaulting to visible/collapsed as in the wpf original).
    /// </summary>
    public class CompareNumbersConverter : IMultiValueConverter
    {
        // these could be overridden on declaration
        public object TrueValue { get; set; } = true;
        public object FalseValue { get; set; } = false;

        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            double a, b;
            try
            {
                a = (double)values[0];
                b = (double)values[1];
            }
            catch
            {
                return null;
            }

            if (parameter is string par)
            {
                int r;
                if (par == ">")      // greater than
                    r = 1;
                else if (par == "<") // less than
                    r = -1;
                else
                    return null;

                bool res = a.CompareTo(b) == r;
                return res ? TrueValue : FalseValue;
            }

            return null;
        }
    }
}
