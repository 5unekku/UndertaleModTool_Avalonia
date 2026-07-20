using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Project;

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

            if (ctrl && !shift && e.Key == Key.N) { New_Click(this, new RoutedEventArgs()); e.Handled = true; }
            else if (ctrl && !shift && e.Key == Key.O) { Open_Click(this, new RoutedEventArgs()); e.Handled = true; }
            else if (ctrl && !shift && e.Key == Key.S) { Save_Click(this, new RoutedEventArgs()); e.Handled = true; }
            else if (ctrl && !shift && e.Key == Key.Q) { Close(); e.Handled = true; }
            else if (ctrl && !shift && e.Key == Key.W) { if (CurrentTab is not null) CloseTab(CurrentTab); e.Handled = true; }
            else if (ctrl && shift && e.Key == Key.W) { CloseAllTabs(); e.Handled = true; }
            else if (ctrl && shift && e.Key == Key.T) { RestoreClosedTab(); e.Handled = true; }
            else if (ctrl && !shift && e.Key == Key.Tab) { SwitchTab(1); e.Handled = true; }
            else if (ctrl && shift && e.Key == Key.Tab) { SwitchTab(-1); e.Handled = true; }
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

            if (target is TreeCategory cat)
            {
                if (cat.Source is null)
                    return;
                AddMenuItem(menu, "Add", () => AddResource(cat.Source));
            }
            else if (target is TreeLeaf leaf)
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

        // whether a string is a valid GML asset identifier (letters/digits/underscore, not starting with a digit)
        private static bool IsValidAssetIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            char firstChar = name[0];
            if (!char.IsAsciiLetter(firstChar) && firstChar != '_')
                return false;
            foreach (char c in name.Skip(1))
            {
                if (!char.IsAsciiLetterOrDigit(c) && c != '_')
                    return false;
            }
            return true;
        }

        /// <summary>
        /// creates a new resource of the list's element type, names it (with per-type initialization), adds it,
        /// and opens it in a new tab. 1:1 port of the wpf MenuItem_Add_Click.
        /// </summary>
        internal void AddResource(IList list)
        {
            if (Data is null || list is null)
                return;

            Type t;
            try
            {
                t = list.GetType().GetGenericArguments()[0];
            }
            catch (Exception ex)
            {
                ScriptError("An error occurred while trying to add the menu item. No action has been taken.\r\n\r\nError:\r\n\r\n" + ex);
                return;
            }
            if (!typeof(UndertaleResource).IsAssignableFrom(t))
                return;

            UndertaleResource obj = Activator.CreateInstance(t) as UndertaleResource;
            if (obj is UndertaleNamedResource namedResource)
            {
                bool doMakeString = obj is not (UndertaleTexturePageItem or UndertaleEmbeddedAudio or UndertaleEmbeddedTexture);
                string notDataNewName = null;
                if (obj is UndertaleTexturePageItem)
                    notDataNewName = "PageItem " + list.Count;
                if (obj is UndertaleEmbeddedAudio)
                    notDataNewName = "EmbeddedSound " + list.Count;
                if (obj is UndertaleEmbeddedTexture)
                    notDataNewName = "Texture " + list.Count;

                if (doMakeString)
                {
                    string assetTypeName = obj.GetType().Name.Replace("Undertale", "").Replace("GameObject", "Object").ToLower();
                    string newName = $"{assetTypeName}{list.Count}";
                    string userNewName = ScriptInputDialog($"Choose new {assetTypeName} name", "Name of new asset:", newName, "Cancel", "Create", false, false);
                    if (userNewName is null)
                        return; // user canceled
                    if (IsValidAssetIdentifier(userNewName))
                    {
                        newName = userNewName;
                    }
                    else if (this.ShowQuestionWithCancel($"Asset name \"{userNewName}\" is not a valid identifier. Add a new asset using an auto-generated name instead?",
                                 MessageBoxImage.Warning, "Invalid name") != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    namedResource.Name = Data.Strings.MakeString(newName);

                    if (obj is UndertaleRoom roomResource)
                    {
                        if (Data.IsGameMaker2())
                        {
                            roomResource.Caption = null;
                            roomResource.Backgrounds.Clear();
                            if (Data.IsVersionAtLeast(2024, 13))
                            {
                                roomResource.Flags |= UndertaleRoom.RoomEntryFlags.IsGM2024_13;
                                roomResource.InstanceCreationOrderIDs ??= new();
                            }
                            else
                            {
                                roomResource.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2;
                                if (Data.IsVersionAtLeast(2, 3))
                                    roomResource.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2_3;
                            }
                        }
                        else
                        {
                            roomResource.Caption = Data.Strings.MakeString("");
                        }

                        if (this.ShowQuestion("Add the new room to the end of the room order list?", MessageBoxImage.Question, "Add to room order list") == MessageBoxResult.Yes)
                            Data.GeneralInfo.RoomOrder.Add(new(roomResource));
                    }
                    else if (obj is UndertaleScript scriptResource)
                    {
                        if (Data.IsVersionAtLeast(2, 3))
                        {
                            scriptResource.Code = UndertaleCode.CreateEmptyEntry(Data, $"gml_GlobalScript_{newName}");
                            if (Data.GlobalInitScripts is IList<UndertaleGlobalInit> globalInitScripts)
                                globalInitScripts.Add(new UndertaleGlobalInit() { Code = scriptResource.Code });
                        }
                        else
                        {
                            scriptResource.Code = UndertaleCode.CreateEmptyEntry(Data, $"gml_Script_{newName}");
                        }
                        Project?.MarkAssetForExport(scriptResource.Code);
                    }
                    else if (obj is UndertaleCode codeResource)
                    {
                        if (Data.CodeLocals is not null)
                        {
                            codeResource.LocalsCount = 1;
                            UndertaleCodeLocals.CreateEmptyEntry(Data, codeResource.Name);
                        }
                        else
                        {
                            codeResource.WeirdLocalFlag = true;
                        }
                    }
                    else if (obj is UndertaleExtension && IsExtProductIDEligible)
                    {
                        var newProductID = new byte[] { 0xBA, 0x5E, 0xBA, 0x11, 0xBA, 0xDD, 0x06, 0x60, 0xBE, 0xEF, 0xED, 0xBA, 0x0B, 0xAB, 0xBA, 0xBE };
                        Data.FORM.EXTN.productIdData.Add(newProductID);
                    }
                    else if (obj is UndertaleShader shader)
                    {
                        shader.GLSL_ES_Vertex = Data.Strings.MakeString("", true);
                        shader.GLSL_ES_Fragment = Data.Strings.MakeString("", true);
                        shader.GLSL_Vertex = Data.Strings.MakeString("", true);
                        shader.GLSL_Fragment = Data.Strings.MakeString("", true);
                        shader.HLSL9_Vertex = Data.Strings.MakeString("", true);
                        shader.HLSL9_Fragment = Data.Strings.MakeString("", true);
                    }
                }
                else
                {
                    namedResource.Name = new UndertaleString(notDataNewName); // not Data.MakeString!
                }
            }
            else if (obj is UndertaleString str)
            {
                str.Content = "string" + list.Count;
            }

            list.Add(obj);

            if (Project is not null && obj is IProjectAsset projectAsset)
                Project.MarkAssetForExport(projectAsset);

            BuildTree(SearchBox?.Text);
            Highlighted = obj;
            OpenInTab(obj, true);
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
