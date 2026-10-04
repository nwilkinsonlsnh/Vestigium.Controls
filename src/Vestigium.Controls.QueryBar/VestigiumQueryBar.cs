using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Vestigium.Helpers.Kql;

namespace Vestigium.Controls.QueryBar;

[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_Clear", Type = typeof(Button))]
[TemplatePart(Name = "PART_Chevron", Type = typeof(ToggleButton))]
[TemplatePart(Name = "PART_Completion", Type = typeof(Popup))]
[TemplatePart(Name = "PART_CompletionList", Type = typeof(ListBox))]
public class VestigiumQueryBar : Control
{
    private readonly DispatcherTimer _timer;
    private TextBox? _box;
    private Button? _clear;
    private Popup? _popup;
    private ListBox? _list;
    private KqlCompletion _last = KqlCompletion.Empty;
    private bool _syncing;
    private bool _applying;

    static VestigiumQueryBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(VestigiumQueryBar), new FrameworkPropertyMetadata(typeof(VestigiumQueryBar)));
    }

    public VestigiumQueryBar()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(140) };
        _timer.Tick += (_, _) => Show();
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(VestigiumQueryBar),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    public static readonly DependencyProperty SessionProperty =
        DependencyProperty.Register(nameof(Session), typeof(KqlSession), typeof(VestigiumQueryBar));

    public static readonly DependencyProperty CompletionDelayProperty =
        DependencyProperty.Register(nameof(CompletionDelay), typeof(int), typeof(VestigiumQueryBar),
            new PropertyMetadata(140));

    public static readonly RoutedEvent ClearedEvent =
        EventManager.RegisterRoutedEvent(nameof(Cleared), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VestigiumQueryBar));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public KqlSession? Session
    {
        get => (KqlSession?)GetValue(SessionProperty);
        set => SetValue(SessionProperty, value);
    }

    public int CompletionDelay
    {
        get => (int)GetValue(CompletionDelayProperty);
        set => SetValue(CompletionDelayProperty, value);
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
        _popup = GetTemplateChild("PART_Completion") as Popup;
        _list = GetTemplateChild("PART_CompletionList") as ListBox;
        if (_box is not null)
        {
            _box.Text = Text;
            _box.TextChanged += OnBoxText;
            _box.PreviewKeyDown += OnKey;
        }

        if (_clear is not null)
            _clear.Click += OnClear;
        if (_list is not null)
            _list.MouseDoubleClick += (_, _) => Accept();
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        if (_popup is not null)
            _popup.IsOpen = false;
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
        if (_box is null)
            return;
        if (!_syncing)
        {
            _syncing = true;
            Text = _box.Text;
            _syncing = false;
        }

        if (_applying || Session is null)
        {
            if (_popup is not null)
                _popup.IsOpen = false;
            return;
        }

        _timer.Interval = TimeSpan.FromMilliseconds(Math.Max(40, CompletionDelay));
        _timer.Stop();
        _timer.Start();
    }

    private void OnKey(object sender, KeyEventArgs e)
    {
        if (_popup is not { IsOpen: true })
            return;

        if (e.Key == Key.Escape)
        {
            _popup.IsOpen = false;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down && _list is not null)
        {
            _list.SelectedIndex = Math.Min(_list.Items.Count - 1, _list.SelectedIndex + 1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up && _list is not null)
        {
            _list.SelectedIndex = Math.Max(0, _list.SelectedIndex - 1);
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Tab)
            return;

        Accept();
        e.Handled = true;
    }

    private void Show()
    {
        _timer.Stop();
        if (_box is null || _list is null || _popup is null || Session is null || _applying)
            return;

        _last = KqlHelper.Complete(_box.Text, _box.CaretIndex, Session);
        _list.ItemsSource = _last.Rows;
        if (_last.Rows.Count == 0)
        {
            _popup.IsOpen = false;
            return;
        }

        _list.SelectedIndex = 0;
        var rect = _box.GetRectFromCharacterIndex(_box.CaretIndex);
        _popup.Placement = PlacementMode.Relative;
        _popup.PlacementTarget = _box;
        _popup.HorizontalOffset = rect.X;
        _popup.VerticalOffset = rect.Bottom;
        _popup.IsOpen = true;
    }

    private void Accept()
    {
        if (_box is null || _popup is null)
            return;
        var index = _list is null || _list.SelectedIndex < 0 ? 0 : _list.SelectedIndex;
        if (index >= _last.Rows.Count)
            return;

        var row = _last.Rows[index];
        var start = Math.Clamp(_last.ReplaceStart, 0, _box.Text.Length);
        var length = Math.Clamp(_last.ReplaceLength, 0, _box.Text.Length - start);
        var next = _box.Text.Remove(start, length).Insert(start, row.Insert);
        if (!next.EndsWith('(') && !next.EndsWith(' '))
            next += " ";

        _timer.Stop();
        _applying = true;
        _popup.IsOpen = false;
        _syncing = true;
        Text = next;
        _box.Text = next;
        _box.CaretIndex = next.Length;
        _syncing = false;
        _box.Focus();
        Dispatcher.BeginInvoke(() =>
        {
            if (_box is null)
                return;
            _box.CaretIndex = _box.Text.Length;
            _box.SelectionLength = 0;
            _applying = false;
            OnBoxText(_box, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        }, DispatcherPriority.Input);
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
        {
            _box.TextChanged -= OnBoxText;
            _box.PreviewKeyDown -= OnKey;
        }

        if (_clear is not null)
            _clear.Click -= OnClear;
    }
}
