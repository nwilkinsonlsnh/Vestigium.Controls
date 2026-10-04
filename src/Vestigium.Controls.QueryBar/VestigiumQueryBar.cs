using System.Collections;
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
[TemplatePart(Name = "PART_Saved", Type = typeof(Popup))]
[TemplatePart(Name = "PART_SavedList", Type = typeof(ListBox))]
public class VestigiumQueryBar : Control
{
    private readonly DispatcherTimer _timer;
    private TextBox? _box;
    private Button? _clear;
    private ToggleButton? _chevron;
    private Popup? _popup;
    private Popup? _saved;
    private ListBox? _list;
    private ListBox? _savedList;
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
        ApplyQueryCommand = new Relay(ApplySaved);
        PinQueryCommand = new Relay(Pin);
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(VestigiumQueryBar),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    public static readonly DependencyProperty SessionProperty =
        DependencyProperty.Register(nameof(Session), typeof(KqlSession), typeof(VestigiumQueryBar));

    public static readonly DependencyProperty QueriesProperty =
        DependencyProperty.Register(nameof(Queries), typeof(IEnumerable), typeof(VestigiumQueryBar));

    public static readonly DependencyProperty LimitProperty =
        DependencyProperty.Register(nameof(Limit), typeof(int), typeof(VestigiumQueryBar),
            new PropertyMetadata(10, OnLimitChanged));

    public static readonly DependencyProperty CloseOnApplyProperty =
        DependencyProperty.Register(nameof(CloseOnApply), typeof(bool), typeof(VestigiumQueryBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty CompletionDelayProperty =
        DependencyProperty.Register(nameof(CompletionDelay), typeof(int), typeof(VestigiumQueryBar),
            new PropertyMetadata(140));

    public static readonly RoutedEvent ClearedEvent =
        EventManager.RegisterRoutedEvent(nameof(Cleared), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VestigiumQueryBar));

    public static readonly RoutedEvent PinRequestedEvent =
        EventManager.RegisterRoutedEvent(nameof(PinRequested), RoutingStrategy.Bubble, typeof(EventHandler<VestigiumQueryRowEventArgs>), typeof(VestigiumQueryBar));

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

    public IEnumerable? Queries
    {
        get => (IEnumerable?)GetValue(QueriesProperty);
        set => SetValue(QueriesProperty, value);
    }

    public int Limit
    {
        get => (int)GetValue(LimitProperty);
        set => SetValue(LimitProperty, value);
    }

    public bool CloseOnApply
    {
        get => (bool)GetValue(CloseOnApplyProperty);
        set => SetValue(CloseOnApplyProperty, value);
    }

    public int CompletionDelay
    {
        get => (int)GetValue(CompletionDelayProperty);
        set => SetValue(CompletionDelayProperty, value);
    }

    public ICommand ApplyQueryCommand { get; }

    public ICommand PinQueryCommand { get; }

    public event RoutedEventHandler Cleared
    {
        add => AddHandler(ClearedEvent, value);
        remove => RemoveHandler(ClearedEvent, value);
    }

    public event EventHandler<VestigiumQueryRowEventArgs> PinRequested
    {
        add => AddHandler(PinRequestedEvent, value);
        remove => RemoveHandler(PinRequestedEvent, value);
    }

    public override void OnApplyTemplate()
    {
        Detach();
        base.OnApplyTemplate();
        _box = GetTemplateChild("PART_TextBox") as TextBox;
        _clear = GetTemplateChild("PART_Clear") as Button;
        _chevron = GetTemplateChild("PART_Chevron") as ToggleButton;
        _popup = GetTemplateChild("PART_Completion") as Popup;
        _saved = GetTemplateChild("PART_Saved") as Popup;
        _list = GetTemplateChild("PART_CompletionList") as ListBox;
        _savedList = GetTemplateChild("PART_SavedList") as ListBox;
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
        if (_saved is not null)
        {
            _saved.Placement = PlacementMode.Custom;
            _saved.PlacementTarget = this;
            _saved.CustomPopupPlacementCallback = Place;
        }

        ApplyLimit();
    }

    private void ApplySaved(object? row)
    {
        WriteSaved(row);
        if (CloseOnApply && _chevron is not null)
            _chevron.IsChecked = false;
    }

    private void WriteSaved(object? row)
    {
        var text = Read(row, "Text");
        _timer.Stop();
        _applying = true;
        if (_popup is not null)
            _popup.IsOpen = false;
        Text = text;
        if (_box is not null)
        {
            _box.Text = text;
            _box.CaretIndex = text.Length;
            _box.Focus();
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (_box is not null)
            {
                _box.CaretIndex = _box.Text.Length;
                _box.SelectionLength = 0;
            }

            _applying = false;
        }, DispatcherPriority.Input);
    }

    private void Pin(object? row) =>
        RaiseEvent(new VestigiumQueryRowEventArgs(PinRequestedEvent, this, row));

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
        if (e.Key == Key.Down && Keyboard.Modifiers == ModifierKeys.Control)
        {
            OpenSaved();
            e.Handled = true;
            return;
        }

        if (_chevron is { IsChecked: true } && e.Key is Key.Left or Key.Right)
        {
            MoveSaved(e.Key == Key.Right ? 1 : -1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _chevron is { IsChecked: true })
        {
            _chevron.IsChecked = false;
            e.Handled = true;
            return;
        }

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

    private void OpenSaved()
    {
        if (Limit <= 0 || _chevron is null)
            return;

        _timer.Stop();
        if (_popup is not null)
            _popup.IsOpen = false;
        _chevron.IsChecked = true;
        if (_savedList is { Items.Count: > 0, SelectedIndex: < 0 })
            _savedList.SelectedIndex = 0;
    }

    private void MoveSaved(int delta)
    {
        if (_savedList is null || _savedList.Items.Count == 0)
            return;
        var next = _savedList.SelectedIndex < 0 ? 0 : _savedList.SelectedIndex + delta;
        _savedList.SelectedIndex = Math.Clamp(next, 0, _savedList.Items.Count - 1);
        _savedList.ScrollIntoView(_savedList.SelectedItem);
        WriteSaved(_savedList.SelectedItem);
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

    private void ApplyLimit()
    {
        if (_chevron is null)
            return;
        _chevron.Visibility = Limit > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (Limit == 0)
            _chevron.IsChecked = false;
    }

    private CustomPopupPlacement[] Place(Size popupSize, Size targetSize, Point offset)
    {
        var x = 0d;
        var window = Window.GetWindow(this);
        if (window is not null)
        {
            var origin = PointToScreen(new Point(0, 0));
            var right = origin.X + popupSize.Width;
            var limit = window.PointToScreen(new Point(window.ActualWidth, 0)).X;
            if (right > limit)
                x = limit - right;
        }

        return [new CustomPopupPlacement(new Point(x, targetSize.Height), PopupPrimaryAxis.Horizontal)];
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

    private static void OnLimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumQueryBar)d).ApplyLimit();

    private static string Read(object? row, string name)
    {
        if (row is null)
            return string.Empty;
        var property = row.GetType().GetProperty(name);
        return property?.GetValue(row) as string ?? string.Empty;
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

    private sealed class Relay : ICommand
    {
        private readonly Action<object?> _run;

        public Relay(Action<object?> run) => _run = run;

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _run(parameter);
    }
}
