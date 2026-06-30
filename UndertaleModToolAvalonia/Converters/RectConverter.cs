using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;

namespace UndertaleModTool
{
    public class RectConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            bool ignore = parameter is string par && par == "returnEmptyOnNull";

            if (values.Any(e => e is null || e == AvaloniaProperty.UnsetValue))
            {
                if (ignore)
                    return new Rect(0, 0, 0, 0);
                else
                    return null;
            }

            return new Rect((ushort)values[0], (ushort)values[1], (ushort)values[2], (ushort)values[3]);
        }
    }
}
