using System.Collections.Generic;
using Avalonia.Interactivity;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertaleCodeLocalsEditor : DataUserControl
    {
        public UndertaleCodeLocalsEditor()
        {
            InitializeComponent();
            LocalsGrid.AddingNewItem += DataGrid_AddingNewItem;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            LocalsGrid.AddNew();
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            UndertaleCodeLocals.LocalVar obj = new();
            obj.Index = (uint)(LocalsGrid.ItemsSource as IList<UndertaleCodeLocals.LocalVar>).Count;
            e.NewItem = obj;
        }
    }
}
