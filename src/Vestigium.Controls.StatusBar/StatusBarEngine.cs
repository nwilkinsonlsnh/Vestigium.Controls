using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Vestigium.Controls.StatusBar;

public sealed class StatusBarEngine : ObservableObject, IUpdateStatusBar, IDisposable
{
    private readonly Func<long> _nowMs;
    private readonly Dictionary<int, StatusBarUpdate> _pending = [];
    private readonly Dictionary<int, long> _lastAppliedAt = [];
    private Timer? _drainTimer;
    private Timer? _clockTimer;
    private SynchronizationContext? _sync;
    private bool _disposed;
    private int _inboundSinceCompact;

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
        BarThickness = StatusBarDefaults.BarThickness;
    }

    public ObservableCollection<StatusBarColumn> Columns { get; }

    private VestigiumStatusBarPosition _position = VestigiumStatusBarPosition.Bottom;
    private double _barThickness = StatusBarDefaults.BarThickness;

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

    public void StartRuntime()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _sync ??= SynchronizationContext.Current;
        _drainTimer ??= new Timer(_ => DrainTick(), null, DrainIntervalMs, Timeout.Infinite);
        _clockTimer ??= new Timer(_ =>
        {
            TickClock();
            Notify();
        }, null, 1000, 1000);
        TickClock();
    }

    public void Post(int index, StatusBarUpdate update) => Enqueue(index, update, immediate: false);

    public void Post(string key, StatusBarUpdate update) => Enqueue(IndexOfKey(key), update, immediate: false);

    public void PostImmediate(int index, StatusBarUpdate update) => Enqueue(index, update, immediate: true);

    public void PostImmediate(string key, StatusBarUpdate update) => Enqueue(IndexOfKey(key), update, immediate: true);

    public void SetIdlePolicy(int idleTimeoutMs, string? idleText = null)
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

    public StatusBarSnapshot Snapshot()
    {
        return new StatusBarSnapshot
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
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _drainTimer?.Dispose();
        _clockTimer?.Dispose();
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
        if (_disposed)
        {
            DroppedDisposed++;
            Notify();
            return;
        }

        if (index < 0 || index >= Columns.Count)
        {
            DroppedMissingColumn++;
            Notify();
            return;
        }

        _inboundSinceCompact++;
        if (_inboundSinceCompact > StatusBarDefaults.InboundHighWater)
        {
            _pending.Clear();
            _inboundSinceCompact = 1;
            Compacted++;
            DroppedHighWater++;
        }

        if (_pending.ContainsKey(index))
            Compacted++;

        if (_pending.TryGetValue(index, out var existing))
        {
            update = Merge(existing, update);
        }

        _pending[index] = update;

        if (immediate || update.Immediate)
        {
            ApplyColumn(index, update, immediate: true);
            _pending.Remove(index);
            DrainIntervalMs = StatusBarDefaults.DrainFloorMs;
            Notify();
            return;
        }

        DrainIntervalMs = Math.Max(StatusBarDefaults.DrainFloorMs, DrainIntervalMs / 2);
        _drainTimer?.Change(DrainIntervalMs, Timeout.Infinite);
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

    private void DrainTick()
    {
        if (_disposed) return;
        var appliedAny = FlushPending();
        var idled = ApplyIdle();
        DrainIntervalMs = appliedAny
            ? StatusBarDefaults.DrainFloorMs
            : Math.Min(StatusBarDefaults.DrainCeilingMs, DrainIntervalMs + StatusBarDefaults.DrainIncreaseMs);
        if (appliedAny || idled || HasArmedIdle())
            Notify();
        try
        {
            _drainTimer?.Change(DrainIntervalMs, Timeout.Infinite);
        }
        catch (ObjectDisposedException)
        {
            // runtime ended
        }
    }

    private bool FlushPending()
    {
        if (_pending.Count == 0) return false;
        var appliedAny = false;
        foreach (var (index, update) in _pending.ToArray())
        {
            if (ApplyColumn(index, update, immediate: false))
                appliedAny = true;
        }
        _pending.Clear();
        _inboundSinceCompact = 0;
        return appliedAny;
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

    private void Notify()
    {
        void Raise()
        {
            OnPropertyChanged(nameof(DrainIntervalMs));
            OnPropertyChanged(nameof(Applied));
            Changed?.Invoke();
        }

        if (_sync is not null)
            _sync.Post(_ => Raise(), null);
        else
            Raise();
    }

    private static double CoerceProgress(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return 0;
        return Math.Clamp(value, 0, 100);
    }
}
