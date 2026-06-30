using Avalonia.Controls;

namespace UndertaleModTool
{
    /// <summary>
    /// a standard text box; fluent handles dark mode and supplies a native cut/copy/paste context menu, so this
    /// is a thin subclass for 1:1 xaml usage (the wpf version wired up a custom dark context menu).
    /// </summary>
    public class TextBoxDark : TextBox
    {
    }
}
