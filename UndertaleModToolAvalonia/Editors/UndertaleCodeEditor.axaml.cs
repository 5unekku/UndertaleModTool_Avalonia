using System;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using UndertaleModLib;
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

            DecompiledEditor.Text = "// Decompiling, please wait...";
            UndertaleCode current = code;
            Task.Run(() =>
            {
                string result;
                try
                {
                    var context = new GlobalDecompileContext(data);
                    result = new Underanalyzer.Decompiler.DecompileContext(context, current, data.ToolInfo.DecompilerSettings).DecompileToString();
                }
                catch (Exception ex)
                {
                    result = "/*\n  EXCEPTION!\n  " + ex + "\n*/";
                }
                Dispatcher.UIThread.Post(() =>
                {
                    if (DataContext == current)
                        DecompiledEditor.Text = result;
                });
            });
        }
    }
}
