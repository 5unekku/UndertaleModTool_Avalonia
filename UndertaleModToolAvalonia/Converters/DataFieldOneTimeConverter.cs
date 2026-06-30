using System;
using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;
using UndertaleModLib;

namespace UndertaleModTool
{
    /// <summary>
    /// reads a named boolean field off <see cref="UndertaleData"/> once and yields it as an <c>IsVisible</c> bool.
    /// </summary>
    public class DataFieldOneTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not UndertaleData data || parameter is not string par)
                return null;

            FieldInfo info = data.GetType().GetField(par);
            object resObj = info?.GetValue(data);

            if (resObj is bool res)
                return res;
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
