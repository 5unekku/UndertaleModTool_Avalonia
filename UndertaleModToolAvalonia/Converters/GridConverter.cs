using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace UndertaleModTool
{
    public class GridConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(x => x is not double))
                return new Rect();

            return new Rect(0, 0, (double)values[0], (double)values[1]);
        }
    }
}
