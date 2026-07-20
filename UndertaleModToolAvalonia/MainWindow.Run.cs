using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Interactivity;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    // run-game handlers ported 1:1 from the wpf Command_Run / Command_RunSpecial / Command_RunDebug.
    // note: game runners and the studio debugger are windows executables; on other platforms Process.Start
    // will just fail to launch them, same as trying to run any .exe.
    public partial class MainWindow
    {
        // once-per-session guard for the "temp run doesn't save" warning
        private bool WasWarnedAboutTempRun = false;

        // F5: run the game (saving to the project's data file, or a temp copy when no project is open)
        private async void Run_Click(object sender, RoutedEventArgs e)
        {
            if (Data is null || FilePath is null)
            {
                ScriptError("Nothing to run!");
                return;
            }

            string gameExeName = Data?.GeneralInfo?.FileName?.Content;
            if (gameExeName is null)
            {
                ScriptError("Null game executable name or location");
                return;
            }

            string saveDataFilePath, gameExePath;
            bool saveSucceeded;
            if (Project is not null)
            {
                saveDataFilePath = Project.SaveDataPath;
                gameExePath = Paths.TryJoinVerifyWithinDirectory(Path.GetDirectoryName(saveDataFilePath), $"{gameExeName}.exe");
                if (gameExePath is null)
                {
                    ScriptError("Failed to find valid game executable path; escaped directory");
                    return;
                }
                if (!File.Exists(gameExePath))
                {
                    ScriptError($"Cannot find game executable path, expected to find it at: {gameExePath}");
                    return;
                }

                saveSucceeded = await SaveFile(saveDataFilePath, false);
            }
            else
            {
                if (!WasWarnedAboutTempRun && Settings.Instance.TempRunMessageShow)
                {
                    ScriptMessage(@"WARNING:
Temp running the game does not permanently
save your changes. Please ""Save"" the game
to save your changes. Closing UndertaleModTool
without using the ""Save"" option can
result in loss of work.");
                    WasWarnedAboutTempRun = true;
                }

                gameExePath = Paths.TryJoinVerifyWithinDirectory(Path.GetDirectoryName(FilePath), $"{gameExeName}.exe");
                if (gameExePath is null)
                {
                    ScriptError("Failed to find valid game executable path; escaped directory");
                    return;
                }
                if (!File.Exists(gameExePath))
                {
                    ScriptError($"Cannot find game executable path, expected to find it at: {gameExePath}");
                    return;
                }

                // disable debugger/steam, and save to the folder where the game was loaded from
                bool oldDisableDebuggerState = Data.GeneralInfo.IsDebuggerDisabled;
                int oldSteamValue = Data.GeneralInfo.SteamAppID;
                Data.GeneralInfo.SteamAppID = 0;
                Data.GeneralInfo.IsDebuggerDisabled = true;
                saveDataFilePath = Path.Join(Path.GetDirectoryName(FilePath), "mod_temprun.temp");
                saveSucceeded = await SaveFile(saveDataFilePath, false);
                Data.GeneralInfo.SteamAppID = oldSteamValue;
                Data.GeneralInfo.IsDebuggerDisabled = oldDisableDebuggerState;
            }

            if (saveSucceeded)
            {
                if (!File.Exists(saveDataFilePath))
                {
                    ScriptError($"Cannot find game path, expected to find it at: {saveDataFilePath}");
                    return;
                }
                Process.Start(new ProcessStartInfo(gameExePath, new[] { "-game", saveDataFilePath }));
            }
            else
            {
                this.ShowWarning("Save failed, cannot run.");
            }
        }

        // Shift+F5: run using a picked runtime (disabling the debugger first)
        private async void RunSpecial_Click(object sender, RoutedEventArgs e)
        {
            if (Data is null)
                return;

            bool saveOk = true;
            if (!Data.GeneralInfo.IsDebuggerDisabled)
            {
                if (this.ShowQuestion("The game has the debugger enabled. Would you like to disable it so the game will run?") == MessageBoxResult.Yes)
                {
                    Data.GeneralInfo.IsDebuggerDisabled = true;
                    if (!await DoSaveDialog())
                    {
                        this.ShowError("You must save your changes to run.");
                        Data.GeneralInfo.IsDebuggerDisabled = false;
                        return;
                    }
                }
                else
                {
                    this.ShowError("Use the \"Run game using debugger\" option to run this game.");
                    return;
                }
            }
            else
            {
                Data.GeneralInfo.IsDebuggerDisabled = true;
                if (this.ShowQuestion("Save changes first?") == MessageBoxResult.Yes)
                    saveOk = await DoSaveDialog();
            }

            if (FilePath is null)
            {
                this.ShowWarning("The file must be saved in order to be run.");
            }
            else if (saveOk)
            {
                var runtime = new RuntimePicker().Pick(FilePath, Data);
                if (runtime is not null)
                    Process.Start(new ProcessStartInfo(runtime.Path, new[] { "-game", FilePath, "-debugoutput", Path.ChangeExtension(FilePath, ".gamelog.txt") }));
            }
        }

        // Alt+F5: run using a picked runtime with the GMS debugger attached
        private async void RunDebug_Click(object sender, RoutedEventArgs e)
        {
            if (Data is null)
                return;

            if (this.ShowQuestion("Are you sure that you want to run the game with GMS debugger?\n" +
                                  "If you want to enable a debug mode in some game, then you need to use one of the scripts.") != MessageBoxResult.Yes)
                return;

            bool origDbg = Data.GeneralInfo.IsDebuggerDisabled;
            Data.GeneralInfo.IsDebuggerDisabled = false;

            bool saveOk = await DoSaveDialog(true);
            if (FilePath is null)
            {
                this.ShowWarning("The file must be saved in order to be run.");
            }
            else if (saveOk)
            {
                var runtime = new RuntimePicker().Pick(FilePath, Data);
                if (runtime is null)
                {
                    Data.GeneralInfo.IsDebuggerDisabled = origDbg;
                    return;
                }
                if (runtime.DebuggerPath is null)
                {
                    this.ShowError("The selected runtime does not support debugging.", "Run error");
                    Data.GeneralInfo.IsDebuggerDisabled = origDbg;
                    return;
                }

                string tempProject = Path.GetTempFileName().Replace(".tmp", ".gmx");
                File.WriteAllText(tempProject, @"<!-- Without this file the debugger crashes, but it doesn't actually need to contain anything! -->
<assets>
  <Configs name=""configs"">
    <Config>Configs\Default</Config>
  </Configs>
  <NewExtensions/>
  <sounds name=""sound""/>
  <sprites name=""sprites""/>
  <backgrounds name=""background""/>
  <paths name=""paths""/>
  <objects name=""objects""/>
  <rooms name=""rooms""/>
  <help/>
  <TutorialState>
    <IsTutorial>0</IsTutorial>
    <TutorialName></TutorialName>
    <TutorialPage>0</TutorialPage>
  </TutorialState>
</assets>");

                Process.Start(new ProcessStartInfo(runtime.Path, new[] { "-game", FilePath, "-debugoutput", Path.ChangeExtension(FilePath, ".gamelog.txt") }));
                Process.Start(runtime.DebuggerPath, "-d=\"" + Path.ChangeExtension(FilePath, ".yydebug") + "\" -t=\"127.0.0.1\" -tp=" + Data.GeneralInfo.DebuggerPort + " -p=\"" + tempProject + "\"");
            }
            Data.GeneralInfo.IsDebuggerDisabled = origDbg;
        }
    }
}
