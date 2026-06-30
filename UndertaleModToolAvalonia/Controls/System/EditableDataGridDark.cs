using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;

namespace UndertaleModTool
{
    /// <summary>arguments for <see cref="EditableDataGridDark.AddingNewItem"/>, mirroring wpf's event.</summary>
    public class AddingNewItemEventArgs : EventArgs
    {
        /// <summary>the item to add; leave null to let the grid create one from the list's element type.</summary>
        public object NewItem { get; set; }
    }

    /// <summary>
    /// a data grid that restores the add/remove "usage" wpf got from its add-row placeholder (which avalonia's
    /// DataGrid lacks). pressing Delete removes the selected row; <see cref="AddNew"/> (wired to an Add button by
    /// the editor) appends a new item, raising <see cref="AddingNewItem"/> so editors can customize it.
    /// </summary>
    public class EditableDataGridDark : DataGridDark
    {
        /// <summary>raised before a new item is added; set <see cref="AddingNewItemEventArgs.NewItem"/> to supply it.</summary>
        public event EventHandler<AddingNewItemEventArgs> AddingNewItem;

        /// <summary>explicit element type for new items, when it cannot be inferred from the bound list.</summary>
        public Type NewItemType { get; set; }

        public EditableDataGridDark()
        {
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && !IsReadOnly && SelectedItem is not null && ItemsSource is IList list)
            {
                list.Remove(SelectedItem);
                e.Handled = true;
            }
        }

        /// <summary>creates a new item (via <see cref="AddingNewItem"/> or the list element type) and appends it.</summary>
        public object AddNew()
        {
            if (ItemsSource is not IList list)
                return null;

            var args = new AddingNewItemEventArgs();
            AddingNewItem?.Invoke(this, args);

            object item = args.NewItem;
            if (item is null)
            {
                Type type = NewItemType ?? ElementTypeOf(list);
                if (type is null)
                    return null;
                item = Activator.CreateInstance(type);
            }

            list.Add(item);
            SelectedItem = item;
            return item;
        }

        private static Type ElementTypeOf(IList list)
        {
            Type listType = list.GetType();
            Type generic = listType.GetInterfaces()
                                   .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
            if (generic is not null)
                return generic.GetGenericArguments()[0];

            // fall back to the type of an existing element
            foreach (object existing in list)
                if (existing is not null)
                    return existing.GetType();

            return null;
        }
    }
}
