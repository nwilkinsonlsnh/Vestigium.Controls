namespace Vestigium.Controls.NumericUpDown;

public enum VestigiumNumericUpdateMode
{
    Immediate = 0,
    Deferred = 1
}

public enum VestigiumNumericCommitMode
{
    Auto = 0,
    Explicit = 1
}

public enum VestigiumNumericInputMode
{
    Full = 0,
    SpinOnly = 1,
    ReadOnly = 2
}

public enum VestigiumNumericSnapMode
{
    Round = 0,
    Floor = 1,
    Ceiling = 2
}

public static class NumericDefaults
{
    public const decimal Value = 0;
    public const decimal Increment = 1;
    public const decimal PageIncrement = 10;
    public const int DecimalPlaces = 0;
    public const string FormatString = "N0";
    public const int DelayMs = 400;
    public const int IntervalMs = 33;
    public const int AccelerationDelayMs = 2000;
    public const int DrainFloorMs = 50;
    public const int DrainCeilingMs = 250;
    public const int DrainIncreaseMs = 25;
}
