using Avalonia.Controls;

namespace UndertaleModTool
{
    /// <summary>
    /// a standard data grid; fluent handles dark mode, so this is a thin subclass for 1:1 xaml usage. the wpf
    /// version also worked around wpf-specific datagrid quirks (unset selection values, new-row commit); those
    /// quirks do not apply to avalonia's datagrid, so the workarounds are dropped.
    /// </summary>
    public class DataGridDark : DataGrid
    {
    }
}
