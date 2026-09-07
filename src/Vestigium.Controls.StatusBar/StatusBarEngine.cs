using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Vestigium.Controls.StatusBar;

/// <summary>
/// Flood pipeline. Workers may Post from any thread. Column objects and
/// INPC exist only on the dispatcher. One pending record per column is the
/// cancellation mechanism — there is no consumer-facing CTS.
/// </summary>
public sealed class StatusBarEngine : ObservableObject, IUpdateStatusBar, IDisposable
{
    private readonly Func<long> _nowMs;
    private readonly object _gate = new();
    private readonly Dictionary<int, StatusBarUpdate> _pending = [];
    private readonly Dictionary<int, long> _lastAppliedAt = [];

    private Dispatcher? _dispatcher;
    private DispatcherTimer? _uiDrainTimer;
    private DispatcherTimer? _uiClockTimer;
    private Timer? _threadDrainTimer;
    private Timer? _threadClockTimer;
    private bool _disposed;
    private bool _drainRunning;
    private bool _immediateQueued;
    private bool _drainQueued;
    private int _inboundSinceFlush;

    public StatusBarEngine()
        : this(StatusBarColumns.Standard(), () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
    {
    }

    public StatusBarEngine(IEnumerable<StatusBarColumn> columns, Func<long>? nowMs = null)
    {
        _nowMs = nowMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Columns = new ObservableCollection<StatusBarColumn>(columns);
        var stamped = _nowMs();
        for (var i = 0; i < Columns.Count; i++)
            _lastAppliedAt[i] = stamped;
        DrainIntervalMs = StatusBarDefaults.DrainFloorMs;
        _barThickness = StatusBarDefaults.BarThickness;
    }

    public ObservableCollection<StatusBarColumn> Columns { get; }

    private VestigiumStatusBarPosition _position = VestigiumStatusBarPosition.Bottom;
    private double _barThickness;

    public VestigiumStatusBarPosition Position
    {
        get => _position;
        set
        {
            if (SetProperty(ref _position, value))
                Changed?.Invoke();
        }
    }

    public double BarThickness
    {
        get => _barThickness;
        set
        {
            var next = Math.Clamp(value, StatusBarDefaults.MinBarThickness, StatusBarDefaults.MaxBarThickness);
            if (SetProperty(ref _barThickness, next))
                Changed?.Invoke();
        }
    }

    public int DrainIntervalMs { get; private set; }
    public int DroppedDuplicate { get; private set; }
    public int DroppedHighWater { get; private set; }
    public int DroppedDisposed { get; private set; }
    public int DroppedMissingColumn { get; private set; }
    public int Compacted { get; private set; }
    public int Applied { get; private set; }

    public event Action? Changed;

    public void StartRuntime(Dispatcher? dispatcher = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _dispatcher ??= dispatcher ?? Dispatcher.FromThread(Thread.CurrentThread);

        if (_dispatcher is not null)
        {
            if (_uiDrainTimer is null)
            {
                _uiDrainTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
                {
                    Interval = TimeSpan.FromMilliseconds(DrainIntervalMs)
                };
                _uiDrainTimer.Tick += (_, _) => DrainTick();
                _uiDrainTimer.Start();
            }

            if (_uiClockTimer is null)
            {
                _uiClockTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _uiClockTimer.Tick += (_, _) =>
                {
                    TickClock();
                    Notify();
                };
                _uiClockTimer.Start();
            }
        }
        else
        {
            _threadDrainTimer ??= new Timer(_ => DrainTick(), null, DrainIntervalMs, Timeout.Infinite);
            _threadClockTimer ??= new Timer(_ =>
            {
                TickClock();
                Notify();
            }, null, 1000, 1000);
        }

        TickClock();
    }

    public void Post(int index, StatusBarUpdate update) => Enqueue(index, update, immediate: false);

    public void Post(string key, StatusBarUpdate update) => Enqueue(IndexOfKey(key), update, immediate: false);

    public void PostImmediate(int index, StatusBarUpdate update) => Enqueue(index, update, immediate: true);

    public void PostImmediate(string key, StatusBarUpdate update) => Enqueue(IndexOfKey(key), update, immediate: true);

    public void SetIdlePolicy(int idleTimeoutMs, string? idleText = null)
    {
        if (_disposed) return;
        RunOnDispatcher(() => ApplyIdlePolicy(idleTimeoutMs, idleText), immediate: true);
    }

    public StatusBarSnapshot Snapshot()
    {
        lock (_gate)
        {
            return BuildSnapshot();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _uiDrainTimer?.Stop();
        _uiClockTimer?.Stop();
        _threadDrainTimer?.Dispose();
        _threadClockTimer?.Dispose();
        _uiDrainTimer = null;
        _uiClockTimer = null;
        _threadDrainTimer = null;
        _threadClockTimer = null;
        lock (_gate)
            _pending.Clear();
    }

    private int IndexOfKey(string key)
    {
        var found = -1;
        for (var i = 0; i < Columns.Count; i++)
        {
            if (Columns[i].Key == key)
                found = i;
        }
        return found;
    }

    private void Enqueue(int index, StatusBarUpdate update, bool immediate)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                DroppedDisposed++;
                return;
            }

            if (index < 0 || index >= Columns.Count)
            {
                DroppedMissingColumn++;
                return;
            }

            _inboundSinceFlush++;
            if (_inboundSinceFlush > StatusBarDefaults.InboundHighWater)
                DroppedHighWater++;

            if (_pending.ContainsKey(index))
                Compacted++;

            if (_pending.TryGetValue(index, out var existing))
                update = Merge(existing, update);

            if (immediate)
                update = Merge(update, new StatusBarUpdate { Immediate = true });

            _pending[index] = update;
        }

        RequestDrain(immediate);
    }

    private static StatusBarUpdate Merge(StatusBarUpdate a, StatusBarUpdate b) => new()
    {
        Text = b.Text ?? a.Text,
        Progress = b.Progress ?? a.Progress,
        IsIndeterminate = b.IsIndeterminate ?? a.IsIndeterminate,
        IsProgressVisible = b.IsProgressVisible ?? a.IsProgressVisible,
        Icon = b.Icon ?? a.Icon,
        Immediate = b.Immediate || a.Immediate
    };

    private void RequestDrain(bool immediate)
    {
        if (_disposed) return;

        if (!immediate)
        {
            DrainIntervalMs = Math.Max(StatusBarDefaults.DrainFloorMs, DrainIntervalMs / 2);
            if (_uiDrainTimer is not null)
                _uiDrainTimer.Interval = TimeSpan.FromMilliseconds(DrainIntervalMs);
        }

        var dispatcher = _dispatcher;
        if (dispatcher is not null)
        {
            if (immediate)
            {
                if (dispatcher.CheckAccess())
                {
                    DrainTick();
                    return;
                }

                if (_immediateQueued) return;
                _immediateQueued = true;
                dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
                {
                    _immediateQueued = false;
                    DrainTick();
                });
                return;
            }

            if (_drainQueued) return;
            _drainQueued = true;
            dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                _drainQueued = false;
                DrainTick();
            });
            return;
        }

        if (immediate)
            DrainTick();
        else
        {
            try
            {
                _threadDrainTimer?.Change(DrainIntervalMs, Timeout.Infinite);
            }
            catch (ObjectDisposedException)
            {
                // runtime ended
            }
        }
    }

    private void DrainTick()
    {
        if (_disposed) return;
        if (_drainRunning)
        {
            RequestDrain(immediate: false);
            return;
        }

        _drainRunning = true;
        try
        {
            KeyValuePair<int, StatusBarUpdate>[] batch;
            lock (_gate)
            {
                batch = _pending.ToArray();
                _pending.Clear();
                _inboundSinceFlush = 0;
            }

            var appliedAny = false;
            foreach (var (index, update) in batch)
            {
                if (ApplyColumn(index, update, update.Immediate))
                    appliedAny = true;
            }

            var idled = ApplyIdle();
            DrainIntervalMs = appliedAny
                ? StatusBarDefaults.DrainFloorMs
                : Math.Min(StatusBarDefaults.DrainCeilingMs, DrainIntervalMs + StatusBarDefaults.DrainIncreaseMs);

            if (_uiDrainTimer is not null)
                _uiDrainTimer.Interval = TimeSpan.FromMilliseconds(DrainIntervalMs);

            if (appliedAny || idled || HasArmedIdle())
                Notify();

            bool pendingAgain;
            lock (_gate) pendingAgain = _pending.Count > 0;
            if (pendingAgain)
                RequestDrain(immediate: false);
        }
        finally
        {
            _drainRunning = false;
            try
            {
                _threadDrainTimer?.Change(DrainIntervalMs, Timeout.Infinite);
            }
            catch (ObjectDisposedException)
            {
                // runtime ended
            }
        }
    }

    private bool ApplyColumn(int index, StatusBarUpdate update, bool immediate)
    {
        if (index < 0 || index >= Columns.Count)
        {
            DroppedMissingColumn++;
            return false;
        }

        var column = Columns[index];
        if (UpdatesEqualApplied(column, update))
        {
            DroppedDuplicate++;
            return false;
        }

        var now = _nowMs();
        if (!immediate && update.Progress is { } raw
            && update.IsIndeterminate != true
            && !column.IsIndeterminate)
        {
            var next = CoerceProgress(raw);
            _lastAppliedAt.TryGetValue(index, out var last);
            var delta = Math.Abs(next - column.Progress);
            var skipEdge = next is not 0 and not 100;
            if (skipEdge && delta < StatusBarDefaults.ProgressEpsilon && now - last < StatusBarDefaults.ProgressTtlMs)
                return false;
        }

        if (update.Text is not null)
        {
            var trimmed = update.Text.Trim();
            column.Text = column.IsLiveRegion && trimmed.Length == 0
                ? StatusBarDefaults.ReadyText
                : update.Text;
        }

        if (update.Progress is not null)
        {
            column.Progress = CoerceProgress(update.Progress.Value);
            if (update.IsProgressVisible is null)
                column.IsProgressVisible = true;
        }

        if (update.IsIndeterminate is not null)
        {
            column.IsIndeterminate = update.IsIndeterminate.Value;
            if (update.IsIndeterminate.Value && update.IsProgressVisible is null)
                column.IsProgressVisible = true;
        }

        if (update.IsProgressVisible is not null)
            column.IsProgressVisible = update.IsProgressVisible.Value;

        if (update.Icon is not null)
            column.Icon = update.Icon.Value;

        column.IsIdle = false;
        _lastAppliedAt[index] = now;
        Applied++;
        return true;
    }

    private static bool UpdatesEqualApplied(StatusBarColumn column, StatusBarUpdate update)
    {
        if (update.Text is not null)
        {
            var next = column.IsLiveRegion && string.IsNullOrWhiteSpace(update.Text)
                ? StatusBarDefaults.ReadyText
                : update.Text;
            if (next != column.Text) return false;
        }
        if (update.Progress is not null && CoerceProgress(update.Progress.Value) != column.Progress) return false;
        if (update.IsIndeterminate is not null && update.IsIndeterminate.Value != column.IsIndeterminate) return false;
        if (update.IsProgressVisible is not null && update.IsProgressVisible.Value != column.IsProgressVisible) return false;
        if (update.Icon is not null && update.Icon.Value != column.Icon) return false;
        return true;
    }

    private void ApplyIdlePolicy(int idleTimeoutMs, string? idleText)
    {
        if (_disposed) return;
        var timeout = Math.Max(0, idleTimeoutMs);
        var stamped = _nowMs();
        for (var i = 0; i < Columns.Count; i++)
        {
            var column = Columns[i];
            if (column.Kind != StatusBarColumnKind.Text || !column.IsLiveRegion)
                continue;
            column.IdleTimeoutMs = timeout;
            if (idleText is not null)
            {
                column.IdleText = string.IsNullOrWhiteSpace(idleText) ? StatusBarDefaults.IdleText : idleText;
                if (column.IsIdle)
                    column.Text = column.IdleText;
            }
            _lastAppliedAt[i] = stamped;
            column.IsIdle = false;
        }
        Notify();
    }

    private bool ApplyIdle()
    {
        var now = _nowMs();
        var changed = false;
        for (var i = 0; i < Columns.Count; i++)
        {
            var column = Columns[i];
            if (column.Kind is StatusBarColumnKind.Clock or StatusBarColumnKind.Empty) continue;
            if (column.IdleTimeoutMs <= 0) continue;
            if (column.IsIdle)
            {
                if (column.Text != column.IdleText)
                {
                    column.Text = column.IdleText;
                    changed = true;
                }
                continue;
            }

            _lastAppliedAt.TryGetValue(i, out var last);
            if (last == 0) last = now;
            if (now - last < column.IdleTimeoutMs) continue;
            column.Text = column.IdleText;
            column.Icon = StatusBarIconKind.None;
            column.IsIdle = true;
            changed = true;
        }
        return changed;
    }

    private bool HasArmedIdle() =>
        Columns.Any(c => c.IdleTimeoutMs > 0 && !c.IsIdle && c.Kind != StatusBarColumnKind.Clock);

    private StatusBarSnapshot BuildSnapshot() => new()
    {
        Columns = Columns.Select(c => new StatusBarColumnSnapshot
        {
            Key = c.Key,
            Kind = c.Kind,
            Text = c.Text,
            Progress = c.Progress,
            IsIndeterminate = c.IsIndeterminate,
            IsProgressVisible = c.IsProgressVisible,
            Icon = c.Icon,
            IsIdle = c.IsIdle,
            IdleText = c.IdleText,
            IdleTimeoutMs = c.IdleTimeoutMs
        }).ToArray(),
        Position = Position,
        BarThickness = BarThickness,
        DroppedDuplicate = DroppedDuplicate,
        DroppedHighWater = DroppedHighWater,
        DroppedDisposed = DroppedDisposed,
        DroppedMissingColumn = DroppedMissingColumn,
        Compacted = Compacted,
        Applied = Applied,
        DrainIntervalMs = DrainIntervalMs,
        IdleRemainingMs = LiveIdleRemaining()
    };

    private int LiveIdleRemaining()
    {
        var now = _nowMs();
        for (var i = 0; i < Columns.Count; i++)
        {
            var column = Columns[i];
            if (!column.IsLiveRegion || column.IdleTimeoutMs <= 0) continue;
            if (column.IsIdle) return 0;
            _lastAppliedAt.TryGetValue(i, out var last);
            return (int)Math.Max(0, column.IdleTimeoutMs - (now - last));
        }
        return -1;
    }

    private void TickClock()
    {
        var stamp = DateTime.Now.ToString("HH:mm:ss");
        foreach (var column in Columns)
        {
            if (column.Kind == StatusBarColumnKind.Clock)
                column.Text = stamp;
        }
    }

    private void RunOnDispatcher(Action action, bool immediate)
    {
        var dispatcher = _dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        if (immediate)
            dispatcher.BeginInvoke(DispatcherPriority.Input, action);
        else
            dispatcher.BeginInvoke(DispatcherPriority.Background, action);
    }

    private void Notify()
    {
        OnPropertyChanged(nameof(DrainIntervalMs));
        OnPropertyChanged(nameof(Applied));
        Changed?.Invoke();
    }

    private static double CoerceProgress(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return 0;
        return Math.Clamp(value, 0, 100);
    }
}
