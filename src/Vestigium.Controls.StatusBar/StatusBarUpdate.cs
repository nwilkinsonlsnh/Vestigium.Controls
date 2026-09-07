namespace Vestigium.Controls.StatusBar;

public sealed class StatusBarUpdate
{
    public string? Text { get; init; }
    public double? Progress { get; init; }
    public bool? IsIndeterminate { get; init; }
    public bool? IsProgressVisible { get; init; }
    public StatusBarIconKind? Icon { get; init; }
    public bool Immediate { get; init; }
}
