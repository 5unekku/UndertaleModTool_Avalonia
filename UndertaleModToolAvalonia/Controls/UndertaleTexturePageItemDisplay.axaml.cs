using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace UndertaleModTool
{
    public partial class UndertaleTexturePageItemDisplay : UserControl
    {
        public static readonly StyledProperty<bool> DisplayBorderProperty =
            AvaloniaProperty.Register<UndertaleTexturePageItemDisplay, bool>(nameof(DisplayBorder), true, defaultBindingMode: BindingMode.TwoWay);

        public bool DisplayBorder
        {
            get => GetValue(DisplayBorderProperty);
            set => SetValue(DisplayBorderProperty, value);
        }

        public UndertaleTexturePageItemDisplay()
        {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DisplayBorderProperty && RenderAreaBorder is not null)
                RenderAreaBorder.BorderThickness = new Thickness(DisplayBorder ? 1 : 0);
        }
    }
}
