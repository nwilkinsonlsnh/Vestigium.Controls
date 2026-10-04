using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Vestigium.Controls.QueryBar;

[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_Clear", Type = typeof(Button))]
[TemplatePart(Name = "PART_Chevron", Type = typeof(ToggleButton))]
public class VestigiumQueryBar : Control
{
    private TextBox? _box;
    private Button? _clear;
    private bool _syncing;

    static VestigiumQueryBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(VestigiumQueryBar), new FrameworkPropertyMetadata(typeof(VestigiumQueryBar)));
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(VestigiumQueryBar),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    public static readonly RoutedEvent ClearedEvent =
        EventManager.RegisterRoutedEvent(nameof(Cleared), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VestigiumQueryBar));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public event RoutedEventHandler Cleared
    {
        add => AddHandler(ClearedEvent, value);
        remove => RemoveHandler(ClearedEvent, value);
    }

    public override void OnApplyTemplate()
    {
        Detach();
        base.OnApplyTemplate();
        _box = GetTemplateChild("PART_TextBox") as TextBox;
        _clear = GetTemplateChild("PART_Clear") as Button;
        if (_box is not null)
        {
            _box.Text = Text;
            _box.TextChanged += OnBoxText;
        }

        if (_clear is not null)
            _clear.Click += OnClear;
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        Text = string.Empty;
        if (_box is not null)
        {
            _box.Focus();
            _box.CaretIndex = _box.Text.Length;
        }

        RaiseEvent(new RoutedEventArgs(ClearedEvent, this));
    }

    private void OnBoxText(object sender, TextChangedEventArgs e)
    {
        if (_syncing || _box is null)
            return;
        _syncing = true;
        Text = _box.Text;
        _syncing = false;
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var bar = (VestigiumQueryBar)d;
        if (bar._syncing || bar._box is null)
            return;
        bar._syncing = true;
        bar._box.Text = e.NewValue as string ?? string.Empty;
        bar._box.CaretIndex = bar._box.Text.Length;
        bar._syncing = false;
    }

    private void Detach()
    {
        if (_box is not null)
            _box.TextChanged -= OnBoxText;
        if (_clear is not null)
            _clear.Click -= OnClear;
    }
}
