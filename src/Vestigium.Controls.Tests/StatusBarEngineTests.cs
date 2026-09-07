using Vestigium.Controls.StatusBar;

namespace Vestigium.Controls.Tests;

public class StatusBarEngineTests : IDisposable
{
    private readonly List<StatusBarEngine> _engines = [];

    private StatusBarEngine Make(Func<long>? now = null)
    {
        var engine = new StatusBarEngine(StatusBarColumns.Standard(), now ?? (() => 1_000_000));
        _engines.Add(engine);
        return engine;
    }

    public void Dispose()
    {
        foreach (var engine in _engines)
            engine.Dispose();
    }

    [Fact]
    public void Standard_columns_use_left_center_right_slots()
    {
        var engine = Make();
        Assert.Equal(StatusBarSlot.Left, engine.Columns.First(c => c.Key == "message").Slot);
        Assert.Equal(StatusBarSlot.Center, engine.Columns.First(c => c.Key == "progress").Slot);
        Assert.Equal(StatusBarSlot.Right, engine.Columns.First(c => c.Key == "clock").Slot);
    }


    [Fact]
    public void Message_column_defaults_to_three_second_idle()
    {
        var engine = Make();
        var message = engine.Columns.First(c => c.Key == "message");
        Assert.Equal(StatusBarDefaults.IdleTimeoutMs, message.IdleTimeoutMs);
        Assert.Equal(StatusBarDefaults.IdleText, message.IdleText);
    }

    [Fact]
    public void Live_region_whitespace_displays_Ready()
    {
        var engine = Make();
        engine.PostImmediate(0, new StatusBarUpdate { Text = "   " });
        Assert.Equal(StatusBarDefaults.ReadyText, engine.Columns[0].Text);
    }

    [Fact]
    public void Progress_coerces_to_0_100()
    {
        var engine = Make();
        engine.PostImmediate(1, new StatusBarUpdate { Progress = -10 });
        Assert.Equal(0, engine.Columns[1].Progress);
        engine.PostImmediate(1, new StatusBarUpdate { Progress = 140 });
        Assert.Equal(100, engine.Columns[1].Progress);
    }

    [Fact]
    public void Duplicate_of_last_applied_is_dropped()
    {
        var engine = Make();
        engine.PostImmediate(0, new StatusBarUpdate { Text = "Probe done" });
        var applied = engine.Applied;
        engine.PostImmediate(0, new StatusBarUpdate { Text = "Probe done" });
        Assert.Equal(applied, engine.Applied);
        Assert.Equal(1, engine.DroppedDuplicate);
    }

    [Fact]
    public void Missing_key_increments_DroppedMissingColumn()
    {
        var engine = Make();
        engine.Post("nope", new StatusBarUpdate { Text = "x" });
        Assert.Equal(1, engine.DroppedMissingColumn);
    }

    [Fact]
    public void Post_after_dispose_increments_DroppedDisposed()
    {
        var engine = Make();
        engine.Dispose();
        engine.Post(0, new StatusBarUpdate { Text = "late" });
        Assert.Equal(1, engine.DroppedDisposed);
    }

    [Fact]
    public void High_water_does_not_wipe_other_columns()
    {
        var engine = Make();
        engine.Post(0, new StatusBarUpdate { Text = "keep-me" });
        for (var i = 0; i < StatusBarDefaults.InboundHighWater + 8; i++)
            engine.Post(1, new StatusBarUpdate { Progress = i % 100 });

        engine.PostImmediate(0, new StatusBarUpdate { Text = "keep-me" });
        Assert.Equal("keep-me", engine.Columns[0].Text);
        Assert.True(engine.DroppedHighWater > 0);
    }

    [Fact]
    public void Worker_flood_coalesces_without_throwing()
    {
        var engine = Make();
        engine.StartRuntime();
        Parallel.For(0, 4_000, i =>
        {
            engine.Post("message", new StatusBarUpdate { Text = $"hop {i}" });
            engine.Post("progress", new StatusBarUpdate { Progress = i % 101, IsProgressVisible = true });
        });
        engine.PostImmediate("message", new StatusBarUpdate { Text = "done" });
        Assert.Equal("done", engine.Columns[0].Text);
        Assert.True(engine.Compacted > 0);
        Assert.True(engine.Applied >= 1);
    }

    [Fact]
    public async Task Idle_rewrites_text_after_timeout()
    {
        var engine = new StatusBarEngine();
        _engines.Add(engine);
        engine.StartRuntime();
        engine.SetIdlePolicy(40, StatusBarDefaults.IdleText);
        engine.PostImmediate("message", new StatusBarUpdate { Text = "Working" });
        await Task.Delay(250);
        Assert.Equal(StatusBarDefaults.IdleText, engine.Columns[0].Text);
        Assert.True(engine.Columns[0].IsIdle);
    }
}

