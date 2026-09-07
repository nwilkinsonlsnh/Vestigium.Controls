using System.Windows;
using System.Windows.Controls;

namespace Vestigium.Controls.StatusBar;

public partial class VestigiumStatusBar : UserControl
{
    private StatusBarEngine? _ownedEngine;
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
        if (_ownedEngine is not null)
        {
            _ownedEngine.Dispose();
            _ownedEngine = null;
            _runtimeStarted = false;
        }
    }

    private void AttachEngine(StatusBarEngine? engine)
    {
        if (engine is null || ColumnHost is null)
            return;
        ColumnHost.ItemsSource = engine.Columns;
        Height = engine.BarThickness;
        MinHeight = engine.BarThickness;
        engine.Changed -= OnEngineChangedTick;
        engine.Changed += OnEngineChangedTick;
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
