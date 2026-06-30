using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace UndertaleModTool
{
    public partial class FlagsBox : UserControl
    {
        public static readonly StyledProperty<object> ValueProperty =
            AvaloniaProperty.Register<FlagsBox, object>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>one checkbox item per enum flag (rebuilt whenever <see cref="Value"/> changes).</summary>
        public ObservableCollection<FlagItem> Flags { get; } = new();

        public FlagsBox()
        {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ValueProperty)
                RebuildFlags();
        }

        private void RebuildFlags()
        {
            Flags.Clear();
            if (Value is not Enum current)
                return;

            foreach (object flag in Enum.GetValues(current.GetType()))
                Flags.Add(new FlagItem(this, (Enum)flag));
        }

        internal bool HasFlag(Enum flag)
        {
            return Value is Enum current && (Convert.ToUInt64(current) & Convert.ToUInt64(flag)) == Convert.ToUInt64(flag);
        }

        // replaces the wpf IMultiValueConverter.ConvertBack (avalonia has no ConvertBack on multi-value converters)
        internal void SetFlag(Enum flag, bool set)
        {
            if (Value is not Enum current)
                return;

            Type type = current.GetType();
            ulong value = Convert.ToUInt64(current);
            ulong bit = Convert.ToUInt64(flag);
            value = set ? (value | bit) : (value & ~bit);
            Value = Enum.ToObject(type, value);
        }
    }

    /// <summary>a single enum flag presented as a checkbox; toggling it updates the owner's value.</summary>
    public class FlagItem : INotifyPropertyChanged
    {
        private readonly FlagsBox owner;
        private readonly Enum flag;

        public FlagItem(FlagsBox owner, Enum flag)
        {
            this.owner = owner;
            this.flag = flag;
        }

        public string Name => flag.ToString();

        public bool IsChecked
        {
            get => owner.HasFlag(flag);
            set
            {
                owner.SetFlag(flag, value);
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
