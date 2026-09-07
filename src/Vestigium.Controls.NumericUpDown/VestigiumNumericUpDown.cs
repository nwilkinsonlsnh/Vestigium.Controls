using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace Vestigium.Controls.NumericUpDown;

[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_UpButton", Type = typeof(RepeatButton))]
[TemplatePart(Name = "PART_DownButton", Type = typeof(RepeatButton))]
public class VestigiumNumericUpDown : Control
{
    static VestigiumNumericUpDown()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(VestigiumNumericUpDown),
            new FrameworkPropertyMetadata(typeof(VestigiumNumericUpDown)));
    }

    private readonly NumericEngine _engine = new();
    private TextBox? _textBox;
    private RepeatButton? _up;
    private RepeatButton? _down;
    private DispatcherTimer? _drain;
    private bool _syncing;
    private bool _editing;
    private bool _drainRunning;
    private int _drainMs = NumericDefaults.DrainFloorMs;

    public VestigiumNumericUpDown()
    {
        _engine.Changed += OnEngineChanged;
        Loaded += (_, _) => StartDrain();
        Unloaded += (_, _) => StopDrain();
    }

    public NumericEngine Engine => _engine;

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(decimal), typeof(VestigiumNumericUpDown),
            new FrameworkPropertyMetadata(NumericDefaults.Value, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(decimal), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(decimal.MinValue, OnBoundChanged));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(decimal), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(decimal.MaxValue, OnBoundChanged));

    public static readonly DependencyProperty IncrementProperty =
        DependencyProperty.Register(nameof(Increment), typeof(decimal), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(NumericDefaults.Increment, OnIncrementChanged, CoerceIncrement));

    public static readonly DependencyProperty PageIncrementProperty =
        DependencyProperty.Register(nameof(PageIncrement), typeof(decimal), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(NumericDefaults.PageIncrement, OnPageIncrementChanged));

    public static readonly DependencyProperty DecimalPlacesProperty =
        DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(NumericDefaults.DecimalPlaces, OnPlacesChanged));

    public static readonly DependencyProperty FormatStringProperty =
        DependencyProperty.Register(nameof(FormatString), typeof(string), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(NumericDefaults.FormatString, OnFormatChanged));

    public static readonly DependencyProperty UpdateModeProperty =
        DependencyProperty.Register(nameof(UpdateMode), typeof(VestigiumNumericUpdateMode), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(VestigiumNumericUpdateMode.Immediate, OnUpdateModeChanged));

    public static readonly DependencyProperty CommitModeProperty =
        DependencyProperty.Register(nameof(CommitMode), typeof(VestigiumNumericCommitMode), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(VestigiumNumericCommitMode.Auto, OnCommitModeChanged));

    public static readonly DependencyProperty InputModeProperty =
        DependencyProperty.Register(nameof(InputMode), typeof(VestigiumNumericInputMode), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(VestigiumNumericInputMode.Full, OnInputModeChanged));

    public static readonly DependencyProperty SignModeProperty =
        DependencyProperty.Register(nameof(SignMode), typeof(VestigiumNumericSignMode), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(VestigiumNumericSignMode.Signed, OnSignModeChanged));

    public static readonly DependencyProperty SnapToIncrementProperty =
        DependencyProperty.Register(nameof(SnapToIncrement), typeof(bool), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(false, OnSnapChanged));

    public static readonly DependencyProperty SnapModeProperty =
        DependencyProperty.Register(nameof(SnapMode), typeof(VestigiumNumericSnapMode), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(VestigiumNumericSnapMode.Round, OnSnapChanged));

    public static readonly DependencyProperty SnapBaseProperty =
        DependencyProperty.Register(nameof(SnapBase), typeof(decimal), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(0m, OnSnapChanged));

    public static readonly DependencyProperty DelayProperty =
        DependencyProperty.Register(nameof(Delay), typeof(int), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(NumericDefaults.DelayMs, OnRepeatTimingChanged));

    public static readonly DependencyProperty IntervalProperty =
        DependencyProperty.Register(nameof(Interval), typeof(int), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(NumericDefaults.IntervalMs, OnRepeatTimingChanged));

    public static readonly DependencyProperty AccelerationDelayProperty =
        DependencyProperty.Register(nameof(AccelerationDelay), typeof(int), typeof(VestigiumNumericUpDown),
            new PropertyMetadata(NumericDefaults.AccelerationDelayMs, OnAccelChanged));

    public static readonly RoutedEvent ValueChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(ValueChanged), RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<decimal>), typeof(VestigiumNumericUpDown));

    public decimal Value
    {
        get => (decimal)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public decimal Minimum
    {
        get => (decimal)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public decimal Maximum
    {
        get => (decimal)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public decimal Increment
    {
        get => (decimal)GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    public decimal PageIncrement
    {
        get => (decimal)GetValue(PageIncrementProperty);
        set => SetValue(PageIncrementProperty, value);
    }

    public int DecimalPlaces
    {
        get => (int)GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    public string FormatString
    {
        get => (string)GetValue(FormatStringProperty);
        set => SetValue(FormatStringProperty, value);
    }

    public VestigiumNumericUpdateMode UpdateMode
    {
        get => (VestigiumNumericUpdateMode)GetValue(UpdateModeProperty);
        set => SetValue(UpdateModeProperty, value);
    }

    public VestigiumNumericCommitMode CommitMode
    {
        get => (VestigiumNumericCommitMode)GetValue(CommitModeProperty);
        set => SetValue(CommitModeProperty, value);
    }

    public VestigiumNumericInputMode InputMode
    {
        get => (VestigiumNumericInputMode)GetValue(InputModeProperty);
        set => SetValue(InputModeProperty, value);
    }

    public VestigiumNumericSignMode SignMode
    {
        get => (VestigiumNumericSignMode)GetValue(SignModeProperty);
        set => SetValue(SignModeProperty, value);
    }


    public bool SnapToIncrement
    {
        get => (bool)GetValue(SnapToIncrementProperty);
        set => SetValue(SnapToIncrementProperty, value);
    }

    public VestigiumNumericSnapMode SnapMode
    {
        get => (VestigiumNumericSnapMode)GetValue(SnapModeProperty);
        set => SetValue(SnapModeProperty, value);
    }

    public decimal SnapBase
    {
        get => (decimal)GetValue(SnapBaseProperty);
        set => SetValue(SnapBaseProperty, value);
    }

    public int Delay
    {
        get => (int)GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    public int Interval
    {
        get => (int)GetValue(IntervalProperty);
        set => SetValue(IntervalProperty, value);
    }

    public int AccelerationDelay
    {
        get => (int)GetValue(AccelerationDelayProperty);
        set => SetValue(AccelerationDelayProperty, value);
    }

    public event RoutedPropertyChangedEventHandler<decimal> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public override void OnApplyTemplate()
    {
        DetachParts();
        base.OnApplyTemplate();
        _textBox = GetTemplateChild("PART_TextBox") as TextBox;
        _up = GetTemplateChild("PART_UpButton") as RepeatButton;
        _down = GetTemplateChild("PART_DownButton") as RepeatButton;
        AttachParts();
        PushEngineFromDps();
        SyncTextFromEngine();
        ApplyInputMode();
        ApplyRepeatTiming();
        UpdateButtons();
    }

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        if (!IsKeyboardFocusWithin || !_engine.CanSpin)
            return;
        _engine.Step(e.Delta > 0 ? 1 : -1, page: false);
        if (UpdateMode == VestigiumNumericUpdateMode.Deferred)
            _engine.CommitDisplay();
        e.Handled = true;
        base.OnPreviewMouseWheel(e);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Down or Key.PageUp or Key.PageDown)
        {
            if (!_engine.CanSpin) return;
            if (!e.IsRepeat)
                _engine.BeginHold();
            _engine.Step(
                e.Key is Key.Up or Key.PageUp ? 1 : -1,
                e.Key is Key.PageUp or Key.PageDown);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            _engine.TryCommitText(_textBox?.Text, isExplicit: true);
            _editing = false;
            SyncTextFromEngine();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _engine.CancelHold();
            _editing = false;
            SyncTextFromEngine();
            e.Handled = true;
        }

        base.OnPreviewKeyDown(e);
    }

    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Down or Key.PageUp or Key.PageDown)
        {
            _engine.EndHold();
            e.Handled = true;
        }
        base.OnPreviewKeyUp(e);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (VestigiumNumericUpDown)d;
        if (control._syncing) return;
        control._engine.SetCommitted((decimal)e.NewValue);
        control.RaiseEvent(new RoutedPropertyChangedEventArgs<decimal>((decimal)e.OldValue, (decimal)e.NewValue, ValueChangedEvent));
    }

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        var control = (VestigiumNumericUpDown)d;
        return control._engine.Finalize((decimal)baseValue);
    }

    private static void OnBoundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (VestigiumNumericUpDown)d;
        if (e.Property == MinimumProperty)
            control._engine.Minimum = (decimal)e.NewValue;
        else
            control._engine.Maximum = (decimal)e.NewValue;
        control.CoerceValue(ValueProperty);
        control.UpdateButtons();
    }

    private static void OnIncrementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumNumericUpDown)d)._engine.Increment = (decimal)e.NewValue;

    private static object CoerceIncrement(DependencyObject d, object baseValue) =>
        NumericMath.NormalizeIncrement((decimal)baseValue);

    private static void OnPageIncrementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumNumericUpDown)d)._engine.PageIncrement = (decimal)e.NewValue;

    private static void OnPlacesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (VestigiumNumericUpDown)d;
        control._engine.DecimalPlaces = (int)e.NewValue;
        control.CoerceValue(ValueProperty);
    }

    private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (VestigiumNumericUpDown)d;
        control._engine.FormatString = e.NewValue as string ?? NumericDefaults.FormatString;
        control.SyncTextFromEngine();
    }

    private static void OnUpdateModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumNumericUpDown)d)._engine.UpdateMode = (VestigiumNumericUpdateMode)e.NewValue;

    private static void OnCommitModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumNumericUpDown)d)._engine.CommitMode = (VestigiumNumericCommitMode)e.NewValue;

    private static void OnInputModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (VestigiumNumericUpDown)d;
        control._engine.InputMode = (VestigiumNumericInputMode)e.NewValue;
        control.ApplyInputMode();
        control.UpdateButtons();
    }

    private static void OnSignModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (VestigiumNumericUpDown)d;
        control._engine.SignMode = (VestigiumNumericSignMode)e.NewValue;
        control.CoerceValue(ValueProperty);
        control.UpdateButtons();
    }


    private static void OnSnapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (VestigiumNumericUpDown)d;
        control._engine.SnapToIncrement = control.SnapToIncrement;
        control._engine.SnapMode = control.SnapMode;
        control._engine.SnapBase = control.SnapBase;
        control.CoerceValue(ValueProperty);

    }

    private static void OnRepeatTimingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumNumericUpDown)d).ApplyRepeatTiming();

    private static void OnAccelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumNumericUpDown)d)._engine.AccelerationDelayMs = (int)e.NewValue;

    private void OnEngineChanged()
    {
        if (_syncing) return;
        Dispatcher.BeginInvoke(() =>
        {
            _syncing = true;
            try
            {
                var old = Value;
                if (old != _engine.Value)
                {
                    SetCurrentValue(ValueProperty, _engine.Value);
                    RaiseEvent(new RoutedPropertyChangedEventArgs<decimal>(old, _engine.Value, ValueChangedEvent));
                }
                if (!_editing)
                    SyncTextFromEngine();
                UpdateButtons();
            }
            finally
            {
                _syncing = false;
            }
        }, DispatcherPriority.Background);
    }

    private void StartDrain()
    {
        _drain ??= new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(NumericDefaults.DrainFloorMs)
        };
        _drain.Tick -= OnDrainTick;
        _drain.Tick += OnDrainTick;
        _drain.Start();
    }

    private void StopDrain()
    {
        if (_drain is null) return;
        _drain.Stop();
        _drain.Tick -= OnDrainTick;
        _drain = null;
        _engine.CancelHold();
    }

    private void OnDrainTick(object? sender, EventArgs e)
    {
        if (_drainRunning)
            return;
        _drainRunning = true;
        try
        {
            var applied = _engine.FlushImmediate();
            _drainMs = applied
                ? NumericDefaults.DrainFloorMs
                : Math.Min(NumericDefaults.DrainCeilingMs, _drainMs + NumericDefaults.DrainIncreaseMs);
            if (_engine.HasPendingImmediate)
                _drainMs = Math.Max(NumericDefaults.DrainFloorMs, _drainMs / 2);
            if (_drain is not null)
                _drain.Interval = TimeSpan.FromMilliseconds(_drainMs);
        }
        finally
        {
            _drainRunning = false;
        }
    }

    private void AttachParts()
    {
        if (_textBox is not null)
        {
            _textBox.GotKeyboardFocus += OnTextGotFocus;
            _textBox.LostKeyboardFocus += OnTextLostFocus;
            _textBox.PreviewTextInput += OnPreviewTextInput;
            DataObject.AddPastingHandler(_textBox, OnPaste);
        }

        if (_up is not null)
        {
            _up.Click += OnUpClick;
            _up.PreviewMouseLeftButtonDown += OnSpinDown;
            _up.PreviewMouseLeftButtonUp += OnSpinUp;
            _up.LostMouseCapture += OnSpinLost;
        }

        if (_down is not null)
        {
            _down.Click += OnDownClick;
            _down.PreviewMouseLeftButtonDown += OnSpinDown;
            _down.PreviewMouseLeftButtonUp += OnSpinUp;
            _down.LostMouseCapture += OnSpinLost;
        }
    }

    private void OnUpClick(object sender, RoutedEventArgs e) => _engine.Step(1, false);

    private void OnDownClick(object sender, RoutedEventArgs e) => _engine.Step(-1, false);

    private void OnSpinDown(object sender, MouseButtonEventArgs e) => _engine.BeginHold();

    private void OnSpinUp(object sender, MouseButtonEventArgs e) => _engine.EndHold();

    private void OnSpinLost(object sender, MouseEventArgs e)
    {
        if (_engine.IsHolding)
            _engine.EndHold();
    }

    private void DetachParts()
    {
        if (_textBox is not null)
        {
            _textBox.GotKeyboardFocus -= OnTextGotFocus;
            _textBox.LostKeyboardFocus -= OnTextLostFocus;
            _textBox.PreviewTextInput -= OnPreviewTextInput;
            DataObject.RemovePastingHandler(_textBox, OnPaste);
        }

        if (_up is not null)
        {
            _up.Click -= OnUpClick;
            _up.PreviewMouseLeftButtonDown -= OnSpinDown;
            _up.PreviewMouseLeftButtonUp -= OnSpinUp;
            _up.LostMouseCapture -= OnSpinLost;
        }

        if (_down is not null)
        {
            _down.Click -= OnDownClick;
            _down.PreviewMouseLeftButtonDown -= OnSpinDown;
            _down.PreviewMouseLeftButtonUp -= OnSpinUp;
            _down.LostMouseCapture -= OnSpinLost;
        }

        _up = null;
        _down = null;
        _textBox = null;
    }


    private void OnTextGotFocus(object sender, RoutedEventArgs e) => _editing = _engine.CanType;

    private void OnTextLostFocus(object sender, RoutedEventArgs e)
    {
        _engine.TryCommitText(_textBox?.Text, isExplicit: false);
        _editing = false;
        SyncTextFromEngine();
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (!_engine.CanType)
        {
            e.Handled = true;
            return;
        }

        var next = ProposedText(e.Text);
        e.Handled = !IsLegalBuffer(next);
    }

    private void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!_engine.CanType || !e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText))
        {
            e.CancelCommand();
            return;
        }

        var text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string ?? "";
        if (!IsLegalBuffer(ProposedText(text)))
            e.CancelCommand();
    }

    private string ProposedText(string incoming)
    {
        if (_textBox is null) return incoming;
        var current = _textBox.Text ?? "";
        var start = _textBox.SelectionStart;
        var length = _textBox.SelectionLength;
        if (start < 0 || start > current.Length) return current + incoming;
        if (start + length > current.Length) length = current.Length - start;
        return current.Remove(start, length).Insert(start, incoming);
    }

    private bool IsLegalBuffer(string text)
    {
        if (_engine.SignMode == VestigiumNumericSignMode.Unsigned && text.Contains('-'))
            return false;
        if (text.Length == 0 || text == "-" || text == CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
            return true;
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out _);
    }


    private void SyncTextFromEngine()
    {
        if (_textBox is null) return;
        var formatted = _engine.FormattedDisplay;
        if (_textBox.Text != formatted)
            _textBox.Text = formatted;
    }

    private void ApplyInputMode()
    {
        if (_textBox is not null)
            _textBox.IsReadOnly = !_engine.CanType;
        UpdateButtons();
    }

    private void ApplyRepeatTiming()
    {
        var delay = Math.Max(0, Delay);
        var interval = Math.Max(1, Interval);
        if (_up is not null)
        {
            _up.Delay = delay;
            _up.Interval = interval;
        }
        if (_down is not null)
        {
            _down.Delay = delay;
            _down.Interval = interval;
        }
    }

    private void UpdateButtons()
    {
        if (_up is not null)
            _up.IsEnabled = _engine.CanStepUp;
        if (_down is not null)
            _down.IsEnabled = _engine.CanStepDown;
    }

    private void PushEngineFromDps()
    {
        _engine.Minimum = Minimum;
        _engine.Maximum = Maximum;
        _engine.Increment = Increment;
        _engine.PageIncrement = PageIncrement;
        _engine.DecimalPlaces = DecimalPlaces;
        _engine.FormatString = FormatString;
        _engine.UpdateMode = UpdateMode;
        _engine.CommitMode = CommitMode;
        _engine.InputMode = InputMode;
        _engine.SignMode = SignMode;
        _engine.SnapToIncrement = SnapToIncrement;
        _engine.SnapMode = SnapMode;
        _engine.SnapBase = SnapBase;
        _engine.AccelerationDelayMs = AccelerationDelay;
        _engine.SetCommitted(Value);
    }
}
