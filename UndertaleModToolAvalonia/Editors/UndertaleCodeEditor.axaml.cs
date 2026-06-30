using System;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    // functional core of the code editor: shows decompiled GML + disassembly with GML highlighting.
    // the advanced features from the wpf version (clickable name links, in-editor compile, search panel,
    // gettext annotations, zoom) are follow-ups; see avalonia-port memory.
    public partial class UndertaleCodeEditor : DataUserControl
    {
        /// <summary>which tab of the code editor to show (decompiled gml or raw disassembly).</summary>
        public enum CodeEditorTab
        {
            Unknown,
            Decompiled,
            Disassembly
        }

        private static MainWindow mainWindow => MainWindow.Instance;
        private static IHighlightingDefinition gmlHighlighting;
        private bool decompiledReady;

        public UndertaleCodeEditor()
        {
            InitializeComponent();

            LoadHighlighting();
            if (gmlHighlighting is not null)
                DecompiledEditor.SyntaxHighlighting = gmlHighlighting;

            DataContextChanged += (_, _) => ShowCode();
        }

        private static void LoadHighlighting()
        {
            if (gmlHighlighting is not null)
                return;
            try
            {
                using var stream = AssetLoader.Open(new Uri("avares://UndertaleModTool/Resources/GML.xshd"));
                using var reader = XmlReader.Create(stream);
                gmlHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
            catch
            {
                // highlighting is optional; fall back to plain text
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // both views are populated on data change, so nothing extra is needed here
        }

        private void ShowCode()
        {
            if (DataContext is not UndertaleCode code)
                return;

            UndertaleData data = mainWindow?.Data;
            if (data is null)
                return;

            // disassembly (fast, inline)
            try
            {
                if (code.ParentEntry != null)
                    DisassemblyEditor.Text = "; This code entry is a reference to an anonymous function within " + code.ParentEntry.Name.Content + ", view it there.";
                else
                    DisassemblyEditor.Text = code.Disassemble(data.Variables, data.CodeLocals?.For(code), data.CodeLocals is null);
            }
            catch (Exception ex)
            {
                DisassemblyEditor.Text = ";  EXCEPTION!\n;   " + string.Join("\n;   ", ex.ToString().Split('\n'));
            }

            // decompiled (can be slow, run off the ui thread)
            if (code.ParentEntry != null)
            {
                DecompiledEditor.Text = "// This code entry is a reference to an anonymous function within " + code.ParentEntry.Name.Content + ", view it there.";
                return;
            }

            decompiledReady = false;
            DecompiledEditor.Text = "// Decompiling, please wait...";
            UndertaleCode current = code;
            Task.Run(() =>
            {
                string result;
                bool ok = true;
                try
                {
                    var context = new GlobalDecompileContext(data);
                    result = new Underanalyzer.Decompiler.DecompileContext(context, current, data.ToolInfo.DecompilerSettings).DecompileToString();
                }
                catch (Exception ex)
                {
                    result = "/*\n  EXCEPTION!\n  " + ex + "\n*/";
                    ok = false;
                }
                Dispatcher.UIThread.Post(() =>
                {
                    if (DataContext == current)
                    {
                        DecompiledEditor.Text = result;
                        decompiledReady = ok;
                    }
                });
            });
        }

        private void CompileButton_Click(object sender, RoutedEventArgs e) => CompileCode();

        private void DecompiledEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                e.Handled = true;
                CompileCode();
            }
        }

        // recompiles the edited GML back into the code entry (1:1 with the wpf Ctrl+K compile path).
        private async void CompileCode()
        {
            if (DataContext is not UndertaleCode code || mainWindow?.Data is null)
                return;
            if (code.ParentEntry is not null)
            {
                mainWindow.ShowMessage("This is a reference to an anonymous function; edit it within its parent entry.");
                return;
            }
            if (!decompiledReady)
            {
                mainWindow.ShowMessage("Please wait for decompilation to finish before compiling.");
                return;
            }

            string source = DecompiledEditor.Text;
            UndertaleData data = mainWindow.Data;
            CompileResult result = default;
            string rootException = null;

            await Task.Run(() =>
            {
                try
                {
                    var group = new CompileGroup(data) { MainThreadAction = f => Dispatcher.UIThread.Invoke(f) };
                    group.QueueCodeReplace(code, source);
                    result = group.Compile();
                }
                catch (Exception ex)
                {
                    rootException = ex.ToString();
                }
            });

            if (rootException is not null)
            {
                mainWindow.ShowError(Truncate(rootException, 512), "Compiler error");
                return;
            }
            if (!result.Successful)
            {
                mainWindow.ShowError(Truncate(result.PrintAllErrors(false), 512), "Compiler error");
                return;
            }

            mainWindow.ShowMessage("Code compiled successfully.");
            ShowCode();
        }

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value[..max] + "...";
    }
}
