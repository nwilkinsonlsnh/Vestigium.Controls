using System.Globalization;

namespace Vestigium.Controls.NumericUpDown;

public sealed class NumericEngine
{
    private decimal _minimum = decimal.MinValue;
    private decimal _maximum = decimal.MaxValue;
    private decimal _increment = NumericDefaults.Increment;
    private decimal _pageIncrement = NumericDefaults.PageIncrement;
    private int _decimalPlaces = NumericDefaults.DecimalPlaces;
    private string _formatString = NumericDefaults.FormatString;
    private long _holdStartedMs = -1;
    private bool _pendingImmediate;

    public decimal Value { get; private set; }
    public decimal DisplayValue { get; private set; }
    public int Commits { get; private set; }
    public int DisplayTicks { get; private set; }
    public bool IsHolding { get; private set; }

    public VestigiumNumericUpdateMode UpdateMode { get; set; } = VestigiumNumericUpdateMode.Immediate;
    public VestigiumNumericCommitMode CommitMode { get; set; } = VestigiumNumericCommitMode.Auto;
    public VestigiumNumericInputMode InputMode { get; set; } = VestigiumNumericInputMode.Full;
    public VestigiumNumericSnapMode SnapMode { get; set; } = VestigiumNumericSnapMode.Round;
    public bool SnapToIncrement { get; set; }
    public decimal SnapBase { get; set; }
    public int AccelerationDelayMs { get; set; } = NumericDefaults.AccelerationDelayMs;
    public bool HasPendingImmediate => _pendingImmediate;

    public decimal Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_minimum > _maximum)
                _maximum = _minimum;
            ApplyCommitted(Value, count: false);
        }
    }

    public decimal Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            if (_maximum < _minimum)
                _minimum = _maximum;
            ApplyCommitted(Value, count: false);
        }
    }

    public decimal Increment
    {
        get => _increment;
        set => _increment = NumericMath.NormalizeIncrement(value);
    }

    public decimal PageIncrement
    {
        get => _pageIncrement;
        set => _pageIncrement = value > 0 ? value : NumericDefaults.PageIncrement;
    }

    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set
        {
            _decimalPlaces = NumericMath.ClampPlaces(value);
            ApplyCommitted(Value, count: false);
        }
    }

    public string FormatString
    {
        get => _formatString;
        set => _formatString = string.IsNullOrWhiteSpace(value) ? NumericDefaults.FormatString : value;
    }

    public bool CanStepUp => CanStep(1);
    public bool CanStepDown => CanStep(-1);
    public bool CanSpin => InputMode != VestigiumNumericInputMode.ReadOnly;
    public bool CanType => InputMode == VestigiumNumericInputMode.Full;

    public string FormattedDisplay => Format(DisplayValue);

    public bool IsAccelerated
    {
        get
        {
            if (!IsHolding || AccelerationDelayMs <= 0 || _holdStartedMs < 0)
                return false;
            return NowMs() - _holdStartedMs >= AccelerationDelayMs;
        }
    }

    public event Action? Changed;

    public string Format(decimal value)
    {
        try
        {
            return value.ToString(FormatString, CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }
    }

    public decimal Finalize(decimal raw)
    {
        var v = raw;
        if (SnapToIncrement)
            v = NumericMath.Snap(v, Increment, SnapBase, SnapMode);
        v = NumericMath.RoundPlaces(v, DecimalPlaces);
        return NumericMath.Clamp(v, Minimum, Maximum);
    }

    public void SetCommitted(decimal value) => ApplyCommitted(value, count: false);

    public void BeginHold()
    {
        if (!CanSpin) return;
        IsHolding = true;
        _holdStartedMs = NowMs();
        Raise();
    }

    public void EndHold()
    {
        if (!IsHolding && !_pendingImmediate)
        {
            CommitDisplay();
            return;
        }

        IsHolding = false;
        _holdStartedMs = -1;
        CommitDisplay();
    }

    public void CancelHold()
    {
        IsHolding = false;
        _holdStartedMs = -1;
        _pendingImmediate = false;
        Revert();
    }

    public void Step(int direction, bool page)
    {
        if (!CanSpin) return;
        direction = Math.Sign(direction);
        if (direction == 0) return;
        var step = page || IsAccelerated ? PageIncrement : Increment;
        decimal next;
        if (SnapToIncrement)
            next = NumericMath.NextGrid(DisplayValue, Increment, SnapBase, direction, step);
        else
            next = DisplayValue + direction * step;
        SetDisplay(Finalize(next));
    }

    public bool TryCommitText(string? text, bool isExplicit)
    {
        if (!CanType)
        {
            Revert();
            return false;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            Revert();
            return true;
        }

        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed))
        {
            Revert();
            return false;
        }

        var final = Finalize(parsed);
        if (CommitMode == VestigiumNumericCommitMode.Explicit && !isExplicit)
        {
            Revert();
            return false;
        }

        Commit(final);
        return true;
    }

    public void Revert()
    {
        DisplayValue = Value;
        _pendingImmediate = false;
        Raise();
    }

    public void CommitDisplay() => Commit(DisplayValue);

    public bool FlushImmediate()
    {
        if (!_pendingImmediate) return false;
        if (UpdateMode != VestigiumNumericUpdateMode.Immediate) return false;
        var before = Value;
        Commit(DisplayValue);
        return Value != before;
    }

    private void SetDisplay(decimal value)
    {
        DisplayValue = value;
        DisplayTicks++;
        if (UpdateMode == VestigiumNumericUpdateMode.Immediate)
            _pendingImmediate = true;
        Raise();
    }

    private void Commit(decimal value)
    {
        var final = Finalize(value);
        DisplayValue = final;
        _pendingImmediate = false;
        if (final == Value)
        {
            Raise();
            return;
        }

        Value = final;
        Commits++;
        Raise();
    }

    private void ApplyCommitted(decimal value, bool count)
    {
        var final = Finalize(value);
        DisplayValue = final;
        _pendingImmediate = false;
        if (final != Value)
        {
            Value = final;
            if (count) Commits++;
        }
        Raise();
    }

    private bool CanStep(int direction)
    {
        if (!CanSpin) return false;
        var step = Increment;
        decimal next;
        if (SnapToIncrement)
            next = NumericMath.NextGrid(DisplayValue, Increment, SnapBase, direction, step);
        else
            next = DisplayValue + direction * step;
        return Finalize(next) != DisplayValue;
    }

    private void Raise() => Changed?.Invoke();

    private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
