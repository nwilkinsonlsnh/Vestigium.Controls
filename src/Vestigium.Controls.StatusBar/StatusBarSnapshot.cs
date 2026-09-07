namespace Vestigium.Controls.StatusBar;

public sealed class StatusBarSnapshot
{
    public required IReadOnlyList<StatusBarColumnSnapshot> Columns { get; init; }
    public VestigiumStatusBarPosition Position { get; init; }
    public double BarThickness { get; init; }
    public int DroppedDuplicate { get; init; }
    public int DroppedHighWater { get; init; }
    public int DroppedDisposed { get; init; }
    public int DroppedMissingColumn { get; init; }
    public int Compacted { get; init; }
    public int Applied { get; init; }
    public int DrainIntervalMs { get; init; }
    public int IdleRemainingMs { get; init; }
}

public sealed class StatusBarColumnSnapshot
{
    public string? Key { get; init; }
    public StatusBarColumnKind Kind { get; init; }
    public string Text { get; init; } = string.Empty;
    public double Progress { get; init; }
    public bool IsIndeterminate { get; init; }
    public bool IsProgressVisible { get; init; }
    public StatusBarIconKind Icon { get; init; }
    public bool IsIdle { get; init; }
    public string IdleText { get; init; } = StatusBarDefaults.IdleText;
    public int IdleTimeoutMs { get; init; }
}
