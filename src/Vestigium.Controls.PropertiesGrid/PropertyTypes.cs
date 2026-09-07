namespace Vestigium.Controls.PropertiesGrid;

public enum VestigiumPropertySort
{
    Categorized = 0,
    Alphabetical = 1
}

public enum VestigiumPropertyEditorKind
{
    Text = 0,
    Boolean = 1,
    Enum = 2,
    Numeric = 3,
    DateTime = 4,
    Color = 5,
    ReadOnly = 6,
    Expandable = 7,
    Collection = 8
}

public sealed class VestigiumPropertyValueChangedEventArgs : EventArgs
{
    public VestigiumPropertyValueChangedEventArgs(string path, object? oldValue, object? newValue, object? target)
    {
        Path = path;
        OldValue = oldValue;
        NewValue = newValue;
        Target = target;
    }

    public string Path { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
    public object? Target { get; }
}
