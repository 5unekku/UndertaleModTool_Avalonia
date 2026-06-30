using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Data.Converters;

namespace UndertaleModTool
{
    /// <summary>
    /// yields an <c>IsVisible</c> bool: true when the loaded data is at least the version named in the
    /// parameter, false otherwise (including the invalid/unknown cases the wpf converter mapped to hidden).
    /// </summary>
    public class IsVersionAtLeastConverter : IValueConverter
    {
        private static readonly Regex versionRegex = new(@"(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?", RegexOptions.Compiled);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            MainWindow mainWindow = MainWindow.Instance;
            if (mainWindow is null
                || mainWindow.Data?.GeneralInfo is null
                || parameter is not string verStr
                || verStr.Length == 0)
                return false;

            var ver = versionRegex.Match(verStr);
            if (!ver.Success)
                return false;
            try
            {
                uint major = uint.Parse(ver.Groups[1].Value);
                uint minor = uint.Parse(ver.Groups[2].Value);
                uint release = 0;
                uint build = 0;
                if (ver.Groups[3].Value != "")
                    release = uint.Parse(ver.Groups[3].Value);
                if (ver.Groups[4].Value != "")
                    build = uint.Parse(ver.Groups[4].Value);

                return mainWindow.Data.IsVersionAtLeast(major, minor, release, build);
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
