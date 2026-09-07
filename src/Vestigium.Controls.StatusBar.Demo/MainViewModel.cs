using System.Text.Json;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Vestigium.Controls.StatusBar.Demo;

public partial class MainViewModel : ObservableObject
{
    private readonly DispatcherTimer _clock;

    public MainViewModel()
    {
        WindowBar = new StatusBarEngine();
        IdleBar = new StatusBarEngine();
        ProbeBar = new StatusBarEngine(StatusBarColumns.Probe());
        EditorBar = new StatusBarEngine(StatusBarColumns.Document());
        SessionBar = new StatusBarEngine(StatusBarColumns.Probe());
        NestedInnerBar = new StatusBarEngine();
        GroupsBar = new StatusBarEngine(StatusBarColumns.Groups());
        IconsBar = new StatusBarEngine(StatusBarColumns.Icons());
        TopSample = new StatusBarEngine(StatusBarColumns.Standard());
        BottomSample = new StatusBarEngine(StatusBarColumns.Standard());
        TopSample.Position = VestigiumStatusBarPosition.Top;
        BottomSample.Position = VestigiumStatusBarPosition.Bottom;

        foreach (var engine in AllEngines)
            engine.StartRuntime();

        IdleText = StatusBarDefaults.IdleText;
        IdleTimeoutMs = StatusBarDefaults.IdleTimeoutMs;
        LabMessage = "Probe 8.8.8.8 — 12 ms";
        SnapshotHint = "Counters are live. Use the buttons to generate traffic, then read what the pipeline did.";

        _clock = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _clock.Tick += (_, _) => RefreshLive();
        _clock.Start();
        RefreshLive();
    }

    public StatusBarEngine WindowBar { get; }
    public StatusBarEngine IdleBar { get; }
    public StatusBarEngine ProbeBar { get; }
    public StatusBarEngine EditorBar { get; }
    public StatusBarEngine SessionBar { get; }
    public StatusBarEngine NestedInnerBar { get; }
    public StatusBarEngine GroupsBar { get; }
    public StatusBarEngine IconsBar { get; }
    public StatusBarEngine TopSample { get; }
    public StatusBarEngine BottomSample { get; }

    private IEnumerable<StatusBarEngine> AllEngines =>
    [
        WindowBar, IdleBar, ProbeBar, EditorBar, SessionBar, NestedInnerBar,
        GroupsBar, IconsBar, TopSample, BottomSample
    ];

    [ObservableProperty] private string _idleText;
    [ObservableProperty] private int _idleTimeoutMs;
    [ObservableProperty] private string _labMessage;
    [ObservableProperty] private bool _idleEnabled = true;
    [ObservableProperty] private bool _isFlooding;
    [ObservableProperty] private string _snapshotJson = "{}";
    [ObservableProperty] private string _snapshotHint = "";
    [ObservableProperty] private string _moveTarget = "center";

    public VestigiumStatusBarPosition Position
    {
        get => WindowBar.Position;
        set
        {
            if (WindowBar.Position == value) return;
            WindowBar.Position = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDockedBottom));
            OnPropertyChanged(nameof(IsDockedTop));
            OnPropertyChanged(nameof(DockLabel));
        }
    }

    public bool IsDockedBottom
    {
        get => Position == VestigiumStatusBarPosition.Bottom;
        set { if (value) Position = VestigiumStatusBarPosition.Bottom; }
    }

    public bool IsDockedTop
    {
        get => Position == VestigiumStatusBarPosition.Top;
        set { if (value) Position = VestigiumStatusBarPosition.Top; }
    }

    public string DockLabel =>
        Position == VestigiumStatusBarPosition.Top
            ? "Window bar is under the menu (Top)"
            : "Window bar is at the foot of the window (Bottom)";

    public string IdleCountdown { get; private set; } = "idle 3.0s";
    public int Applied { get; private set; }
    public int Compacted { get; private set; }
    public int DroppedDuplicate { get; private set; }
    public int DroppedHighWater { get; private set; }
    public int DroppedMissing { get; private set; }
    public int DroppedDisposed { get; private set; }
    public int DrainMs { get; private set; }
    public int IdleRemaining { get; private set; }

    private void RefreshLive()
    {
        var snap = WindowBar.Snapshot();
        var live = snap.Columns.FirstOrDefault(c => c.Key == "message");
        IdleCountdown = live is { IsIdle: true }
            ? live.IdleText
            : snap.IdleRemainingMs < 0
                ? "idle off"
                : $"idle {(snap.IdleRemainingMs / 1000.0):0.0}s";
        Applied = snap.Applied;
        Compacted = snap.Compacted;
        DroppedDuplicate = snap.DroppedDuplicate;
        DroppedHighWater = snap.DroppedHighWater;
        DroppedMissing = snap.DroppedMissingColumn;
        DroppedDisposed = snap.DroppedDisposed;
        DrainMs = snap.DrainIntervalMs;
        IdleRemaining = snap.IdleRemainingMs;
        OnPropertyChanged(nameof(IdleCountdown));
        OnPropertyChanged(nameof(Applied));
        OnPropertyChanged(nameof(Compacted));
        OnPropertyChanged(nameof(DroppedDuplicate));
        OnPropertyChanged(nameof(DroppedHighWater));
        OnPropertyChanged(nameof(DroppedMissing));
        OnPropertyChanged(nameof(DroppedDisposed));
        OnPropertyChanged(nameof(DrainMs));
        OnPropertyChanged(nameof(IdleRemaining));
    }

    partial void OnIdleTextChanged(string value) =>
        WindowBar.SetIdlePolicy(IdleEnabled ? IdleTimeoutMs : 0, value);

    partial void OnIdleTimeoutMsChanged(int value) =>
        WindowBar.SetIdlePolicy(IdleEnabled ? value : 0, IdleText);

    partial void OnIdleEnabledChanged(bool value) =>
        WindowBar.SetIdlePolicy(value ? IdleTimeoutMs : 0, IdleText);

    [RelayCommand]
    private void DockBottom() => Position = VestigiumStatusBarPosition.Bottom;

    [RelayCommand]
    private void DockTop() => Position = VestigiumStatusBarPosition.Top;

    [RelayCommand]
    private void PostLab() =>
        WindowBar.Post("message", new StatusBarUpdate { Text = LabMessage });

    [RelayCommand]
    private void PostLabImmediate() =>
        WindowBar.PostImmediate("message", new StatusBarUpdate { Text = LabMessage, Icon = StatusBarIconKind.Info });

    [RelayCommand]
    private void Ready() =>
        WindowBar.PostImmediate("message", new StatusBarUpdate { Text = StatusBarDefaults.ReadyText, Icon = StatusBarIconKind.None });

    [RelayCommand]
    private void SetIcon(string? kind)
    {
        if (Enum.TryParse<StatusBarIconKind>(kind, out var icon))
        {
            WindowBar.PostImmediate("message", new StatusBarUpdate { Icon = icon, Text = $"{icon} on the live region" });
            IconsBar.PostImmediate("message", new StatusBarUpdate { Text = $"{icon} on channel A", Icon = icon });
        }
    }

    [RelayCommand]
    private void TouchIdle() =>
        IdleBar.PostImmediate("message", new StatusBarUpdate { Text = "Touched — timer reset", Icon = StatusBarIconKind.Info });

    [RelayCommand]
    private void RunPing()
    {
        ProbeBar.PostImmediate("message", new StatusBarUpdate { Text = "Pinging 1.1.1.1", Icon = StatusBarIconKind.Busy });
        ProbeBar.PostImmediate("progress", new StatusBarUpdate { Progress = 0, IsProgressVisible = true, IsIndeterminate = false });
        var i = 0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        timer.Tick += (_, _) =>
        {
            i += 12;
            if (i >= 100)
            {
                timer.Stop();
                ProbeBar.PostImmediate("progress", new StatusBarUpdate { Progress = 100, IsProgressVisible = true });
                ProbeBar.PostImmediate("message", new StatusBarUpdate { Text = "Reply from 1.1.1.1", Icon = StatusBarIconKind.Success });
                ProbeBar.PostImmediate("rtt", new StatusBarUpdate { Text = "11 ms" });
                return;
            }
            ProbeBar.Post("progress", new StatusBarUpdate { Progress = i, IsProgressVisible = true });
        };
        timer.Start();
    }

    [RelayCommand]
    private void IndeterminateProbe() =>
        ProbeBar.PostImmediate("progress", new StatusBarUpdate { IsIndeterminate = true, IsProgressVisible = true });

    [RelayCommand]
    private void BuildEditor() =>
        EditorBar.PostImmediate("message", new StatusBarUpdate { Text = "Build succeeded", Icon = StatusBarIconKind.Success });

    [RelayCommand]
    private void WarnEditor() =>
        EditorBar.PostImmediate("message", new StatusBarUpdate { Text = "3 warnings", Icon = StatusBarIconKind.Warning });

    [RelayCommand]
    private void TraceSession()
    {
        SessionBar.PostImmediate("message", new StatusBarUpdate { Text = "Tracing hops", Icon = StatusBarIconKind.Busy });
        SessionBar.PostImmediate("progress", new StatusBarUpdate { IsIndeterminate = true, IsProgressVisible = true });
    }

    [RelayCommand]
    private void CompleteSession()
    {
        SessionBar.PostImmediate("progress", new StatusBarUpdate { IsIndeterminate = false, Progress = 100, IsProgressVisible = true });
        SessionBar.PostImmediate("message", new StatusBarUpdate { Text = "Hop 12 reached", Icon = StatusBarIconKind.Success });
        SessionBar.PostImmediate("rtt", new StatusBarUpdate { Text = "84 ms" });
    }

    [RelayCommand]
    private void WriteOutput() =>
        NestedInnerBar.PostImmediate("message", new StatusBarUpdate { Text = "Output flushed", Icon = StatusBarIconKind.Info });

    [RelayCommand]
    private async Task FloodAsync()
    {
        if (IsFlooding) return;
        IsFlooding = true;
        SnapshotHint = "Flood: 100 posts at 8 ms. Watch Compacted rise and Applied stay far below 100.";
        WindowBar.PostImmediate("message", new StatusBarUpdate { Text = "Flooding worker posts…", Icon = StatusBarIconKind.Busy });
        WindowBar.PostImmediate("progress", new StatusBarUpdate { Progress = 0, IsProgressVisible = true, IsIndeterminate = false });
        for (var i = 1; i <= 100; i++)
        {
            WindowBar.Post("progress", new StatusBarUpdate { Progress = i, IsProgressVisible = true });
            WindowBar.Post("message", new StatusBarUpdate { Text = $"Probe hop {i}/100" });
            await Task.Delay(8);
        }
        WindowBar.PostImmediate("progress", new StatusBarUpdate { Progress = 100, IsProgressVisible = true });
        WindowBar.PostImmediate("message", new StatusBarUpdate { Text = "Probe complete", Icon = StatusBarIconKind.Success });
        IsFlooding = false;
        CaptureSnapshot();
    }

    [RelayCommand]
    private void RestoreIdleDefaults()
    {
        IdleText = StatusBarDefaults.IdleText;
        IdleTimeoutMs = StatusBarDefaults.IdleTimeoutMs;
        IdleEnabled = true;
        WindowBar.SetIdlePolicy(IdleTimeoutMs, IdleText);
    }

    [RelayCommand]
    private void SelectTarget(string? key)
    {
        if (!string.IsNullOrWhiteSpace(key))
            MoveTarget = key;
    }

    [RelayCommand]
    private void MoveToLeft() => MoveSelected(StatusBarSlot.Left);

    [RelayCommand]
    private void MoveToCenter() => MoveSelected(StatusBarSlot.Center);

    [RelayCommand]
    private void MoveToRight() => MoveSelected(StatusBarSlot.Right);

    private void MoveSelected(StatusBarSlot slot)
    {
        var column = GroupsBar.Columns.FirstOrDefault(c => c.Key == MoveTarget);
        if (column is null) return;
        column.Slot = slot;
        GroupsBar.PostImmediate(column.Key ?? MoveTarget, new StatusBarUpdate { Text = $"{slot}" });
    }

    [RelayCommand]
    private void PostDuplicate()
    {
        SnapshotHint = "Duplicate: two identical Immediate posts. DroppedDuplicate should bump by 1.";
        WindowBar.PostImmediate("message", new StatusBarUpdate { Text = "Same payload" });
        WindowBar.PostImmediate("message", new StatusBarUpdate { Text = "Same payload" });
        CaptureSnapshot();
    }

    [RelayCommand]
    private void PostMissingKey()
    {
        SnapshotHint = "Missing key: Post to a column that does not exist. DroppedMissing increments. The bar does not throw.";
        WindowBar.Post("no-such-column", new StatusBarUpdate { Text = "ghost" });
        CaptureSnapshot();
    }

    [RelayCommand]
    private void CaptureSnapshot()
    {
        var snap = WindowBar.Snapshot();
        SnapshotJson = JsonSerializer.Serialize(snap, new JsonSerializerOptions { WriteIndented = true });
        RefreshLive();
    }
}
