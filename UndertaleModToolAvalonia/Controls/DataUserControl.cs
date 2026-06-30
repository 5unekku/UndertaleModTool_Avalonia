using Avalonia;
using Avalonia.Controls;

namespace UndertaleModTool
{
    /// <summary>
    /// base class for the resource editors. it ignores a transient null <c>DataContext</c> (which happens while
    /// switching between tabs to an incompatible data type) so bindings are not torn down and re-evaluated needlessly.
    /// </summary>
    public class DataUserControl : UserControl
    {
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            // skip the transient null DataContext that occurs when switching to an incompatible data type
            if (change.Property == DataContextProperty && change.NewValue is null)
                return;

            base.OnPropertyChanged(change);
        }
    }
}
