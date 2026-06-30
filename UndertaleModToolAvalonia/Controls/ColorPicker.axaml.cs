using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace UndertaleModTool
{
    public partial class ColorPicker : UserControl
    {
        public static readonly StyledProperty<uint> ColorProperty =
            AvaloniaProperty.Register<ColorPicker, uint>(nameof(Color), 0xFFFFFFFF, defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> HasAlphaProperty =
            AvaloniaProperty.Register<ColorPicker, bool>(nameof(HasAlpha), true, defaultBindingMode: BindingMode.TwoWay);

        public uint Color
        {
            get => GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public bool HasAlpha
        {
            get => GetValue(HasAlphaProperty);
            set => SetValue(HasAlphaProperty, value);
        }

        public ColorPicker()
        {
            InitializeComponent();
            ApplyTextBinding(HasAlpha);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == HasAlphaProperty)
                ApplyTextBinding(HasAlpha);
        }

        private void ApplyTextBinding(bool hasAlpha)
        {
            if (ColorText is null)
                return;

            var binding = new Binding(nameof(Color))
            {
                Converter = new ColorTextConverter(),
                ConverterParameter = hasAlpha.ToString(),
                RelativeSource = new RelativeSource { AncestorType = typeof(ColorPicker) },
                Mode = BindingMode.TwoWay
            };
            ColorText.Bind(TextBox.TextProperty, binding);

            ColorText.MaxLength = hasAlpha ? 9 : 7;
            ToolTip.SetTip(ColorText, $"#{(hasAlpha ? "AA" : "")}BBGGRR");
        }
    }

    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint val = System.Convert.ToUInt32(value);
            return Color.FromArgb((byte)((val >> 24) & 0xff), (byte)(val & 0xff), (byte)((val >> 8) & 0xff), (byte)((val >> 16) & 0xff));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color val = (Color)value;
            return (uint)((val.A << 24) | (val.B << 16) | (val.G << 8) | val.R);
        }
    }

    public class ColorTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                uint val = System.Convert.ToUInt32(value);
                bool hasAlpha = bool.Parse((string)parameter);
                return "#" + (hasAlpha ? val.ToString("X8") : val.ToString("X8")[2..]);
            }
            catch
            {
                return BindingOperations.DoNothing;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string val = (string)value;
                bool hasAlpha = bool.Parse((string)parameter);

                if (val[0] != '#')
                    return BindingOperations.DoNothing;

                val = val[1..];
                if (val.Length != (hasAlpha ? 8 : 6))
                    return BindingOperations.DoNothing;

                if (!hasAlpha)
                    val = "FF" + val; // add alpha (255)

                return System.Convert.ToUInt32(val, 16);
            }
            catch
            {
                return BindingOperations.DoNothing;
            }
        }
    }
}
