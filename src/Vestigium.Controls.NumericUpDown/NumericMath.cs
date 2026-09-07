namespace Vestigium.Controls.NumericUpDown;

public static class NumericMath
{
    public static decimal NormalizeIncrement(decimal increment) =>
        increment > 0 ? increment : NumericDefaults.Increment;

    public static int ClampPlaces(int places) => Math.Clamp(places, 0, 28);

    public static decimal Clamp(decimal value, decimal min, decimal max)
    {
        if (min > max)
            (min, max) = (max, min);
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static decimal RoundPlaces(decimal value, int places) =>
        Math.Round(value, ClampPlaces(places), MidpointRounding.AwayFromZero);

    public static bool OnGrid(decimal value, decimal increment, decimal snapBase)
    {
        increment = NormalizeIncrement(increment);
        var n = (value - snapBase) / increment;
        return n == decimal.Truncate(n);
    }

    public static decimal Snap(decimal value, decimal increment, decimal snapBase, VestigiumNumericSnapMode mode)
    {
        increment = NormalizeIncrement(increment);
        var n = (value - snapBase) / increment;
        var idx = mode switch
        {
            VestigiumNumericSnapMode.Floor => Math.Floor(n),
            VestigiumNumericSnapMode.Ceiling => Math.Ceiling(n),
            _ => Math.Round(n, MidpointRounding.AwayFromZero)
        };
        return snapBase + idx * increment;
    }

    public static decimal NextGrid(decimal from, decimal increment, decimal snapBase, int direction, decimal stepMagnitude)
    {
        increment = NormalizeIncrement(increment);
        direction = Math.Sign(direction);
        if (direction == 0) return from;
        var unitSteps = (int)Math.Max(1m, Math.Round(Math.Abs(stepMagnitude) / increment, MidpointRounding.AwayFromZero));
        var n = (from - snapBase) / increment;
        decimal nextIndex;
        if (n == decimal.Truncate(n))
            nextIndex = n + direction * unitSteps;
        else
            nextIndex = (direction > 0 ? Math.Ceiling(n) : Math.Floor(n)) + direction * (unitSteps - 1);
        return snapBase + nextIndex * increment;
    }
}
