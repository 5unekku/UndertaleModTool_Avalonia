using Avalonia.Controls;

namespace UndertaleModTool
{
    /// <summary>
    /// a standard button. in wpf this hand-rolled dark-mode foreground handling; under avalonia the fluent theme
    /// themes light/dark natively, so this is just a thin subclass kept for 1:1 xaml usage.
    /// </summary>
    public class ButtonDark : Button
    {
    }
}
