using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Vestigium.Controls.UnderConstruction;

[TemplatePart(Name = "PART_Image", Type = typeof(Image))]
[TemplatePart(Name = "PART_DefaultArt", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PART_Icon", Type = typeof(Path))]
[TemplatePart(Name = "PART_Title", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_Subject", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_Description", Type = typeof(TextBlock))]
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

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(VestigiumUnderConstruction),
            new FrameworkPropertyMetadata(UnderConstructionRules.DefaultTitle,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTitleChanged, CoerceTitle));

    public static readonly DependencyProperty SubjectProperty =
        DependencyProperty.Register(nameof(Subject), typeof(string), typeof(VestigiumUnderConstruction),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSubjectChanged, CoerceSubject));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(VestigiumUnderConstruction),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDescriptionChanged, CoerceDescription));

    public static readonly DependencyProperty TitleMaxLengthProperty =
        DependencyProperty.Register(nameof(TitleMaxLength), typeof(int), typeof(VestigiumUnderConstruction),
            new PropertyMetadata(UnderConstructionRules.DefaultTitleMaxLength, OnTitleMaxLengthChanged, CoerceTitleMax));

    public static readonly DependencyProperty SubjectMaxLengthProperty =
        DependencyProperty.Register(nameof(SubjectMaxLength), typeof(int), typeof(VestigiumUnderConstruction),
            new PropertyMetadata(UnderConstructionRules.DefaultSubjectMaxLength, OnSubjectMaxLengthChanged, CoerceSubjectMax));

    public static readonly DependencyProperty DescriptionMaxLengthProperty =
        DependencyProperty.Register(nameof(DescriptionMaxLength), typeof(int), typeof(VestigiumUnderConstruction),
            new PropertyMetadata(0, OnDescriptionMaxLengthChanged, CoerceDescriptionMax));

    public static readonly DependencyProperty ImageSourceProperty =
        DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(VestigiumUnderConstruction),
            new PropertyMetadata(null, OnArtworkChanged));

    public static readonly DependencyProperty ImageUriProperty =
        DependencyProperty.Register(nameof(ImageUri), typeof(string), typeof(VestigiumUnderConstruction),
            new PropertyMetadata(null, OnImageUriChanged));

    public static readonly DependencyProperty IconDataProperty =
        DependencyProperty.Register(nameof(IconData), typeof(Geometry), typeof(VestigiumUnderConstruction),
            new PropertyMetadata(null, OnArtworkChanged));

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
            new PropertyMetadata(UnderConstructionRules.DefaultActionText, null, CoercePlainText));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public int TitleMaxLength
    {
        get => (int)GetValue(TitleMaxLengthProperty);
        set => SetValue(TitleMaxLengthProperty, value);
    }

    public string Subject
    {
        get => (string)GetValue(SubjectProperty);
        set => SetValue(SubjectProperty, value);
    }

    public int SubjectMaxLength
    {
        get => (int)GetValue(SubjectMaxLengthProperty);
        set => SetValue(SubjectMaxLengthProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public int DescriptionMaxLength
    {
        get => (int)GetValue(DescriptionMaxLengthProperty);
        set => SetValue(DescriptionMaxLengthProperty, value);
    }

    public ImageSource? ImageSource
    {
        get => (ImageSource?)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public string? ImageUri
    {
        get => (string?)GetValue(ImageUriProperty);
        set => SetValue(ImageUriProperty, value);
    }

    public Geometry? IconData
    {
        get => (Geometry?)GetValue(IconDataProperty);
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

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ApplyActive();
        ApplyArtwork();
        ApplyTextVisibility();
        ApplyActionVisibility();
    }

    private static object CoercePlainText(DependencyObject d, object baseValue) =>
        UnderConstructionRules.CoerceText(baseValue as string);

    private static object CoerceTitle(DependencyObject d, object baseValue) =>
        UnderConstructionRules.Limit(baseValue as string, ((VestigiumUnderConstruction)d).TitleMaxLength);

    private static object CoerceSubject(DependencyObject d, object baseValue) =>
        UnderConstructionRules.Limit(baseValue as string, ((VestigiumUnderConstruction)d).SubjectMaxLength);

    private static object CoerceDescription(DependencyObject d, object baseValue) =>
        UnderConstructionRules.Limit(baseValue as string, ((VestigiumUnderConstruction)d).DescriptionMaxLength);

    private static object CoerceTitleMax(DependencyObject d, object baseValue) =>
        UnderConstructionRules.CoerceMaxLength(baseValue is int n ? n : UnderConstructionRules.DefaultTitleMaxLength,
            UnderConstructionRules.DefaultTitleMaxLength);

    private static object CoerceSubjectMax(DependencyObject d, object baseValue) =>
        UnderConstructionRules.CoerceMaxLength(baseValue is int n ? n : UnderConstructionRules.DefaultSubjectMaxLength,
            UnderConstructionRules.DefaultSubjectMaxLength);

    private static object CoerceDescriptionMax(DependencyObject d, object baseValue) =>
        UnderConstructionRules.CoerceMaxLength(baseValue is int n ? n : 0, 0);

    private static void OnTitleMaxLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        d.CoerceValue(TitleProperty);

    private static void OnSubjectMaxLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        d.CoerceValue(SubjectProperty);

    private static void OnDescriptionMaxLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        d.CoerceValue(DescriptionProperty);

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumUnderConstruction)d).ApplyTextVisibility();

    private static void OnSubjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumUnderConstruction)d).ApplyTextVisibility();

    private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumUnderConstruction)d).ApplyTextVisibility();

    private static void OnArtworkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumUnderConstruction)d).ApplyArtwork();

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumUnderConstruction)d).ApplyActive();

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumUnderConstruction)d).ApplyActionVisibility();

    private static void OnImageUriChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (VestigiumUnderConstruction)d;
        var uriText = e.NewValue as string;
        if (string.IsNullOrWhiteSpace(uriText))
        {
            control.ApplyArtwork();
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(uriText, UriKind.RelativeOrAbsolute);
            bitmap.EndInit();
            bitmap.Freeze();
            control.SetCurrentValue(ImageSourceProperty, bitmap);
        }
        catch (Exception)
        {
            // SVG or bad URI: leave ImageSource alone; template falls back to default art or IconData.
        }

        control.ApplyArtwork();
    }

    private void ApplyActive()
    {
        Visibility = UnderConstructionRules.ActiveVisibility(IsActive);
        IsHitTestVisible = IsActive;
    }

    private void ApplyArtwork()
    {
        var image = GetTemplateChild("PART_Image") as UIElement;
        var builtIn = GetTemplateChild("PART_DefaultArt") as UIElement;
        var icon = GetTemplateChild("PART_Icon") as UIElement;
        if (image is null && builtIn is null && icon is null)
            return;

        var hasImage = ImageSource is not null;
        var hasGeometry = IconData is not null && !hasImage;
        var hasBuiltIn = !hasImage && !hasGeometry;

        if (image is not null)
            image.Visibility = hasImage ? Visibility.Visible : Visibility.Collapsed;
        if (builtIn is not null)
            builtIn.Visibility = hasBuiltIn ? Visibility.Visible : Visibility.Collapsed;
        if (icon is not null)
            icon.Visibility = hasGeometry ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyTextVisibility()
    {
        SetPartVisibility("PART_Title", UnderConstructionRules.ShowText(Title));
        SetPartVisibility("PART_Subject", UnderConstructionRules.ShowText(Subject));
        SetPartVisibility("PART_Description", UnderConstructionRules.ShowText(Description));
    }

    private void ApplyActionVisibility()
    {
        SetPartVisibility("PART_Action", UnderConstructionRules.ShowAction(Command));
    }

    private void SetPartVisibility(string name, bool show)
    {
        if (GetTemplateChild(name) is UIElement part)
            part.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }
}
