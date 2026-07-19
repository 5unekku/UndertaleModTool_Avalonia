using System;
using System.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    // resource-tree operations and keyboard shortcuts (a lean subset of the wpf MainWindow input bindings +
    // tree context menu). "Add" is intentionally not ported yet: creating a resource needs faithful per-type
    // initialization (room GMS2 flags/room-order, script code entries, etc.) or it produces broken assets.
    public partial class MainWindow
    {
        // keyboard shortcuts: Ctrl+O open, Ctrl+S save, Ctrl+W close tab, Ctrl+Shift+F search in code
        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

            if (ctrl && !shift && e.Key == Key.O) { Open_Click(this, new RoutedEventArgs()); e.Handled = true; }
            else if (ctrl && !shift && e.Key == Key.S) { Save_Click(this, new RoutedEventArgs()); e.Handled = true; }
            else if (ctrl && !shift && e.Key == Key.W) { if (CurrentTab is not null) CloseTab(CurrentTab); e.Handled = true; }
            else if (ctrl && shift && e.Key == Key.F) { SearchInCode_Click(this, new RoutedEventArgs()); e.Handled = true; }

            if (!e.Handled)
                base.OnKeyDown(e);
        }

        // delete key on the tree removes the selected resource (same guarded confirm as the context menu)
        private void ResourceTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && (sender as TreeView)?.SelectedItem is UndertaleResource res)
            {
                DeleteItem(res);
                e.Handled = true;
            }
        }

        // right-click on a tree node: build a context menu appropriate to what was clicked
        private void ResourceTree_ContextRequested(object sender, ContextRequestedEventArgs e)
        {
            object target = (e.Source as Control)?.DataContext;
            var menu = new ContextMenu();

            if (target is TreeLeaf leaf)
            {
                AddMenuItem(menu, "Open in new tab", () => ChangeSelection(leaf.Target, true));
            }
            else if (target is UndertaleResource res)
            {
                AddMenuItem(menu, "Open in new tab", () => ChangeSelection(res, true));
                AddMenuItem(menu, "Copy name", () => CopyItemName(res));
                AddMenuItem(menu, "Delete", () => DeleteItem(res));
            }
            else
            {
                // categories (Add not ported yet) and empty space: no menu
                return;
            }

            menu.Open(ResourceTree);
            e.Handled = true;
        }

        private static void AddMenuItem(ContextMenu menu, string header, Action action)
        {
            var item = new MenuItem { Header = header };
            item.Click += (_, _) => action();
            menu.Items.Add(item);
        }

        /// <summary>deletes a resource from its data list, closing any tabs that show it (1:1 with the wpf delete).</summary>
        internal void DeleteItem(object obj)
        {
            if (Data is null || obj is null)
                return;
            IList list = Data[obj.GetType()];
            if (list is null)
                return;
            int index = list.IndexOf(obj);
            if (index < 0)
                return;

            bool isLast = index == list.Count - 1;
            string message = "Delete " + obj + "?"
                + (!isLast ? "\n\nNote that the code often references objects by ID, so this operation is likely to break stuff because other items will shift up!" : "");
            if (this.ShowQuestion(message, isLast ? MessageBoxImage.Question : MessageBoxImage.Warning, "Confirmation") != MessageBoxResult.Yes)
                return;

            list.Remove(obj);

            // close any tabs currently showing the deleted object
            for (int i = Tabs.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(Tabs[i].CurrentObject, obj))
                    CloseTab(Tabs[i]);
            }
            // strip the deleted object from the remaining tabs' back/forward history
            foreach (Tab tab in Tabs)
            {
                for (int i = tab.History.Count - 1; i >= 0; i--)
                {
                    if (!ReferenceEquals(tab.History[i], obj))
                        continue;
                    if (i <= tab.HistoryPosition && tab.HistoryPosition > 0)
                        tab.HistoryPosition--;
                    tab.History.RemoveAt(i);
                }
            }

            BuildTree(SearchBox?.Text);
        }

        /// <summary>copies a resource's name to the clipboard (1:1 with the wpf copy-name action).</summary>
        internal void CopyItemName(object obj)
        {
            string name = null;
            if (obj is UndertaleNamedResource named)
                name = named.Name?.Content;
            else if (obj is UndertaleString str && str.Content?.Length > 0)
                name = StringTitleConverter.Instance.Convert(str.Content, null, null, null) as string;

            if (name is null)
            {
                this.ShowWarning("Item name is null.");
                return;
            }

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is not null)
                _ = clipboard.SetTextAsync(name);
        }
    }
}
