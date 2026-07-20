using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using UndertaleModLib;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// the single main window instance. avalonia has no <c>Application.Current.MainWindow</c>, so this
        /// static accessor stands in for it (the wpf code used that property throughout).
        /// </summary>
        public static MainWindow Instance { get; private set; }

        /// <summary>the currently loaded data file, or null if nothing is open.</summary>
        public UndertaleData Data { get; set; }

        /// <summary>the open editor tabs.</summary>
        public ObservableCollection<Tab> Tabs { get; } = new();

        /// <summary>the currently active tab.</summary>
        public Tab CurrentTab { get; set; }

        public MainWindow() : this(null) { }

        public MainWindow(string[] startupArgs)
        {
            Instance = this;
            InitializeComponent();

            // editors bind visibility etc. to MainWindow properties via
            // {Binding DataContext.IsGMS2, RelativeSource=... AncestorType=MainWindow}, so the window's own
            // DataContext must be itself (1:1 with wpf's `this.DataContext = this`). without it those bindings
            // resolve against a null DataContext and GMS2-only fields never show/hide correctly.
            DataContext = this;

            EditorTabs.ItemsSource = Tabs;
            OpenInTab(new DescriptionView("Welcome to UndertaleModTool!",
                "Open a data.win file to get started, then click items on the left to view them."), true, "Welcome!");

            RestoreWindowPlacement();

            // associate GameMaker data files with the tool (no-op on non-windows platforms)
            if (Settings.Instance?.AutomaticFileAssociation == true)
            {
                try { FileAssociations.CreateAssociations(); } catch { }
            }

            UpdateMenuStates();

            // process startup args once the window is up (open a passed file; connect a child-file pipe)
            if (startupArgs is { Length: > 0 })
                Opened += async (_, _) => await ProcessStartupArgs(startupArgs);
        }

        /// <summary>greys out menu items that need a loaded data file / open project (1:1 with the wpf datatriggers).</summary>
        public void UpdateMenuStates()
        {
            bool hasData = Data is not null;
            bool inProject = hasData && FilePath is not null && Project is not null;

            if (MenuSave is null) // called before the view is built
                return;
            MenuSave.IsEnabled = MenuRun.IsEnabled = MenuRunDebug.IsEnabled = MenuRunSpecial.IsEnabled = hasData;
            MenuFind.IsEnabled = hasData;
            MenuProjectSave.IsEnabled = MenuProjectView.IsEnabled = MenuProjectClose.IsEnabled = inProject;
        }

        // handles command-line args passed to the gui: [dataFile] or [dataFile, childPipeKey] (child-file launch).
        private async Task ProcessStartupArgs(string[] args)
        {
            if (args.Length >= 1 && File.Exists(args[0]))
                await LoadFile(args[0]);
            if (args.Length >= 2)
                _ = ListenChildConnection(args[1]);
        }

        // restores the saved cross-platform window position/size (replaces the wpf Win32 WINDOWPLACEMENT handling)
        private void RestoreWindowPlacement()
        {
            var settings = Settings.Instance;
            if (settings is null || !settings.RememberWindowPlacements)
                return;

            if (settings.MainWindowWidth is > 0 && settings.MainWindowHeight is > 0)
            {
                Width = settings.MainWindowWidth.Value;
                Height = settings.MainWindowHeight.Value;
            }
            if (settings.MainWindowX is double x && settings.MainWindowY is double y)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Position = new PixelPoint((int)x, (int)y);
            }
            if (settings.MainWindowMaximized)
                WindowState = WindowState.Maximized;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            var settings = Settings.Instance;
            if (settings is not null && settings.RememberWindowPlacements)
            {
                settings.MainWindowMaximized = WindowState == WindowState.Maximized;
                if (WindowState == WindowState.Normal)
                {
                    settings.MainWindowX = Position.X;
                    settings.MainWindowY = Position.Y;
                    settings.MainWindowWidth = Width;
                    settings.MainWindowHeight = Height;
                }
                Settings.Save();
            }
            CloseChildFiles();
            base.OnClosing(e);
        }

        /// <summary>
        /// opens a resource in the tab host: navigates the current tab, or creates a new tab when requested
        /// (or when there is no current tab). the editor for the resource is chosen by the window data templates.
        /// </summary>
        internal void OpenInTab(object obj, bool isNewTab = false, string tabTitle = null)
        {
            Highlighted = obj;

            if (isNewTab || CurrentTab is null || Tabs.Count == 0)
            {
                var tab = new Tab(obj, Tabs.Count, tabTitle);
                Tabs.Add(tab);
                CurrentTab = tab;
                EditorTabs.SelectedItem = tab;
            }
            else
            {
                CurrentTab.NavigateTo(obj);
            }

            UpdateObjectLabel(obj);
        }

        /// <summary>whether the editor host has an editor/viewer template for the given asset.</summary>
        public bool HasEditorForAsset(object obj) => obj is UndertaleModLib.UndertaleResource;

        /// <summary>opens a code entry (by name) in the editor host. line/tab targeting is not yet implemented.</summary>
        public void OpenCodeEntry(string codeName, int lineNumber = -1, UndertaleCodeEditor.CodeEditorTab tab = UndertaleCodeEditor.CodeEditorTab.Unknown, bool inNewTab = false)
        {
            var code = Data?.Code?.ByName(codeName);
            if (code is not null)
                ChangeSelection(code, inNewTab);
        }

        private void EditorTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // note: this can fire during InitializeComponent before the EditorTabs field is assigned, so use sender
            if ((sender as TabControl)?.SelectedItem is Tab tab)
            {
                CurrentTab = tab;
                Highlighted = tab.CurrentObject;
                UpdateObjectLabel(tab.CurrentObject);
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Control)?.Tag is Tab tab)
                CloseTab(tab);
        }

        /// <summary>closes a tab and selects a sensible neighbour (or the welcome tab when none remain).</summary>
        internal void CloseTab(Tab tab)
        {
            int index = Tabs.IndexOf(tab);
            if (index < 0)
                return;

            // remember closed resource tabs so Ctrl+Shift+T can reopen them (skip the welcome/description tab)
            if (tab.CurrentObject is not DescriptionView)
                ClosedTabsHistory.Add(tab);

            Tabs.Remove(tab);
            for (int i = 0; i < Tabs.Count; i++)
                Tabs[i].TabIndex = i;

            if (Tabs.Count == 0)
            {
                OpenInTab(new DescriptionView("Welcome to UndertaleModTool!", "Open a data.win file to get started."), true, "Welcome!");
            }
            else if (CurrentTab == tab)
            {
                CurrentTab = Tabs[System.Math.Min(index, Tabs.Count - 1)];
                EditorTabs.SelectedItem = CurrentTab;
            }
        }

        /// <summary>
        /// returns an avalonia bitmap for the given <see cref="GMImage"/>, reusing a cached instance when one is
        /// still alive. (1:1 with the wpf method of the same name, which returned a <c>BitmapSource</c>.)
        /// </summary>
        public Bitmap GetBitmapSourceForImage(GMImage image)
        {
            return TextureCache.GetBitmap(image);
        }

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "GameMaker data files (.win, .unx, .ios, .droid, audiogroup*.dat)|*.win;*.unx;*.ios;*.droid;audiogroup*.dat|All files|*"
            };
            if (dlg.ShowDialog(this) != true)
                return;

            await LoadFile(dlg.FileName);
        }

        /// <summary>loads a data file into the window, rebuilding the tree; returns whether the load succeeded.</summary>
        internal async Task<bool> LoadFile(string path)
        {
            CloseChildFiles();
            StatusBar.Text = "Loading " + path + " ...";
            try
            {
                UndertaleData data = await Task.Run(() =>
                {
                    using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
                    return UndertaleIO.Read(stream);
                });

                Data = data;
                FilePath = path;
                UndertaleCachedImageLoader.Reset();
                BuildTree();
                Title = "UndertaleModTool - " + Path.GetFileName(path);
                StatusBar.Text = $"Loaded {Path.GetFileName(path)}";
                ChangeSelection(new DescriptionView(Path.GetFileName(path),
                    "Data file loaded. Select a resource on the left to edit it."));
                UpdateMenuStates();
                return true;
            }
            catch (Exception ex)
            {
                this.ShowError("Failed to load data file:\n" + ex.Message, "Load failed");
                StatusBar.Text = "Load failed.";
                return false;
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (Data is null)
            {
                this.ShowMessage("No data file is open.");
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "GameMaker data files (.win, .unx, .ios, .droid)|*.win;*.unx;*.ios;*.droid|All files|*",
                FileName = FilePath is null ? "data.win" : Path.GetFileName(FilePath)
            };
            if (dlg.ShowDialog(this) != true)
                return;

            await SaveFile(dlg.FileName);
        }

        /// <summary>writes the current data to a path, updating FilePath on success; returns whether it succeeded.</summary>
        internal async Task<bool> SaveFile(string path, bool _suppressDebug = false)
        {
            if (Data is null)
                return false;

            StatusBar.Text = "Saving...";
            try
            {
                await Task.Run(() =>
                {
                    using FileStream stream = new(path, FileMode.Create, FileAccess.Write);
                    UndertaleIO.Write(stream, Data);
                });
                FilePath = path;
                StatusBar.Text = $"Saved {Path.GetFileName(path)}";
                return true;
            }
            catch (Exception ex)
            {
                this.ShowError("Failed to save data file:\n" + ex.Message, "Save failed");
                StatusBar.Text = "Save failed.";
                return false;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().ShowDialogSync(this);
        }

        private void SearchInCode_Click(object sender, RoutedEventArgs e)
        {
            new Windows.SearchInCodeWindow().Show(this);
        }

        private void ResourceTree_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object selected = (sender as TreeView)?.SelectedItem;
            if (selected is null || selected is TreeCategory)
                return;
            if (selected is TreeLeaf leaf)
            {
                ChangeSelection(leaf.Target);
                return;
            }
            ChangeSelection(selected);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Data is not null)
                BuildTree((sender as TextBox)?.Text);
        }

        // whether a resource matches the tree search box (by name, case-insensitive; mirrors wpf's name filter)
        private static bool MatchesFilter(object item, string filter)
        {
            string text = item?.ToString();
            return text is not null && text.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }

        // builds the resource tree categories from the loaded data (a flat-but-grouped view; the wpf tree is richer).
        // an optional filter narrows each category to items whose name contains the text (wpf's FilteredViewConverter).
        private void BuildTree(string filter = null)
        {
            bool hasFilter = !string.IsNullOrWhiteSpace(filter);
            string trimmed = filter?.Trim();

            var nodes = new List<object>();

            // standalone editors at the top (wpf shows these as leaf nodes under "Data"); hidden while filtering
            // by name since they have no searchable resource name.
            if (!hasFilter)
            {
                if (Data.GeneralInfo is not null)
                    nodes.Add(new TreeLeaf("General info", new GeneralInfoEditor(Data.GeneralInfo, Data.Options, Data.Language)));
                if (Data.GlobalInitScripts is not null)
                    nodes.Add(new TreeLeaf("Global init", new GlobalInitEditor(Data.GlobalInitScripts)));
                if (Data.GameEndScripts is not null)
                    nodes.Add(new TreeLeaf("Game End scripts", new GameEndEditor(Data.GameEndScripts)));
            }

            void Add(string name, IEnumerable items)
            {
                if (items is null)
                    return;
                IEnumerable shown = hasFilter
                    ? items.Cast<object>().Where(o => MatchesFilter(o, trimmed)).ToList()
                    : items;
                nodes.Add(new TreeCategory(name, shown, items as IList));
            }

            Add("Audio Groups", Data.AudioGroups);
            Add("Sounds", Data.Sounds);
            Add("Sprites", Data.Sprites);
            Add("Backgrounds & Tilesets", Data.Backgrounds);
            Add("Paths", Data.Paths);
            Add("Scripts", Data.Scripts);
            Add("Shaders", Data.Shaders);
            Add("Fonts", Data.Fonts);
            Add("Timelines", Data.Timelines);
            Add("Game Objects", Data.GameObjects);
            Add("Rooms", Data.Rooms);
            Add("Extensions", Data.Extensions);
            Add("Texture Page Items", Data.TexturePageItems);
            Add("Code", Data.Code);
            Add("Variables", Data.Variables);
            Add("Functions", Data.Functions);
            Add("Code Locals", Data.CodeLocals);
            Add("Strings", Data.Strings);
            Add("Embedded Textures", Data.EmbeddedTextures);
            Add("Embedded Audio", Data.EmbeddedAudio);
            Add("Texture Group Info", Data.TextureGroupInfo);
            Add("Embedded Images", Data.EmbeddedImages);
            Add("Particle Systems", Data.ParticleSystems);
            Add("Particle System Emitters", Data.ParticleSystemEmitters);

            ResourceTree.ItemsSource = nodes;
        }
    }

    /// <summary>a top-level grouping of resources in the resource tree.</summary>
    public class TreeCategory
    {
        public string Name { get; }
        public IEnumerable Items { get; }
        /// <summary>the real backing data list (unfiltered), used by "Add"; <see cref="Items"/> may be a filtered copy.</summary>
        public IList Source { get; }

        public TreeCategory(string name, IEnumerable items, IList source)
        {
            Name = name;
            Items = items;
            Source = source;
        }
    }

    /// <summary>a single leaf node in the resource tree that opens a fixed editor target (general info, etc.).</summary>
    public class TreeLeaf
    {
        public string Name { get; }
        public object Target { get; }

        public TreeLeaf(string name, object target)
        {
            Name = name;
            Target = target;
        }
    }
}
