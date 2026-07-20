using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    // dynamic Scripts menu: scans the Scripts folder (and subfolders) for .csx files on submenu open.
    // 1:1 port of the wpf MenuItem_RunScript_SubmenuOpened, minus the wpf popup dark-mode background hack
    // (the fluent theme styles menus itself).
    public partial class MainWindow
    {
        private void ScriptsMenu_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            // avalonia bubbles SubmenuOpened; stop it so opening a child submenu doesn't re-run parent handlers
            e.Handled = true;
            PopulateScriptMenu(sender as MenuItem, Path.Join(ExePath, "Scripts"), isRoot: true);
        }

        private void PopulateScriptMenu(MenuItem item, string folderDir, bool isRoot)
        {
            if (item is null)
                return;

            var directory = new DirectoryInfo(folderDir);
            item.Items.Clear();
            try
            {
                if (!directory.Exists)
                {
                    item.Items.Add(new MenuItem { Header = $"(Path {folderDir} does not exist, cannot search for files!)", IsEnabled = false });
                    if (isRoot)
                        AddRunOtherScriptItem(item);
                    return;
                }

                foreach (FileInfo file in directory.EnumerateFiles("*.csx"))
                {
                    // double the underscores: avalonia (like wpf) treats a single _ as an access key
                    var subitem = new MenuItem { Header = file.Name.Replace("_", "__"), CommandParameter = file.FullName };
                    subitem.Click += RunBuiltinScript_Click;
                    item.Items.Add(subitem);
                }

                foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories())
                {
                    if (!subDirectory.EnumerateFiles("*.csx", SearchOption.AllDirectories).Any())
                        continue;

                    // need at least one child item so the submenu arrow shows; it's replaced on open
                    var subItem = new MenuItem
                    {
                        Header = subDirectory.Name.Replace("_", "__"),
                        Items = { new MenuItem { Header = "(loading...)", IsEnabled = false } }
                    };
                    string subDirFullName = subDirectory.FullName;
                    subItem.SubmenuOpened += (o, args) => { args.Handled = true; PopulateScriptMenu(o as MenuItem, subDirFullName, isRoot: false); };
                    item.Items.Add(subItem);
                }

                if (item.Items.Count == 0)
                    item.Items.Add(new MenuItem { Header = "(No scripts found!)", IsEnabled = false });
            }
            catch (Exception err)
            {
                item.Items.Add(new MenuItem { Header = err.ToString(), IsEnabled = false });
            }

            if (isRoot)
                AddRunOtherScriptItem(item);
        }

        private void AddRunOtherScriptItem(MenuItem item)
        {
            var otherScripts = new MenuItem { Header = "Run _other script..." };
            otherScripts.Click += RunOtherScript_Click;
            item.Items.Add(otherScripts);
        }

        private async void RunBuiltinScript_Click(object sender, RoutedEventArgs e)
        {
            string path = (sender as MenuItem)?.CommandParameter as string;
            if (path is not null && !File.Exists(path))
                path = Paths.TryJoinVerifyWithinDirectory(Program.GetExecutableDirectory(), path);

            if (path is not null && File.Exists(path))
                await RunScript(path);
            else
                this.ShowError("The script file doesn't exist.");
        }

        private async void RunOtherScript_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { DefaultExt = "csx", Filter = "Scripts (.csx)|*.csx|All files|*" };
            if (dlg.ShowDialog(this) == true)
                await RunScript(dlg.FileName);
        }
    }
}
