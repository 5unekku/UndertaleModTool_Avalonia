using System;
using Avalonia.Markup.Xaml;

namespace UndertaleModTool
{
    /// <summary>
    /// markup extension that yields all values of an enum type, replacing the wpf
    /// <c>ObjectDataProvider</c>/<c>Enum.GetValues</c> pattern used to fill enum combo boxes.
    /// usage: <c>ItemsSource="{local:EnumValues undertale:SomeEnum}"</c>.
    /// </summary>
    public class EnumValuesExtension : MarkupExtension
    {
        public Type Type { get; set; }

        public EnumValuesExtension()
        {
        }

        public EnumValuesExtension(Type type)
        {
            Type = type;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Type is null || !Type.IsEnum)
                return Array.Empty<object>();
            return Enum.GetValues(Type);
        }
    }
}
