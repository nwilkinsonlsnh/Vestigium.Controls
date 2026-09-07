namespace Vestigium.Controls.StatusBar;

public enum StatusBarColumnKind
{
    Text = 0,
    Progress = 1,
    Clock = 2,
    Icon = 3,
    Empty = 4
}

public enum StatusBarColumnWidth
{
    Auto = 0,
    Star = 1,
    Fixed = 2
}

public enum StatusBarIconKind
{
    None = 0,
    Info = 1,
    Success = 2,
    Warning = 3,
    Error = 4,
    Busy = 5
}

public static class StatusBarDefaults
{
    public const string ReadyText = "Ready";
    public const string IdleText = "Idle. . .";
    public const int IdleTimeoutMs = 3000;
    public const double BarThickness = 28;
    public const double MinBarThickness = 20;
    public const double MaxBarThickness = 64;
    public const int DrainFloorMs = 100;
    public const int DrainCeilingMs = 250;
    public const int DrainIncreaseMs = 25;
    public const double ProgressEpsilon = 0.5;
    public const int ProgressTtlMs = 500;
    public const int InboundHighWater = 256;
}
