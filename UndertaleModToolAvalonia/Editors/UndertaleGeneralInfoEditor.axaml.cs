using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertaleGeneralInfoEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        public UndertaleGeneralInfoEditor()
        {
            InitializeComponent();
            // intercept the toggle (tunnel) to confirm before enabling the gms debugger, as the wpf version did
            DebuggerCheckBox.AddHandler(PointerPressedEvent, DebuggerCheckBox_PointerPressed, RoutingStrategies.Tunnel);
        }

        private void AddRoom_Click(object sender, RoutedEventArgs e) => RoomListGrid.AddNew();
        private void AddConstant_Click(object sender, RoutedEventArgs e) => ConstantsGrid.AddNew();

        private void SyncRoomList_Click(object sender, RoutedEventArgs e)
        {
            IList<UndertaleRoom> rooms = mainWindow.Data.Rooms;
            IList<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> roomOrder = (this.DataContext as GeneralInfoEditor).GeneralInfo.RoomOrder;
            roomOrder.Clear();
            foreach (var room in rooms)
                roomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>() { Resource = room });
        }

        private void DebuggerCheckBox_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (sender is not CheckBox checkBox)
                return;

            if (checkBox.IsChecked != true)
                return;

            e.Handled = true;
            var result = mainWindow.ShowQuestion("Are you sure that you want to enable GMS debugger?\n" +
                                                 "If you want to enable a debug mode in some game, then you need to use one of the scripts.");
            if (result == MessageBoxResult.Yes)
                checkBox.IsChecked = false;
        }
    }

    public class TimestampDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ulong timestamp)
                return "(error)";
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds((long)timestamp);
            if (parameter is string par && par == "GMT")
                return "GMT+0: " + dateTimeOffset.UtcDateTime.ToString();
            else
                return dateTimeOffset.LocalDateTime.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string dateTimeStr)
                return BindingOperations.DoNothing;
            if (!DateTime.TryParse(dateTimeStr, out DateTime dateTime))
                return BindingOperations.DoNothing;

            return (ulong)(new DateTimeOffset(dateTime).ToUnixTimeSeconds());
        }
    }
}
