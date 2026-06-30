using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModTool.Windows
{
    public partial class SearchInCodeWindow : Window
    {
        private static MainWindow mainWindow => MainWindow.Instance;
        private static bool isSearchInProgress = false;

        private bool isCaseSensitive, isRegexSearch, isMultilineRegex, isInAssembly;
        private string text;

        private int progressCount = 0;
        private int resultCount = 0;

        private ConcurrentDictionary<string, List<(int, string)>> resultsDict;
        private ConcurrentBag<string> failedList;
        private IEnumerable<KeyValuePair<string, List<(int, string)>>> resultsDictSorted;
        private IEnumerable<string> failedListSorted;

        private Regex keywordRegex, nameRegex;
        private GlobalDecompileContext decompileContext;
        private LoaderDialog loaderDialog;
        private UndertaleCodeEditor.CodeEditorTab editorTab;

        public readonly record struct Result(string Code, int LineNumber, string LineText);

        public ObservableCollection<Result> Results { get; set; } = new();

        public SearchInCodeWindow(string query = null, bool inAssembly = false)
        {
            InitializeComponent();
            DataContext = this;

            if (query is not null)
            {
                if (query.Length > 256 || query.Count(x => x == '\n') > 16)
                    return; // ignore overly long queries
                SearchTextBox.Text = query;
                SearchTextBox.SelectAll();
            }

            InAssemblyCheckBox.IsChecked = inAssembly;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e) => await Search();

        private async Task Search()
        {
            if (mainWindow.Data == null)
            {
                this.ShowError("No data.win loaded.");
                return;
            }
            if (mainWindow.Data.IsYYC())
            {
                this.ShowError("Can't search code in YYC game, there's no code to search.");
                return;
            }

            text = (SearchTextBox.Text ?? "").Replace("\r\n", "\n");
            if (string.IsNullOrEmpty(text))
                return;
            if (isSearchInProgress)
            {
                this.ShowError("Can't search while another search is in progress.");
                return;
            }

            isCaseSensitive = CaseSensitiveCheckBox.IsChecked ?? false;
            isRegexSearch = RegexSearchCheckBox.IsChecked ?? false;
            isMultilineRegex = MultilineRegexCheckBox.IsChecked ?? false;
            isInAssembly = InAssemblyCheckBox.IsChecked ?? false;

            bool filterByName = FilterByNameExpander.IsExpanded;
            IList<UndertaleCode> codeEntriesToSearch = mainWindow.Data.Code;

            if (isRegexSearch)
            {
                try
                {
                    RegexOptions options = RegexOptions.Compiled;
                    if (!isCaseSensitive) options |= RegexOptions.IgnoreCase;
                    if (isMultilineRegex) options |= RegexOptions.Multiline;
                    keywordRegex = new(text, options);
                }
                catch (ArgumentException ex)
                {
                    this.ShowError($"Invalid Regex: {ex.Message}");
                    return;
                }
            }

            if (filterByName)
            {
                string name = NameFilterTextBox.Text;
                if (!string.IsNullOrEmpty(name))
                {
                    bool nameIsCaseSensitive = NameCaseSensitiveCheckBox.IsChecked ?? false;
                    bool nameIsRegex = NameRegexSearchCheckBox.IsChecked ?? false;
                    if (nameIsRegex)
                    {
                        try
                        {
                            nameRegex = new(name, nameIsCaseSensitive ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            codeEntriesToSearch = mainWindow.Data.Code.Where(c => !string.IsNullOrEmpty(c.Name.Content) && nameRegex.IsMatch(c.Name.Content)).ToList();
                        }
                        catch (ArgumentException ex)
                        {
                            this.ShowError($"Invalid name Regex: {ex.Message}");
                            filterByName = false;
                        }
                    }
                    else
                    {
                        var comparison = nameIsCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                        codeEntriesToSearch = mainWindow.Data.Code.Where(c => !string.IsNullOrEmpty(c.Name.Content) && c.Name.Content.Contains(name, comparison)).ToList();
                    }
                }
            }

            if (codeEntriesToSearch.Count == 0)
            {
                this.ShowMessage("There are no code entries that match the name filter.");
                return;
            }

            mainWindow.IsEnabled = false;
            IsEnabled = false;
            isSearchInProgress = true;

            loaderDialog = new("Searching...", null) { PreventClose = true };
            loaderDialog.Show(this);

            Results.Clear();
            resultsDict = new();
            failedList = new();
            resultsDictSorted = null;
            failedListSorted = null;
            progressCount = 0;
            resultCount = 0;

            if (!isInAssembly)
                decompileContext = new GlobalDecompileContext(mainWindow.Data);

            loaderDialog.SavedStatusText = "Code entries";
            loaderDialog.Update(null, "Code entries", 0, codeEntriesToSearch.Count);

            await Task.Run(() => Parallel.ForEach(codeEntriesToSearch, SearchInUndertaleCode));
            await Task.Run(SortResults);

            loaderDialog.Maximum = null;
            loaderDialog.Update("Generating result list...");

            editorTab = isInAssembly ? UndertaleCodeEditor.CodeEditorTab.Disassembly : UndertaleCodeEditor.CodeEditorTab.Decompiled;
            ShowResults();

            loaderDialog.PreventClose = false;
            loaderDialog.Close();
            loaderDialog = null;

            mainWindow.IsEnabled = true;
            IsEnabled = true;
            isSearchInProgress = false;
        }

        private string GetCodeString(UndertaleCode code)
        {
            if (mainWindow.Project is null || !mainWindow.Project.TryGetCodeSource(code, out string decompiled))
                decompiled = new Underanalyzer.Decompiler.DecompileContext(decompileContext, code, mainWindow.Data.ToolInfo.DecompilerSettings).DecompileToString();
            return decompiled;
        }

        private void SearchInUndertaleCode(UndertaleCode code)
        {
            try
            {
                if (code is not null && code.ParentEntry is null)
                {
                    string codeText = isInAssembly
                        ? code.Disassemble(mainWindow.Data.Variables, mainWindow.Data.CodeLocals?.For(code), mainWindow.Data.CodeLocals is null)
                        : GetCodeString(code);
                    SearchInCodeText(code.Name.Content, codeText);
                }
            }
            catch (Exception)
            {
                failedList.Add(code.Name.Content);
            }

            Interlocked.Increment(ref progressCount);
            loaderDialog?.ReportProgress(progressCount);
        }

        private void SearchInCodeText(string codeName, string codeText)
        {
            List<int> results = new();
            if (isRegexSearch)
            {
                foreach (Match match in keywordRegex.Matches(codeText))
                    results.Add(match.Index);
            }
            else
            {
                StringComparison comparisonType = isCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                int index = 0;
                while ((index = codeText.IndexOf(text, index, comparisonType)) != -1)
                {
                    results.Add(index);
                    index += text.Length;
                }
            }

            bool nameWritten = false;
            int lineNumber = 0;
            int lineStartIndex = 0;

            foreach (int index in results)
            {
                for (int i = lineStartIndex; i < index; ++i)
                {
                    if (codeText[i] == '\n')
                    {
                        lineNumber++;
                        lineStartIndex = i + 1;
                    }
                }

                int lineEndIndex = codeText.IndexOf('\n', index);
                lineEndIndex = lineEndIndex == -1 ? codeText.Length : lineEndIndex;

                string lineText;
                if (lineEndIndex - lineStartIndex > 128)
                {
                    lineEndIndex = lineStartIndex + 128;
                    lineText = codeText[lineStartIndex..lineEndIndex] + "...";
                }
                else
                {
                    lineText = codeText[lineStartIndex..lineEndIndex];
                }

                if (!nameWritten)
                {
                    resultsDict[codeName] = new List<(int, string)>();
                    nameWritten = true;
                }
                resultsDict[codeName].Add((lineNumber + 1, lineText));
                Interlocked.Increment(ref resultCount);
            }
        }

        private void SortResults()
        {
            string[] codeNames = mainWindow.Data.Code.Select(x => x.Name.Content).ToArray();
            resultsDictSorted = resultsDict.OrderBy(c => Array.IndexOf(codeNames, c.Key));
            failedListSorted = failedList.OrderBy(c => Array.IndexOf(codeNames, c));
        }

        public void ShowResults()
        {
            static string GetWordEnding(int quantity, bool isResults)
            {
                if (isResults) return quantity != 1 ? "s" : "";
                return quantity != 1 ? "ies" : "y";
            }

            var resultsSorted = resultsDictSorted.ToArray();
            var failedSorted = failedListSorted.ToArray();
            foreach (var result in resultsSorted)
            {
                var code = result.Key;
                foreach (var (lineText, lineNumber) in result.Value)
                    Results.Add(new(code, lineText, lineNumber));
            }

            string str = $"{resultCount} result{GetWordEnding(resultCount, true)} found in {resultsSorted.Length} code entr{GetWordEnding(resultsSorted.Length, false)}.";
            if (failedSorted.Length > 0)
                str += $" {failedSorted.Length} code entr{GetWordEnding(failedSorted.Length, false)} with an error.";
            StatusBarTextBlock.Text = str;
        }

        private void OpenSelectedListViewItem(bool inNewTab = false, Result resultToOpen = default)
        {
            if (isSearchInProgress)
            {
                this.ShowError("Can't open results while a search is in progress.");
                return;
            }

            if (resultToOpen != default)
            {
                mainWindow.OpenCodeEntry(resultToOpen.Code, resultToOpen.LineNumber, editorTab, inNewTab);
            }
            else
            {
                foreach (Result result in ResultsListView.SelectedItems.Cast<Result>())
                {
                    mainWindow.OpenCodeEntry(result.Code, result.LineNumber, editorTab, inNewTab);
                    inNewTab = true; // only the first opens in the current tab
                }
            }
            mainWindow.Activate();
        }

        private async void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                e.Handled = true;
                await Search();
            }
        }

        private void ResultsListView_DoubleTapped(object sender, TappedEventArgs e) => OpenSelectedListViewItem();

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            e.Cancel = loaderDialog is not null;
            base.OnClosing(e);
        }
    }
}
