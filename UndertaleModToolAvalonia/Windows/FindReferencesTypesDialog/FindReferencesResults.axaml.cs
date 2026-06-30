using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Data.Converters;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModTool.Windows
{
    // results window for "find all references". the wpf version used XamlReader templates + a live FilteredViewConverter
    // + per-editor tab content state; this port builds the tree in code, filters by rebuilding, and navigates via
    // ChangeSelection (instance-in-room highlighting is not restored).
    public partial class FindReferencesResults : Window
    {
        private static MainWindow mainWindow => MainWindow.Instance;
        private object highlighted;
        private string sourceObjName;
        private readonly UndertaleData data;
        private Dictionary<string, List<object>> allResults;

        public FindReferencesResults(object sourceObj, UndertaleData data, Dictionary<string, List<object>> results)
        {
            InitializeComponent();
            this.data = data;

            if (sourceObj is UndertaleNamedResource namedObj)
                sourceObjName = namedObj.Name.Content;
            else if (sourceObj is UndertaleString str)
                sourceObjName = str.Content;
            else if (sourceObj is ValueTuple<UndertaleBackground, UndertaleBackground.TileID> tileTuple)
                sourceObjName = $"Tile {tileTuple.Item2.ID} of {tileTuple.Item1.Name.Content}";
            else
                sourceObjName = sourceObj.GetType().Name;

            Title = $"The references of game asset \"{sourceObjName}\"";
            label.Text = $"The search results for the game asset\n\"{sourceObjName}\".";

            ProcessResults(results);
        }

        public FindReferencesResults(UndertaleData data, Dictionary<string, List<object>> results)
        {
            InitializeComponent();
            this.data = data;

            Title = "The unreferenced game assets";
            label.Text = "The search results for the unreferenced game assets.";

            ProcessResults(results);
        }

        private void ProcessResults(Dictionary<string, List<object>> results)
        {
            allResults = results;
            if (results is null || results.Count == 0)
            {
                ResultsTree.Items.Add(new TextBlock { Text = "No references found.", FontSize = 16, Margin = new(8) });
                return;
            }
            BuildTree("");
        }

        private void BuildTree(string filter)
        {
            ResultsTree.Items.Clear();
            if (allResults is null)
                return;

            bool noFilter = string.IsNullOrEmpty(filter);
            foreach (var result in allResults)
            {
                // a standalone editor entry (general info / global init / game end) is a leaf
                if (result.Value.Count > 0 && result.Value[0] is GeneralInfoEditor or GlobalInitEditor or GameEndEditor)
                {
                    if (noFilter || result.Key.Contains(filter, StringComparison.OrdinalIgnoreCase))
                        ResultsTree.Items.Add(new TreeViewItem { Header = result.Key, Tag = result.Value[0] });
                    continue;
                }

                var group = new TreeViewItem { Header = result.Key, IsExpanded = true };
                foreach (object obj in result.Value)
                {
                    string name = DisplayName(obj);
                    if (!noFilter && (name is null || !name.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    group.Items.Add(new TreeViewItem { Header = name, Tag = obj });
                }

                if (group.Items.Count > 0)
                    ResultsTree.Items.Add(group);
            }
        }

        private static string DisplayName(object obj) => obj switch
        {
            object[] inst => ChildInstanceNameConverter.Instance.Convert(inst, null, null, null) as string,
            UndertaleNamedResource named => named.Name?.Content ?? named.ToString(),
            UndertaleString str => '"' + (str.Content?.Length > 64 ? str.Content[..64] + "..." : str.Content) + '"',
            null => "(null)",
            _ => obj.ToString()
        };

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => BuildTree(SearchBox.Text ?? "");

        private void ExportButton_Click(object sender, RoutedEventArgs e) => ExportResults();
        private void DoneButton_Click(object sender, RoutedEventArgs e) => Close();

        private void ResultsTree_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            highlighted = (ResultsTree.SelectedItem as TreeViewItem)?.Tag;
        }

        private void ResultsTree_DoubleTapped(object sender, TappedEventArgs e) => Open(highlighted);

        private void ResultsTree_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Open(highlighted);
        }

        private void Open(object obj, bool inNewTab = false)
        {
            if (obj is null)
                return;
            if (data.FORM is null)
            {
                this.ShowError("The object reference is stale - a different game data was loaded.");
                return;
            }

            object target = obj is object[] inst ? inst[^1] : obj;
            mainWindow.Activate();
            mainWindow.ChangeSelection(target, inNewTab);
        }

        private void ExportResults()
        {
            if (data.FORM is null)
            {
                this.ShowError("The object references are stale - a different game data was loaded.");
                return;
            }
            if (allResults is null || allResults.Count == 0)
            {
                this.ShowError("No results to export.");
                return;
            }

            string initContent = Title + ":\n";
            initContent += new string('-', initContent.Length - 1) + "\n\n";
            StringBuilder sb = new(initContent);

            foreach (var group in allResults)
            {
                sb.AppendLine(group.Key + ':');
                foreach (object obj in group.Value)
                    sb.AppendLine("    " + DisplayName(obj));
                sb.Append('\n');
            }

            string name = sourceObjName;
            if (name is not null)
            {
                string invalidCharsRegex = '[' + string.Join("", Path.GetInvalidFileNameChars()) + ']';
                name = Regex.Replace(name, invalidCharsRegex, "_");
            }

            string folderPath = Path.GetDirectoryName(mainWindow.FilePath);
            string filePath = Paths.TryJoinVerifyWithinDirectory(folderPath, name is null ? "unreferenced_assets.txt" : $"references_of_asset_{name}.txt");
            if (filePath is null)
            {
                this.ShowError("Failed to choose good output file name; directory escaped.");
                return;
            }
            if (File.Exists(filePath) && this.ShowQuestion($"File \"{filePath}\" exists.\nOverwrite?") == MessageBoxResult.No)
                return;

            File.WriteAllText(filePath, sb.ToString());
            this.ShowMessage($"The results were successfully saved at path\n\"{filePath}\".");
        }
    }

    public class ChildInstanceNameConverter : IValueConverter
    {
        public static ChildInstanceNameConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is object[] inst)
            {
                StringBuilder sb = new();
                for (int i = 0; i < inst.Length; i++)
                {
                    var link = inst[i];
                    if (link is UndertaleNamedResource namedObj)
                        sb.Append(namedObj.Name);
                    else
                        sb.Append(link.ToString());
                    if (i != inst.Length - 1)
                        sb.Append(" — ");
                }
                return sb.ToString();
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
