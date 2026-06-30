using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool.Windows
{
    public partial class FindReferencesTypesDialog : Window
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        private readonly UndertaleResource sourceObj;
        private readonly UndertaleData data;
        private readonly bool dontShowWindow = false;

        public FindReferencesTypesDialog(UndertaleResource obj, UndertaleData data)
        {
            InitializeComponent();
            Opened += OnOpened;

            if (data.GeneralInfo is null)
            {
                this.ShowError("Cannot determine GameMaker version - \"General Info\" is null.");
                dontShowWindow = true;
                return;
            }

            (Type, string)[] sourceTypes = UndertaleResourceReferenceMap.GetTypeMapForVersion(obj.GetType(), data);
            if (sourceTypes is null)
            {
                this.ShowError($"Cannot get the source types for object of type \"{obj.GetType()}\".");
                dontShowWindow = true;
                return;
            }

            foreach (var typePair in sourceTypes)
                TypesList.Items.Add(new CheckBox { DataContext = typePair.Item1, Content = typePair.Item2, IsChecked = true });

            sourceObj = obj;
            this.data = data;
        }

        public FindReferencesTypesDialog(UndertaleData data)
        {
            InitializeComponent();
            Opened += OnOpened;

            if (data.GeneralInfo is null)
            {
                this.ShowError("Cannot determine GameMaker version - \"General Info\" is null.");
                dontShowWindow = true;
                return;
            }

            var ver = (data.GeneralInfo.Major, data.GeneralInfo.Minor, data.GeneralInfo.Release);
            var sourceTypes = UndertaleResourceReferenceMap.GetReferenceableTypes(ver);

            foreach (var typePair in sourceTypes)
            {
                if (data.Code is null && UndertaleResourceReferenceMap.CodeTypes.Contains(typePair.Key))
                    continue;
                TypesList.Items.Add(new CheckBox { DataContext = typePair.Key, Content = typePair.Value, IsChecked = true });
            }

            this.data = data;
        }

        private void OnOpened(object sender, EventArgs e)
        {
            if (dontShowWindow)
                Close();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in TypesList.Items)
                if (item is CheckBox checkBox)
                    checkBox.IsChecked = true;
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in TypesList.Items)
                if (item is CheckBox checkBox)
                    checkBox.IsChecked = false;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (sourceObj is not null)
            {
                HashSetTypesOverride typesList = new();
                foreach (var item in TypesList.Items)
                    if (item is CheckBox checkBox && checkBox.IsChecked == true && checkBox.DataContext is Type t)
                        typesList.Add(t);

                if (typesList.Count == 0)
                {
                    this.ShowError("At least one type should be selected.");
                    return;
                }

                FindReferencesResults dialog = null;
                try
                {
                    var results = UndertaleResourceReferenceMethodsMap.GetReferencesOfObject(sourceObj, data, typesList);
                    dialog = new(sourceObj, data, results);
                    dialog.Show();
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("An error occurred in the object references related window.\nPlease report this on GitHub.\n\n" + ex);
                    dialog?.Close();
                }
            }
            else
            {
                Dictionary<Type, string> typesDict = new();
                foreach (var item in TypesList.Items)
                    if (item is CheckBox checkBox && checkBox.IsChecked == true && checkBox.DataContext is Type t)
                        typesDict[t] = checkBox.Content as string;

                if (typesDict.Count == 0)
                {
                    this.ShowError("At least one type should be selected.");
                    return;
                }

                if (typesDict.Count > 1 && typesDict.ContainsKey(typeof(UndertaleString)) && data.Strings.Count > 5000)
                {
                    var res = this.ShowQuestion("You have selected the \"Strings\" when there are a lot of strings.\n" +
                                                "That could make the search process noticeably longer.\nDo you want to proceed?");
                    if (res != MessageBoxResult.Yes)
                        return;
                }

                Hide();
                FindReferencesResults dialog = null;
                try
                {
                    var results = await UndertaleResourceReferenceMethodsMap.GetUnreferencedObjects(data, typesDict);
                    dialog = new(data, results);
                    dialog.Show();
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("An error occurred in the object references related window.\nPlease report this on GitHub.\n\n" + ex);
                    dialog?.Close();
                }
            }

            Close();
        }
    }
}
