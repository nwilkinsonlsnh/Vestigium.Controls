using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace Vestigium.Controls.PropertiesGrid;

public sealed class VestigiumPropertyItem : INotifyPropertyChanged
{
    internal PropertyEngine? Engine;
    internal PropertyDescriptor? Descriptor;
    internal List<object> Owners { get; } = [];
    internal VestigiumPropertyItem? Parent;
    internal IList? List;
    internal int Index = -1;
    internal Type? PropertyType;
    internal object? DefaultValue;
    internal bool HasDefault;
    internal HashSet<object> Ancestors { get; } = [];

    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = PropertyRules.MiscCategory;
    public string Description { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public int Depth { get; init; }
    public bool IsCategory { get; init; }
    public IReadOnlyList<string>? Choices { get; init; }

    public VestigiumPropertyEditorKind Kind
    {
        get;
        set => Set(ref field, value);
    }

    public bool IsReadOnly
    {
        get;
        set => Set(ref field, value);
    }

    public bool IsMixed
    {
        get;
        set => Set(ref field, value);
    }

    public bool CanReset
    {
        get;
        set => Set(ref field, value);
    }

    public bool CanExpand
    {
        get;
        set => Set(ref field, value);
    }

    public bool IsExpanded
    {
        get;
        set => Set(ref field, value);
    }

    public bool IsCircular { get; internal set; }
    public bool IsUnsigned { get; internal set; }
    public int DecimalPlaces { get; internal set; }

    public object? Value
    {
        get;
        set
        {
            if (Set(ref field, value))
            {
                OnPropertyChanged(nameof(Display));
                OnPropertyChanged(nameof(EditText));
                OnPropertyChanged(nameof(NumericValue));
                OnPropertyChanged(nameof(BoolValue));
                OnPropertyChanged(nameof(EnumValue));
                OnPropertyChanged(nameof(DateValue));
                OnPropertyChanged(nameof(Swatch));
            }
        }
    }

    public string Display => IsCircular
        ? "(circular)"
        : PropertyRules.Format(Value, Kind, IsMixed);

    public string EditText
    {
        get => IsMixed ? string.Empty : Display;
        set
        {
            if (IsReadOnly || IsCategory) return;
            Engine?.TryCommit(this, value);
        }
    }

    public decimal NumericValue
    {
        get
        {
            try { return Value is null || IsMixed ? 0m : Convert.ToDecimal(Value); }
            catch { return 0m; }
        }
        set
        {
            if (IsReadOnly) return;
            Engine?.TryCommit(this, value);
        }
    }

    public bool? BoolValue
    {
        get => IsMixed ? null : Value as bool?;
        set
        {
            if (IsReadOnly || value is null) return;
            Engine?.TryCommit(this, value);
        }
    }

    public string? EnumValue
    {
        get => IsMixed || Value is null ? null : Convert.ToString(Value);
        set
        {
            if (IsReadOnly || value is null) return;
            Engine?.TryCommit(this, value);
        }
    }

    public DateTime? DateValue
    {
        get
        {
            if (IsMixed || Value is null) return null;
            if (Value is DateTime dt) return dt;
            if (Value is DateTimeOffset dto) return dto.DateTime;
            return null;
        }
        set
        {
            if (IsReadOnly || value is null) return;
            Engine?.TryCommit(this, value);
        }
    }

    public Color Swatch => Value is Color c ? c : Color.FromRgb(0x1E, 0x3A, 0x5F);

    public IList<VestigiumPropertyItem>? Children { get; internal set; }

    public ICommand AddCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand ToggleCommand { get; }
    public ICommand ResetCommand { get; }

    public VestigiumPropertyItem()
    {
        AddCommand = new RelayCommand(() => Engine?.TryAdd(this), () => Engine?.CanAdd(this) == true);
        RemoveCommand = new RelayCommand(() => Engine?.TryRemoveSelected(this), () => Engine?.CanRemove(this) == true);
        MoveUpCommand = new RelayCommand(() => Engine?.TryMove(this, -1), () => Engine?.CanMove(this, -1) == true);
        MoveDownCommand = new RelayCommand(() => Engine?.TryMove(this, 1), () => Engine?.CanMove(this, 1) == true);
        ToggleCommand = new RelayCommand(() => Engine?.ToggleExpand(this));
        ResetCommand = new RelayCommand(() => Engine?.TryReset(this), () => CanReset && Engine?.IsReadOnly == false);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void RefreshCommands()
    {
        (AddCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RemoveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (MoveUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (MoveDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ResetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(Display));
        OnPropertyChanged(nameof(CanReset));
        OnPropertyChanged(nameof(CanExpand));
        OnPropertyChanged(nameof(IsExpanded));
        OnPropertyChanged(nameof(IsMixed));
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged(string? name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
