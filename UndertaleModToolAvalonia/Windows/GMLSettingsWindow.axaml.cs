using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;

namespace UndertaleModTool
{
    public partial class GMLSettingsWindow : Window
    {
        public GMLSettingsWindow(Settings settings)
        {
            InitializeComponent();
            DataContext = settings;
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.DecompilerSettings.RestoreDefaults();
            Settings.Instance.InstanceIdPrefix = Settings.DefaultInstanceIdPrefix;

            // force all bindings to re-read the (now reset) values
            DataContext = null;
            DataContext = Settings.Instance;
        }
    }

    public class IndentStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DecompilerSettings.IndentStyleKind kind)
                return null;
            return kind switch
            {
                DecompilerSettings.IndentStyleKind.FourSpaces => "4 spaces",
                DecompilerSettings.IndentStyleKind.TwoSpaces => "2 spaces",
                DecompilerSettings.IndentStyleKind.Tabs => "Tabs",
                _ => throw new Exception("Unknown indent style kind")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
