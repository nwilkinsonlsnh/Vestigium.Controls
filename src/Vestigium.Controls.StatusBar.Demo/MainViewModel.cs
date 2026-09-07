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

        WindowBar.StartRuntime();
        IdleBar.StartRuntime();
        ProbeBar.StartRuntime();
        EditorBar.StartRuntime();
        SessionBar.StartRuntime();
        NestedInnerBar.StartRuntime();

        IdleText = StatusBarDefaults.IdleText;
        IdleTimeoutMs = StatusBarDefaults.IdleTimeoutMs;
        LabMessage = "Probe 8.8.8.8 — 12 ms";

        _clock = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _clock.Tick += (_, _) =>
        {
            OnPropertyChanged(nameof(IdleCountdown));
            OnPropertyChanged(nameof(SnapshotText));
        };
        _clock.Start();
    }

    public StatusBarEngine WindowBar { get; }
    public StatusBarEngine IdleBar { get; }
    public StatusBarEngine ProbeBar { get; }
    public StatusBarEngine EditorBar { get; }
    public StatusBarEngine SessionBar { get; }
    public StatusBarEngine NestedInnerBar { get; }

    [ObservableProperty] private string _idleText;
    [ObservableProperty] private int _idleTimeoutMs;
    [ObservableProperty] private string _labMessage;
    [ObservableProperty] private bool _idleEnabled = true;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private bool _isFlooding;

    public VestigiumStatusBarPosition Position
    {
        get => WindowBar.Position;
        set
        {
            if (WindowBar.Position == value) return;
            WindowBar.Position = value;
            OnPropertyChanged();
        }
    }

    public string IdleCountdown
    {
        get
        {
            var snap = WindowBar.Snapshot();
            var live = snap.Columns.FirstOrDefault(c => c.Key == "message");
            if (live is { IsIdle: true }) return live.IdleText;
            if (snap.IdleRemainingMs < 0) return "idle off";
            return $"idle {(snap.IdleRemainingMs / 1000.0):0.0}s";
        }
    }

    public string SnapshotText
    {
        get
        {
            var s = WindowBar.Snapshot();
            return $"Applied {s.Applied}  Compacted {s.Compacted}  Dup {s.DroppedDuplicate}  Drain {s.DrainIntervalMs}ms  IdleRem {s.IdleRemainingMs}";
        }
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
            WindowBar.PostImmediate("message", new StatusBarUpdate { Icon = icon });
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
    }

    [RelayCommand]
    private void RestoreIdleDefaults()
    {
        IdleText = StatusBarDefaults.IdleText;
        IdleTimeoutMs = StatusBarDefaults.IdleTimeoutMs;
        IdleEnabled = true;
        WindowBar.SetIdlePolicy(IdleTimeoutMs, IdleText);
    }
}
