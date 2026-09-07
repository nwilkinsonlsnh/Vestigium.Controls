using System.Windows;
using System.Windows.Controls;

namespace Vestigium.Controls.StatusBar;

public partial class VestigiumStatusBar : UserControl
{
    public VestigiumStatusBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public static readonly DependencyProperty PositionProperty =
        DependencyProperty.Register(
            nameof(Position),
            typeof(VestigiumStatusBarPosition),
            typeof(VestigiumStatusBar),
            new FrameworkPropertyMetadata(
                VestigiumStatusBarPosition.Bottom,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPositionChanged));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(
            nameof(Message),
            typeof(string),
            typeof(VestigiumStatusBar),
            new FrameworkPropertyMetadata("Ready", OnMessageChanged));

    public VestigiumStatusBarPosition Position
    {
        get => (VestigiumStatusBarPosition)GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => ApplyPosition(Position);

    private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VestigiumStatusBar bar)
        {
            bar.ApplyPosition((VestigiumStatusBarPosition)e.NewValue);
        }
    }

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not VestigiumStatusBar bar)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(e.NewValue as string))
        {
            bar.SetCurrentValue(MessageProperty, "Ready");
        }
    }

    private void ApplyPosition(VestigiumStatusBarPosition position)
    {
        var dock = position == VestigiumStatusBarPosition.Top
            ? Dock.Top
            : Dock.Bottom;

        DockPanel.SetDock(this, dock);

        if (RootBorder is not null)
        {
            RootBorder.BorderThickness = position == VestigiumStatusBarPosition.Top
                ? new Thickness(0, 0, 0, 1)
                : new Thickness(0, 1, 0, 0);
        }
    }
}
