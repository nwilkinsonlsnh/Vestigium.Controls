using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Vestigium.Controls.StatusBar;

public partial class VestigiumStatusBar : UserControl
{
    private StatusBarEngine? _ownedEngine;
    private StatusBarEngine? _attached;
    private bool _runtimeStarted;

    public VestigiumStatusBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public static readonly DependencyProperty EngineProperty =
        DependencyProperty.Register(
            nameof(Engine),
            typeof(StatusBarEngine),
            typeof(VestigiumStatusBar),
            new PropertyMetadata(null, OnEngineChanged));

    public static readonly DependencyProperty PositionProperty =
        DependencyProperty.Register(
            nameof(Position),
            typeof(VestigiumStatusBarPosition),
            typeof(VestigiumStatusBar),
            new FrameworkPropertyMetadata(
                VestigiumStatusBarPosition.Bottom,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPositionChanged));

    public StatusBarEngine Engine
    {
        get => (StatusBarEngine)GetValue(EngineProperty);
        set => SetValue(EngineProperty, value);
    }

    public VestigiumStatusBarPosition Position
    {
        get => (VestigiumStatusBarPosition)GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    private static void OnEngineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VestigiumStatusBar bar)
            bar.AttachEngine(e.NewValue as StatusBarEngine);
    }

    private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VestigiumStatusBar bar)
        {
            bar.ApplyPosition((VestigiumStatusBarPosition)e.NewValue);
            if (bar.Engine is not null)
                bar.Engine.Position = (VestigiumStatusBarPosition)e.NewValue;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Engine is null)
        {
            _ownedEngine = new StatusBarEngine();
            Engine = _ownedEngine;
        }

        AttachEngine(Engine);
        ApplyPosition(Position);
        Height = Engine.BarThickness;
        MinHeight = Engine.BarThickness;
        if (!_runtimeStarted)
        {
            Engine.StartRuntime(Dispatcher);
            _runtimeStarted = true;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachEngine();
        if (_ownedEngine is not null)
        {
            _ownedEngine.Dispose();
            _ownedEngine = null;
            _runtimeStarted = false;
        }
    }

    private void AttachEngine(StatusBarEngine? engine)
    {
        if (engine is null || LeftHost is null)
            return;
        if (ReferenceEquals(_attached, engine))
        {
            BindGroups();
            return;
        }

        DetachEngine();
        _attached = engine;
        engine.Changed += OnEngineChangedTick;
        engine.Columns.CollectionChanged += OnColumnsChanged;
        foreach (var column in engine.Columns)
            column.PropertyChanged += OnColumnPropertyChanged;
        BindGroups();
        Height = engine.BarThickness;
        MinHeight = engine.BarThickness;
    }

    private void DetachEngine()
    {
        if (_attached is null) return;
        _attached.Changed -= OnEngineChangedTick;
        _attached.Columns.CollectionChanged -= OnColumnsChanged;
        foreach (var column in _attached.Columns)
            column.PropertyChanged -= OnColumnPropertyChanged;
        _attached = null;
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e) => BindGroups();

    private void OnColumnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(StatusBarColumn.Slot)
            or nameof(StatusBarColumn.IsProgressVisible)
            or nameof(StatusBarColumn.Kind)
            or nameof(StatusBarColumn.Icon))
        {
            BindGroups();
        }
    }

    private void BindGroups()
    {
        if (_attached is null || LeftHost is null) return;
        LeftHost.ItemsSource = SlotItems(StatusBarSlot.Left);
        CenterHost.ItemsSource = SlotItems(StatusBarSlot.Center);
        RightHost.ItemsSource = SlotItems(StatusBarSlot.Right);
    }

    private List<StatusBarColumn> SlotItems(StatusBarSlot slot)
    {
        var items = _attached!.Columns.Where(ShouldRender).Where(c => c.Slot == slot).ToList();
        for (var i = 0; i < items.Count; i++)
            items[i].IsSeparatorVisible = i > 0;
        return items;
    }

    private static bool ShouldRender(StatusBarColumn column)
    {
        if (column.Kind == StatusBarColumnKind.Empty) return true;
        if (column.Kind == StatusBarColumnKind.Progress) return column.IsProgressVisible;
        return true;
    }

    private void OnEngineChangedTick()
    {
        Dispatcher.Invoke(() =>
        {
            if (Engine is null) return;
            Height = Engine.BarThickness;
            MinHeight = Engine.BarThickness;
            if (Position != Engine.Position)
                Position = Engine.Position;
            BindGroups();
        });
    }

    private void ApplyPosition(VestigiumStatusBarPosition position)
    {
        var dock = position == VestigiumStatusBarPosition.Top ? Dock.Top : Dock.Bottom;
        DockPanel.SetDock(this, dock);
        if (RootBorder is not null)
        {
            RootBorder.BorderThickness = position == VestigiumStatusBarPosition.Top
                ? new Thickness(0, 0, 0, 1)
                : new Thickness(0, 1, 0, 0);
        }
    }
}
