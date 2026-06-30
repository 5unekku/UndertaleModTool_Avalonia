using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleExtensionOption;

namespace UndertaleModTool
{
    public partial class UndertaleExtensionEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;
        private byte[] lastProductId;

        public int MyIndex
        {
            get
            {
                if (DataContext is not UndertaleExtension ext)
                    return -1;
                return mainWindow.Data.Extensions.IndexOf(ext);
            }
        }

        // bindable (two-way) direct property; avalonia controls can't add a plain INotifyPropertyChanged event
        public static readonly DirectProperty<UndertaleExtensionEditor, byte[]> ProductIdDataProperty =
            AvaloniaProperty.RegisterDirect<UndertaleExtensionEditor, byte[]>(nameof(ProductIdData), o => o.ProductIdData, (o, v) => o.ProductIdData = v);

        public byte[] ProductIdData
        {
            get
            {
                if (mainWindow.Data?.GeneralInfo is UndertaleGeneralInfo generalInfo &&
                    (generalInfo.Major >= 2 ||
                    (generalInfo.Major == 1 && (generalInfo.Build >= 1773 || generalInfo.Build == 1539))))
                {
                    return mainWindow.Data.FORM.EXTN.productIdData[MyIndex];
                }
                return null;
            }
            set
            {
                mainWindow.Data.FORM.EXTN.productIdData[MyIndex] = value;
            }
        }

        public UndertaleExtensionEditor()
        {
            InitializeComponent();
            DataContextChanged += (_, _) =>
            {
                byte[] old = lastProductId;
                lastProductId = ProductIdData;
                RaisePropertyChanged(ProductIdDataProperty, old, lastProductId);
            };
        }

        private void NewFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UndertaleExtension extension)
                return;

            int lastItem = extension.Files.Count;
            UndertaleExtensionFile obj = new()
            {
                Kind = UndertaleExtensionKind.Dll,
                Filename = mainWindow.Data.Strings.MakeString($"NewExtensionFile{lastItem}.dll"),
                Functions = new UndertalePointerList<UndertaleExtensionFunction>()
            };
            extension.Files.Add(obj);
        }

        private void NewOptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UndertaleExtension extension)
                return;

            int lastItem = extension.Options.Count;
            UndertaleExtensionOption obj = new()
            {
                Name = mainWindow.Data.Strings.MakeString($"extensionOption{lastItem}"),
                Value = mainWindow.Data.Strings.MakeString("", true)
            };
            extension.Options.Add(obj);
        }

        private void KindComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var option = comboBox?.DataContext as UndertaleExtensionOption;
            if (option?.Value is null)
                return;

            switch (comboBox.SelectedItem)
            {
                case OptionKind.String:
                case null:
                    break;

                case OptionKind.Boolean:
                    if (option.Value.Content.ToLowerInvariant() == "true")
                        option.Value.Content = "True";
                    else
                        option.Value.Content = "False";
                    break;

                case OptionKind.Number:
                    if (!Double.TryParse(option.Value.Content, NumberStyles.Any, CultureInfo.InvariantCulture, out double _))
                        option.Value.Content = "0";
                    break;
            }
        }
    }

    /// <summary>selects the value editor template based on an option's <c>OptionKind</c> (was a wpf DataTemplateSelector).</summary>
    public class OptionValueTemplateSelector : IDataTemplate
    {
        public IDataTemplate StringTemplate { get; set; }
        public IDataTemplate BooleanTemplate { get; set; }
        public IDataTemplate NumberTemplate { get; set; }

        public bool Match(object data) => data is OptionKind;

        public Control Build(object data)
        {
            IDataTemplate template = (OptionKind)data switch
            {
                OptionKind.String => StringTemplate,
                OptionKind.Boolean => BooleanTemplate,
                OptionKind.Number => NumberTemplate,
                _ => StringTemplate
            };
            return template?.Build(data);
        }
    }

    public class OptionValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string str)
                return null;

            switch (parameter)
            {
                case "boolean":
                    return str.ToLowerInvariant() == "true";
                case "number":
                    if (Double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double _))
                        return str;
                    return "0";
                default:
                    return str;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string par)
                return BindingOperations.DoNothing;

            switch (par)
            {
                case "boolean":
                    if (value is not bool b)
                        return BindingOperations.DoNothing;
                    return b ? "True" : "False";
                case "number":
                    if (value is string s && Double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double _))
                        return s;
                    return BindingOperations.DoNothing;
                default:
                    return BindingOperations.DoNothing;
            }
        }
    }
}
