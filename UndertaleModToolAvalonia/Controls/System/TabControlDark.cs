using Avalonia.Controls;

namespace UndertaleModTool
{
    /// <summary>
    /// a standard tab control; fluent handles dark mode, so this is a thin subclass for 1:1 xaml usage. the wpf
    /// version generated <see cref="TabItemDark"/> containers and recolored them by hand for dark mode, which
    /// fluent does natively.
    /// </summary>
    public class TabControlDark : TabControl
    {
    }

    /// <summary>a standard tab item; fluent handles dark mode, so this is a thin subclass for 1:1 xaml usage.</summary>
    public class TabItemDark : TabItem
    {
    }
}
