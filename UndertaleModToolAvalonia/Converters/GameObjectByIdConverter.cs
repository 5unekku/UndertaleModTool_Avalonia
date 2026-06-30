using System;
using System.Globalization;
using Avalonia.Data.Converters;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public class GameObjectByIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint val = System.Convert.ToUInt32(value);
            UndertaleGameObject returnObj = null;
            if (val < MainWindow.Instance.Data.GameObjects.Count)
            {
                returnObj = MainWindow.Instance.Data.GameObjects[(int)val];
                return returnObj;
            }
            else
            {
                return returnObj;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (uint)MainWindow.Instance.Data.GameObjects.IndexOf((UndertaleGameObject)value);
        }
    }
}
