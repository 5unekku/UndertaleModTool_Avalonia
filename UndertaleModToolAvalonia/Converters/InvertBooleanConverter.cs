using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UndertaleModTool
{
    public sealed class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is not bool boolean || !boolean;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is not bool boolean || !boolean;
        }
    }
}
