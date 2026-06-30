using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CodeEditorTab = UndertaleModTool.UndertaleCodeEditor.CodeEditorTab;

namespace UndertaleModTool.Windows
{
    // search-results window with clickable code-entry + line links. the wpf version used a RichTextBox/FlowDocument
    // with Hyperlinks; avalonia has no FlowDocument, so this builds a panel of clickable TextBlocks instead.
    public partial class ClickableTextOutput : Window
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        public string Query { get; }
        public int ResultsCount { get; }

        private readonly IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict;
        private readonly IEnumerable<string> failedList;
        private readonly CodeEditorTab editorTab;

        public ClickableTextOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, bool editorDecompile, IOrderedEnumerable<string> failedList = null)
            : this(title, query, resultsCount, resultsDict.ToDictionary(x => x.Key, x => x.Value), editorDecompile, failedList?.ToList())
        {
        }

        public ClickableTextOutput(string title, string query, int resultsCount, IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict, bool editorDecompile, IEnumerable<string> failedList = null)
        {
            InitializeComponent();
            Title = title;
            Query = query;
            ResultsCount = resultsCount;
            this.resultsDict = resultsDict;
            this.editorTab = editorDecompile ? CodeEditorTab.Decompiled : CodeEditorTab.Disassembly;
            this.failedList = failedList;
        }

        public void GenerateResults()
        {
            OutputPanel.Children.Clear();

            if (failedList is not null)
            {
                var failed = failedList.ToList();
                if (failed.Count > 0)
                {
                    string header = failed.Count == 1
                        ? "There is 1 code entry that encountered an error while searching:"
                        : $"There are {failed.Count} code entries that encountered an error while searching:";
                    OutputPanel.Children.Add(new TextBlock { Text = header, FontWeight = FontWeight.Bold, Foreground = Brushes.OrangeRed, TextWrapping = TextWrapping.Wrap });
                    OutputPanel.Children.Add(new TextBlock { Text = string.Join(", ", failed), Foreground = Brushes.OrangeRed, TextWrapping = TextWrapping.Wrap, Margin = new(0, 0, 0, 8) });
                }
            }

            int resCount = resultsDict.Count;
            OutputPanel.Children.Add(new TextBlock { Text = $"{ResultsCount} results in {resCount} code entries for \"{Query}\".", FontWeight = FontWeight.Bold, Margin = new(0, 0, 0, 6) });

            int totalLineCount = resultsDict.Select(x => x.Value.Count).Sum();
            bool tooManyLines = totalLineCount > 10000;
            if (tooManyLines)
                mainWindow.ShowWarning($"There are too many code lines to display ({totalLineCount}), so the line numbers aren't clickable.");

            foreach (var result in resultsDict)
            {
                var headerRow = new WrapPanel { Margin = new(0, 6, 0, 0) };
                headerRow.Children.Add(new TextBlock { Text = "Results in " });
                headerRow.Children.Add(MakeLink(result.Key, result.Key, -1));
                headerRow.Children.Add(new TextBlock { Text = ":" });
                OutputPanel.Children.Add(headerRow);

                foreach (var (lineNum, codeLine) in result.Value)
                {
                    var row = new WrapPanel();
                    if (!tooManyLines)
                        row.Children.Add(MakeLink($"Line {lineNum}", result.Key, lineNum));
                    else
                        row.Children.Add(new TextBlock { Text = $"Line {lineNum}" });
                    row.Children.Add(new TextBlock { Text = $": {codeLine}", TextWrapping = TextWrapping.Wrap });
                    OutputPanel.Children.Add(row);
                }
            }
        }

        private TextBlock MakeLink(string text, string codeName, int lineNum)
        {
            var link = new TextBlock
            {
                Text = text,
                Foreground = Brushes.DeepSkyBlue,
                TextDecorations = TextDecorations.Underline,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            link.PointerPressed += (_, e) =>
            {
                bool middle = e.GetCurrentPoint(link).Properties.IsMiddleButtonPressed;
                mainWindow.OpenCodeEntry(codeName, lineNum, editorTab, middle);
            };
            return link;
        }

        private void Button_Click(object sender, RoutedEventArgs e) => Close();
    }
}
