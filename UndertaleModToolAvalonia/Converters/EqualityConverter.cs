using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UndertaleModTool
{
    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
                return values;

            if (values.Count < 2)
                return false;

            bool invert = parameter is string par && par == "invert";
            // wpf original used reference equality (object ==); keep that semantic
            return ReferenceEquals(values[0], values[1]) ^ invert;
        }
    }
}
