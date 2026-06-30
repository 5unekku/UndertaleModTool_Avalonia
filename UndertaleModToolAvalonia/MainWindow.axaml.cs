using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            EditorTabs.ItemsSource = Tabs;
            OpenInTab(new DescriptionView("Welcome to UndertaleModTool!",
                "Open a data.win file to get started, then click items on the left to view them."), true, "Welcome!");
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
        }

        private void EditorTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EditorTabs.SelectedItem is Tab tab)
            {
                CurrentTab = tab;
                Highlighted = tab.CurrentObject;
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Control)?.Tag is not Tab tab)
                return;

            int index = Tabs.IndexOf(tab);
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

            string path = dlg.FileName;
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
            }
            catch (Exception ex)
            {
                this.ShowError("Failed to load data file:\n" + ex.Message, "Load failed");
                StatusBar.Text = "Load failed.";
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

            string path = dlg.FileName;
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
            }
            catch (Exception ex)
            {
                this.ShowError("Failed to save data file:\n" + ex.Message, "Save failed");
                StatusBar.Text = "Save failed.";
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void ResourceTree_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResourceTree.SelectedItem is null || ResourceTree.SelectedItem is TreeCategory)
                return;
            ChangeSelection(ResourceTree.SelectedItem);
        }

        // builds the resource tree categories from the loaded data (a flat-but-grouped view; the wpf tree is richer)
        private void BuildTree()
        {
            var categories = new List<TreeCategory>();
            void Add(string name, IEnumerable items)
            {
                if (items is not null)
                    categories.Add(new TreeCategory(name, items));
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

            ResourceTree.ItemsSource = categories;
        }
    }

    /// <summary>a top-level grouping of resources in the resource tree.</summary>
    public class TreeCategory
    {
        public string Name { get; }
        public IEnumerable Items { get; }

        public TreeCategory(string name, IEnumerable items)
        {
            Name = name;
            Items = items;
        }
    }
}
