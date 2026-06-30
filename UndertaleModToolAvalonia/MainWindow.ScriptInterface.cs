using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    // the UndertaleModTool scripting interface: lets .csx scripts drive the tool (run via Roslyn CSharpScript with
    // `this` as the IScriptInterface globals). a functional core; progress/output use the status bar + simple dialogs.
    public partial class MainWindow : IScriptInterface
    {
        private ScriptOptions scriptOptions;
        private int progressValue;

        public string ScriptPath { get; set; }
        public object Selected => CurrentTab?.CurrentObject;
        public bool CanSave => Data is not null;
        public bool ScriptExecutionSuccess { get; private set; } = true;
        public string ScriptErrorMessage { get; private set; } = "";
        public string ScriptErrorType { get; private set; } = "";
        public string ExePath => Program.GetExecutableDirectory();
        public bool IsAppClosed { get; private set; }
        public Action<Action> MainThreadAction => action => Dispatcher.UIThread.Invoke(action);

        private void SetupScriptOptions()
        {
            scriptOptions ??= ScriptingUtil.CreateDefaultScriptOptions()
                .AddImports("UndertaleModTool")
                .AddReferences(GetType().GetTypeInfo().Assembly, typeof(Newtonsoft.Json.JsonConvert).GetTypeInfo().Assembly);
        }

        /// <summary>runs a .csx script (chosen from the menu); refreshes the resource tree afterward.</summary>
        public async Task RunScript(string path)
        {
            SetupScriptOptions();
            ScriptPath = path;
            ScriptExecutionSuccess = true;
            ScriptErrorMessage = "";
            ScriptErrorType = "";

            SetStatus("Running " + Path.GetFileName(path) + " ...");
            try
            {
                string text = File.ReadAllText(path);
                await CSharpScript.EvaluateAsync(text, scriptOptions.WithFilePath(path).WithFileEncoding(Encoding.UTF8),
                                                 this, typeof(IScriptInterface));
                SetStatus("Script finished: " + Path.GetFileName(path));
            }
            catch (Exception exc)
            {
                ScriptExecutionSuccess = false;
                ScriptErrorMessage = exc.Message;
                ScriptErrorType = exc.GetType().Name;
                this.ShowError(ScriptingUtil.PrettifyException(in exc), "Script error");
                SetStatus("Script failed.");
            }

            if (Data is not null)
                BuildTree();
        }

        private async void RunScriptMenu_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { DefaultExt = ".csx", Filter = "Scripts (.csx)|*.csx|All files|*" };
            if (dlg.ShowDialog(this) == true)
                await RunScript(dlg.FileName);
        }

        private void SetStatus(string text)
        {
            Dispatcher.UIThread.Post(() => { if (StatusBar is not null) StatusBar.Text = text; });
        }

        // --- IScriptInterface members ---

        public bool MakeNewDataFile()
        {
            Data = UndertaleData.CreateNew();
            FilePath = null;
            Dispatcher.UIThread.Post(BuildTree);
            return true;
        }

        public void ScriptMessage(string message) => this.ShowMessage(message, "Script message");
        public void ScriptWarning(string message) => this.ShowWarning(message, "Script warning");
        public bool ScriptQuestion(string message) => this.ShowQuestion(message, MessageBoxImage.Question, "Script Question") == MessageBoxResult.Yes;

        public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
        {
            this.ShowError(error, title);
            if (SetConsoleText)
                SetUMTConsoleText(error);
        }

        public void SetUMTConsoleText(string message) => SetStatus(message);

        public void ScriptOpenURL(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        }

        public bool RunUMTScript(string path)
        {
            // run a sub-script synchronously (the caller is already on a background script thread)
            RunScript(path).GetAwaiter().GetResult();
            return ScriptExecutionSuccess;
        }

        public bool LintUMTScript(string path)
        {
            SetupScriptOptions();
            try
            {
                string text = File.ReadAllText(path);
                CSharpScript.Create(text, scriptOptions.WithFilePath(path).WithFileEncoding(Encoding.UTF8), typeof(IScriptInterface)).Compile();
                return true;
            }
            catch (Exception exc)
            {
                ScriptErrorMessage = exc.Message;
                return false;
            }
        }

        public void InitializeScriptDialog() { }

        public string GetDecompiledText(string codeName, GlobalDecompileContext context = null, IDecompileSettings settings = null)
            => GetDecompiledText(Data?.Code?.ByName(codeName), context, settings);

        public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null, IDecompileSettings settings = null)
        {
            if (code is null)
                return "";
            if (code.ParentEntry is not null)
                return $"// This code entry is a reference to an anonymous function within {code.ParentEntry.Name?.Content}, view it there.";
            try
            {
                context ??= new GlobalDecompileContext(Data);
                settings ??= Data.ToolInfo.DecompilerSettings;
                return new Underanalyzer.Decompiler.DecompileContext(context, code, settings).DecompileToString();
            }
            catch (Exception e)
            {
                return "/*\nDECOMPILER FAILED!\n\n" + e + "\n*/";
            }
        }

        public string GetDisassemblyText(string codeName) => GetDisassemblyText(Data?.Code?.ByName(codeName));

        public string GetDisassemblyText(UndertaleCode code)
        {
            if (code is null)
                return "";
            if (code.ParentEntry is not null)
                return $"; This code entry is a reference to an anonymous function within {code.ParentEntry.Name?.Content}, view it there.";
            try
            {
                return code.Disassemble(Data.Variables, Data.CodeLocals?.For(code), Data.CodeLocals is null);
            }
            catch (Exception e)
            {
                return ";  DISASSEMBLY FAILED!\n;   " + string.Join("\n;   ", e.ToString().Split('\n'));
            }
        }

        public string ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose)
        {
            return Dispatcher.UIThread.Invoke(() =>
            {
                var dlg = new TextInputDialog(title, label, defaultInput, cancelText, submitText, isMultiline, preventClose);
                dlg.ShowDialogSync(this);
                return dlg.Result ? dlg.InputText : null;
            });
        }

        public string SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline, bool showDialog = true)
            => ScriptInputDialog(title, label, defaultValue, "Cancel", "OK", allowMultiline, false);

        public void SimpleTextOutput(string title, string label, string message, bool allowMultiline)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                var dlg = new TextInputDialog(title, label, message, null, "OK", true, false);
                dlg.ShowDialogSync(this);
            });
        }

        public Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, bool showInDecompiledView, IOrderedEnumerable<string> failedList = null)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                var window = new Windows.ClickableTextOutput(title, query, resultsCount, resultsDict, showInDecompiledView, failedList);
                window.GenerateResults();
                window.Show();
            });
            return Task.CompletedTask;
        }

        public Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict, bool showInDecompiledView, IEnumerable<string> failedList = null)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                var window = new Windows.ClickableTextOutput(title, query, resultsCount, resultsDict, showInDecompiledView, failedList);
                window.GenerateResults();
                window.Show();
            });
            return Task.CompletedTask;
        }

        public void SetFinishedMessage(bool isFinishedMessageEnabled) { }

        public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
            => SetStatus($"{status} {progressValue}/{maxValue}");
        public void SetProgressBar(string message, string status, double progressValue, double maxValue)
            => SetStatus($"{status} {progressValue}/{maxValue}");
        public void SetProgressBar() { }
        public void InitializeProgressDialog(string title, string status) => SetStatus(status);
        public void UpdateProgressValue(double progressValue) => SetStatus(progressValue.ToString());
        public void UpdateProgressStatus(string status) => SetStatus(status);
        public void AddProgress(int amount) => progressValue += amount;
        public void IncrementProgress() => progressValue++;
        public void AddProgressParallel(int amount) => Interlocked.Add(ref progressValue, amount);
        public void IncrementProgressParallel() => Interlocked.Increment(ref progressValue);
        public int GetProgress() => progressValue;
        public void SetProgress(int value) => progressValue = value;
        public void HideProgressBar() { }
        public void EnableUI() { }
        public void StartProgressBarUpdater() { }
        public Task StopProgressBarUpdater() => Task.CompletedTask;

        public string PromptChooseDirectory()
        {
            return Dispatcher.UIThread.Invoke(() =>
            {
                var dlg = new VistaFolderBrowserDialog();
                return dlg.ShowDialog(this) == true ? dlg.SelectedPath : null;
            });
        }

        public string PromptLoadFile(string defaultExt, string filter)
        {
            return Dispatcher.UIThread.Invoke(() =>
            {
                var dlg = new OpenFileDialog { DefaultExt = defaultExt, Filter = filter ?? "All files|*" };
                return dlg.ShowDialog(this) == true ? dlg.FileName : null;
            });
        }

        public string PromptSaveFile(string defaultExt, string filter)
        {
            return Dispatcher.UIThread.Invoke(() =>
            {
                var dlg = new SaveFileDialog { DefaultExt = defaultExt, Filter = filter ?? "All files|*" };
                return dlg.ShowDialog(this) == true ? dlg.FileName : null;
            });
        }
    }
}
