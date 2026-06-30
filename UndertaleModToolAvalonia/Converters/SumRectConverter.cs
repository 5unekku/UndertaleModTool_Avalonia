using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;

namespace UndertaleModTool
{
    public class SumRectConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(e => e is null || e == AvaloniaProperty.UnsetValue))
            {
                return null;
            }
            return new Rect((ushort)values[0] + (uint)values[1], (ushort)values[2] + (uint)values[3], (uint)values[4], (uint)values[5]);
        }
    }
}
