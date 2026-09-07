using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Vestigium.Controls.UnderConstruction;

[TemplatePart(Name = "PART_Icon", Type = typeof(Path))]
[TemplatePart(Name = "PART_Header", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_Message", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_Action", Type = typeof(Button))]
public class VestigiumUnderConstruction : Control
{
    static VestigiumUnderConstruction()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(VestigiumUnderConstruction),
            new FrameworkPropertyMetadata(typeof(VestigiumUnderConstruction)));
    }

    public VestigiumUnderConstruction()
    {
        Focusable = false;
    }

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(VestigiumUnderConstruction),
            new PropertyMetadata(UnderConstructionRules.DefaultHeader, null, CoerceText));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(VestigiumUnderConstruction),
            new PropertyMetadata(string.Empty, OnMessageChanged, CoerceText));

    public static readonly DependencyProperty IconDataProperty =
        DependencyProperty.Register(nameof(IconData), typeof(Geometry), typeof(VestigiumUnderConstruction),
            new PropertyMetadata(VestigiumUnderConstructionGlyphs.CreateDefault()));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(VestigiumUnderConstruction),
            new PropertyMetadata(true, OnIsActiveChanged));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(VestigiumUnderConstruction),
            new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(VestigiumUnderConstruction),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ActionTextProperty =
        DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(VestigiumUnderConstruction),
            new PropertyMetadata(UnderConstructionRules.DefaultActionText, null, CoerceText));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public Geometry IconData
    {
        get => (Geometry)GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public bool HasAction => UnderConstructionRules.ShowAction(Command);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ApplyActive();
        ApplyActionVisibility();
        ApplyMessageVisibility();
    }

    private static object CoerceText(DependencyObject d, object baseValue) =>
        UnderConstructionRules.CoerceText(baseValue as string);

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumUnderConstruction)d).ApplyActive();

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumUnderConstruction)d).ApplyActionVisibility();

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumUnderConstruction)d).ApplyMessageVisibility();

    private void ApplyActive()
    {
        Visibility = UnderConstructionRules.ActiveVisibility(IsActive);
        IsHitTestVisible = IsActive;
    }

    private void ApplyActionVisibility()
    {
        if (GetTemplateChild("PART_Action") is UIElement button)
            button.Visibility = UnderConstructionRules.ShowAction(Command) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyMessageVisibility()
    {
        if (GetTemplateChild("PART_Message") is UIElement block)
            block.Visibility = UnderConstructionRules.ShowMessage(Message) ? Visibility.Visible : Visibility.Collapsed;
    }
}
