using Avalonia.Controls;
using Avalonia.Interactivity;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertaleGlobalInitEditor : DataUserControl
    {
        public UndertaleGlobalInitEditor()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            ScriptsGrid.AddNew();
        }

        private void UndertaleObjectReference_Loaded(object sender, RoutedEventArgs e)
        {
            var objRef = sender as UndertaleObjectReference;

            objRef.ClearRemoveClickHandler();
            objRef.RemoveButton.Click += Remove_Click_Override;
            ToolTip.SetTip(objRef.RemoveButton, "Remove script");
            objRef.RemoveButton.IsEnabled = true;
        }

        private void Remove_Click_Override(object sender, RoutedEventArgs e)
        {
            var btn = (ButtonDark)sender;

            var data = (GlobalInitEditor)DataContext;
            var globalInits = data.GlobalInits;
            if (btn.DataContext is not UndertaleGlobalInit)
                return;

            globalInits.Remove((UndertaleGlobalInit)btn.DataContext);
        }
    }
}
