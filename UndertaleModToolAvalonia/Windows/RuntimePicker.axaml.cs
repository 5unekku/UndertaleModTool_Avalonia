using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UndertaleModLib;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    public partial class RuntimePicker : Window
    {
        public class Runtime
        {
            public string Version { get; set; }
            public string Path { get; set; }
            public string DebuggerPath { get; set; }
        }

        public ObservableCollection<Runtime> Runtimes { get; private set; } = new();
        public Runtime Selected { get; private set; } = null;

        public RuntimePicker()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Selected = Picker.SelectedItem as Runtime;
            Close();
        }

        public void DiscoverRuntimes(string dataFilePath, UndertaleData data)
        {
            Runtimes.Clear();
            DiscoverGameExe(dataFilePath, data);
            DiscoverGMS2();
            DiscoverGMS1();
        }

        private void DiscoverGameExe(string dataFilePath, UndertaleData data)
        {
            string gameExeName = data?.GeneralInfo?.FileName?.Content;
            if (gameExeName == null)
                return;

            string gameExePath = Paths.JoinVerifyWithinDirectory(System.IO.Path.GetDirectoryName(dataFilePath), gameExeName + ".exe");
            if (!File.Exists(gameExePath))
                return;

            Runtimes.Add(new Runtime { Version = "Game EXE", Path = gameExePath });
        }

        private void DiscoverGMS1()
        {
            string studioRunner = System.IO.Path.Join(Environment.ExpandEnvironmentVariables(Settings.Instance.GameMakerStudioPath), "Runner.exe");
            if (!File.Exists(studioRunner))
                return;

            string studioDebugger = System.IO.Path.Join(Environment.ExpandEnvironmentVariables(Settings.Instance.GameMakerStudioPath), @"GMDebug\GMDebug.exe");
            if (!File.Exists(studioDebugger))
                studioDebugger = null;

            Runtimes.Add(new Runtime { Version = "1.4.xxx", Path = studioRunner, DebuggerPath = studioDebugger });
        }

        private void DiscoverGMS2()
        {
            string runtimesPath = Environment.ExpandEnvironmentVariables(Settings.Instance.GameMakerStudio2RuntimesPath);
            if (!Directory.Exists(runtimesPath))
                return;

            Regex runtimePattern = new(@"^runtime-(.*)$");
            foreach (var runtimePath in Directory.EnumerateDirectories(runtimesPath))
            {
                Match m = runtimePattern.Match(System.IO.Path.GetFileName(runtimePath));
                if (!m.Success)
                    continue;

                string runtimeRunner = System.IO.Path.Join(runtimePath, @"windows\Runner.exe");
                string runtimeRunnerX64 = System.IO.Path.Join(runtimePath, @"windows\x64\Runner.exe");
                if (Environment.Is64BitOperatingSystem && File.Exists(runtimeRunnerX64))
                    runtimeRunner = runtimeRunnerX64;
                if (!File.Exists(runtimeRunner))
                    continue;

                Runtimes.Add(new Runtime { Version = m.Groups[1].Value, Path = runtimeRunner });
            }
        }

        public Runtime Pick(string dataFilePath, UndertaleData data)
        {
            DiscoverRuntimes(dataFilePath, data);
            if (Runtimes.Count == 0)
            {
                this.ShowError("Unable to find game EXE or any installed Studio runtime", "Run error");
                return null;
            }
            if (Runtimes.Count == 1)
                return Runtimes[0];

            this.ShowDialogSync(MainWindow.Instance);
            return Selected;
        }
    }
}
