using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UndertaleModTool
{
    /// <summary>
    /// maps a boolean to a visibility (an <c>IsVisible</c> bool in avalonia). the configurable
    /// <see cref="trueValue"/>/<see cref="falseValue"/> mirror the wpf converter's visibility outputs,
    /// collapsed/hidden both becoming <c>false</c>.
    /// </summary>
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public bool trueValue { get; set; } = true;
        public bool falseValue { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool boolean && boolean) ? trueValue : falseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
