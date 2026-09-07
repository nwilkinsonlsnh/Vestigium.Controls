using Vestigium.Controls.NumericUpDown;

namespace Vestigium.Controls.Tests;

public class NumericEngineTests
{
    [Fact]
    public void Default_value_is_zero_and_range_is_unbounded()
    {
        var engine = new NumericEngine();
        Assert.Equal(0m, engine.Value);
        Assert.Equal(decimal.MinValue, engine.Minimum);
        Assert.Equal(decimal.MaxValue, engine.Maximum);
    }

    [Fact]
    public void Binding_over_max_clamps()
    {
        var engine = new NumericEngine { Maximum = 50 };
        engine.SetCommitted(999);
        Assert.Equal(50m, engine.Value);
    }

    [Fact]
    public void Increment_not_positive_becomes_one()
    {
        var engine = new NumericEngine { Increment = 0 };
        Assert.Equal(1m, engine.Increment);
        engine.Increment = -4;
        Assert.Equal(1m, engine.Increment);
    }

    [Fact]
    public void Empty_commit_reverts_display()
    {
        var engine = new NumericEngine();
        engine.SetCommitted(8);
        engine.TryCommitText("   ", isExplicit: true);
        Assert.Equal(8m, engine.Value);
        Assert.Equal(8m, engine.DisplayValue);
    }

    [Fact]
    public void Deferred_step_does_not_change_value_until_end_hold()
    {
        var engine = new NumericEngine { UpdateMode = VestigiumNumericUpdateMode.Deferred };
        engine.SetCommitted(8);
        engine.BeginHold();
        engine.Step(1, false);
        engine.Step(1, false);
        Assert.Equal(8m, engine.Value);
        Assert.Equal(10m, engine.DisplayValue);
        engine.EndHold();
        Assert.Equal(10m, engine.Value);
        Assert.Equal(1, engine.Commits);
    }

    [Fact]
    public void Immediate_flush_copies_display_to_value()
    {
        var engine = new NumericEngine { UpdateMode = VestigiumNumericUpdateMode.Immediate };
        engine.SetCommitted(1);
        engine.Step(1, false);
        Assert.True(engine.HasPendingImmediate);
        Assert.Equal(1m, engine.Value);
        Assert.True(engine.FlushImmediate());
        Assert.Equal(2m, engine.Value);
        Assert.False(engine.HasPendingImmediate);
    }

    [Fact]
    public void Snap_round_eleven_to_ten()
    {
        var engine = new NumericEngine
        {
            SnapToIncrement = true,
            SnapMode = VestigiumNumericSnapMode.Round,
            Increment = 5
        };
        engine.TryCommitText("11", isExplicit: true);
        Assert.Equal(10m, engine.Value);
    }

    [Fact]
    public void Snap_up_from_seven_goes_to_ten()
    {
        var engine = new NumericEngine
        {
            SnapToIncrement = true,
            Increment = 5
        };
        engine.SetCommitted(7);
        engine.Step(1, false);
        engine.CommitDisplay();
        Assert.Equal(10m, engine.Value);
    }

    [Fact]
    public void Explicit_lost_focus_reverts_typed_draft()
    {
        var engine = new NumericEngine
        {
            CommitMode = VestigiumNumericCommitMode.Explicit
        };
        engine.SetCommitted(4);
        engine.TryCommitText("99", isExplicit: false);
        Assert.Equal(4m, engine.Value);
        Assert.Equal(4m, engine.DisplayValue);
    }

    [Fact]
    public void Auto_lost_focus_commits_typed_draft()
    {
        var engine = new NumericEngine
        {
            CommitMode = VestigiumNumericCommitMode.Auto
        };
        engine.SetCommitted(4);
        engine.TryCommitText("99", isExplicit: false);
        Assert.Equal(99m, engine.Value);
    }

    [Fact]
    public void Decimal_places_round_on_commit()
    {
        var engine = new NumericEngine { DecimalPlaces = 2, FormatString = "N2" };
        engine.TryCommitText("1.239", isExplicit: true);
        Assert.Equal(1.24m, engine.Value);
    }

    [Fact]
    public void ReadOnly_step_is_noop()
    {
        var engine = new NumericEngine { InputMode = VestigiumNumericInputMode.ReadOnly };
        engine.SetCommitted(3);
        engine.Step(1, false);
        Assert.Equal(3m, engine.Value);
        Assert.Equal(3m, engine.DisplayValue);
    }

    [Fact]
    public void Min_greater_than_max_last_write_wins()
    {
        var engine = new NumericEngine();
        engine.Maximum = 10;
        engine.Minimum = 20;
        Assert.Equal(20m, engine.Minimum);
        Assert.Equal(20m, engine.Maximum);
    }

    [Fact]
    public void Cancel_hold_restores_value()
    {
        var engine = new NumericEngine { UpdateMode = VestigiumNumericUpdateMode.Deferred };
        engine.SetCommitted(5);
        engine.BeginHold();
        engine.Step(1, false);
        engine.CancelHold();
        Assert.Equal(5m, engine.Value);
        Assert.Equal(5m, engine.DisplayValue);
    }
}
