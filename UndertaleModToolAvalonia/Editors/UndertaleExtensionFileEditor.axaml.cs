using System.Collections.Generic;
using Avalonia.Interactivity;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertaleExtensionFileEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        public UndertaleExtensionFileEditor()
        {
            InitializeComponent();
            FunctionsList.AddingNewItem += DataGrid_AddingNewItem;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            FunctionsList.AddNew();
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            var itemList = FunctionsList.ItemsSource as IList<UndertaleExtensionFunction>;
            int lastItem = itemList.Count;

            UndertaleExtensionFunction obj = new()
            {
                Name = mainWindow.Data.Strings.MakeString($"new_extension_function_{lastItem}"),
                ExtName = mainWindow.Data.Strings.MakeString($"new_extension_function_{lastItem}_ext"),
                RetType = UndertaleExtensionVarType.Double,
                Arguments = new UndertaleSimpleList<UndertaleExtensionFunctionArg>(),
                Kind = 11, // ???
                ID = mainWindow.Data.ExtensionFindLastId()
            };

            e.NewItem = obj;
        }
    }
}
