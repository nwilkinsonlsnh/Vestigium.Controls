using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace Vestigium.Controls.PropertiesGrid;

[TemplatePart(Name = "PART_Search", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_Toolbar", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PART_List", Type = typeof(ListBox))]
[TemplatePart(Name = "PART_Splitter", Type = typeof(Thumb))]
[TemplatePart(Name = "PART_Description", Type = typeof(FrameworkElement))]
public class VestigiumPropertiesGrid : Control
{
    static VestigiumPropertiesGrid()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(VestigiumPropertiesGrid),
            new FrameworkPropertyMetadata(typeof(VestigiumPropertiesGrid)));
    }

    private readonly PropertyEngine _engine = new();
    private ListBox? _list;
    private Thumb? _splitter;
    private DispatcherTimer? _drain;
    private bool _syncing;
    private int _drainMs = 50;
    private bool _flushQueued;

    public VestigiumPropertiesGrid()
    {
        _engine.RowsChanged += (_, _) => RefreshList();
        _engine.ValueChanged += (_, e) => RaiseEvent(new RoutedPropertyChangedEventArgs<object>(e.OldValue!, e.NewValue!, PropertyValueChangedEvent)
        {
            Source = this
        });
        _engine.NeedsRefresh += (_, _) =>
        {
            _flushQueued = true;
            _drainMs = 50;
            if (_drain is not null)
                _drain.Interval = TimeSpan.FromMilliseconds(_drainMs);
        };
        Loaded += (_, _) => StartDrain();
        Unloaded += (_, _) => StopDrain();
        CommandBindings.Add(new CommandBinding(ResetCommand, (_, _) =>
        {
            if (_engine.SelectedProperty is { } item)
                _engine.TryReset(item);
        }, (_, e) => e.CanExecute = _engine.SelectedProperty?.CanReset == true && !IsReadOnly));
        CommandBindings.Add(new CommandBinding(ResetAllCommand, (_, _) => _engine.ResetAll(),
            (_, e) => e.CanExecute = !IsReadOnly));
        CommandBindings.Add(new CommandBinding(CategorizedCommand, (_, _) => Sort = VestigiumPropertySort.Categorized));
        CommandBindings.Add(new CommandBinding(AlphabeticalCommand, (_, _) => Sort = VestigiumPropertySort.Alphabetical));
        SetCurrentValue(CategoryIconsProperty, new VestigiumCategoryIconCollection());
    }

    public PropertyEngine Engine => _engine;

    public static readonly RoutedCommand ResetCommand = new(nameof(ResetCommand), typeof(VestigiumPropertiesGrid));
    public static readonly RoutedCommand ResetAllCommand = new(nameof(ResetAllCommand), typeof(VestigiumPropertiesGrid));
    public static readonly RoutedCommand CategorizedCommand = new(nameof(CategorizedCommand), typeof(VestigiumPropertiesGrid));
    public static readonly RoutedCommand AlphabeticalCommand = new(nameof(AlphabeticalCommand), typeof(VestigiumPropertiesGrid));

    public static readonly DependencyProperty SelectedObjectProperty =
        DependencyProperty.Register(nameof(SelectedObject), typeof(object), typeof(VestigiumPropertiesGrid),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, OnSelectedObjectChanged));

    public static readonly DependencyProperty SelectedObjectsProperty =
        DependencyProperty.Register(nameof(SelectedObjects), typeof(IList), typeof(VestigiumPropertiesGrid),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, OnSelectedObjectsChanged));

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(VestigiumPropertiesGrid),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty SortProperty =
        DependencyProperty.Register(nameof(Sort), typeof(VestigiumPropertySort), typeof(VestigiumPropertiesGrid),
            new PropertyMetadata(VestigiumPropertySort.Categorized, OnSortChanged));

    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(VestigiumPropertiesGrid),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSearchChanged));

    public static readonly DependencyProperty ShowSearchProperty =
        DependencyProperty.Register(nameof(ShowSearch), typeof(bool), typeof(VestigiumPropertiesGrid),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowToolbarProperty =
        DependencyProperty.Register(nameof(ShowToolbar), typeof(bool), typeof(VestigiumPropertiesGrid),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowDescriptionProperty =
        DependencyProperty.Register(nameof(ShowDescription), typeof(bool), typeof(VestigiumPropertiesGrid),
            new PropertyMetadata(true));

    public static readonly DependencyProperty NameColumnWidthProperty =
        DependencyProperty.Register(nameof(NameColumnWidth), typeof(double), typeof(VestigiumPropertiesGrid),
            new FrameworkPropertyMetadata(PropertyRules.DefaultNameColumnWidth, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty SelectedPropertyProperty =
        DependencyProperty.Register(nameof(SelectedProperty), typeof(VestigiumPropertyItem), typeof(VestigiumPropertiesGrid),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedPropertyChanged));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(VestigiumPropertiesGrid),
            new PropertyMetadata(false, OnReadOnlyChanged));

    public static readonly DependencyProperty MaxExpandDepthProperty =
        DependencyProperty.Register(nameof(MaxExpandDepth), typeof(int), typeof(VestigiumPropertiesGrid),
            new PropertyMetadata(PropertyRules.DefaultMaxExpandDepth, OnMaxDepthChanged, CoerceMaxDepth));

    public static readonly DependencyProperty EditorTextAlignmentProperty =
        DependencyProperty.Register(nameof(EditorTextAlignment), typeof(TextAlignment), typeof(VestigiumPropertiesGrid),
            new FrameworkPropertyMetadata(TextAlignment.Left, OnEditorTextAlignmentChanged, CoerceEditorTextAlignment));

    public static readonly DependencyProperty CategoryIconsProperty =
        DependencyProperty.Register(nameof(CategoryIcons), typeof(IList), typeof(VestigiumPropertiesGrid),
            new PropertyMetadata(null, OnCategoryIconsChanged));

    public static readonly DependencyProperty CategoryTextAlignmentProperty =
        DependencyProperty.Register(nameof(CategoryTextAlignment), typeof(TextAlignment), typeof(VestigiumPropertiesGrid),
            new FrameworkPropertyMetadata(TextAlignment.Left, FrameworkPropertyMetadataOptions.AffectsRender, null, CoerceEditorTextAlignment));

    public static readonly DependencyProperty IsCategoryBoldProperty =
        DependencyProperty.Register(nameof(IsCategoryBold), typeof(bool), typeof(VestigiumPropertiesGrid),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CategoryOrientationProperty =
        DependencyProperty.Register(nameof(CategoryOrientation), typeof(Orientation), typeof(VestigiumPropertiesGrid),
            new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ObjectCountTextProperty =
        DependencyProperty.Register(nameof(ObjectCountText), typeof(string), typeof(VestigiumPropertiesGrid),
            new PropertyMetadata(string.Empty));

    public static readonly RoutedEvent PropertyValueChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(PropertyValueChanged), RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<object>), typeof(VestigiumPropertiesGrid));

    public object? SelectedObject
    {
        get => GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    public IList? SelectedObjects
    {
        get => (IList?)GetValue(SelectedObjectsProperty);
        set => SetValue(SelectedObjectsProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public VestigiumPropertySort Sort
    {
        get => (VestigiumPropertySort)GetValue(SortProperty);
        set => SetValue(SortProperty, value);
    }

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public bool ShowSearch
    {
        get => (bool)GetValue(ShowSearchProperty);
        set => SetValue(ShowSearchProperty, value);
    }

    public bool ShowToolbar
    {
        get => (bool)GetValue(ShowToolbarProperty);
        set => SetValue(ShowToolbarProperty, value);
    }

    public bool ShowDescription
    {
        get => (bool)GetValue(ShowDescriptionProperty);
        set => SetValue(ShowDescriptionProperty, value);
    }

    public double NameColumnWidth
    {
        get => (double)GetValue(NameColumnWidthProperty);
        set => SetValue(NameColumnWidthProperty, value);
    }

    public VestigiumPropertyItem? SelectedProperty
    {
        get => (VestigiumPropertyItem?)GetValue(SelectedPropertyProperty);
        set => SetValue(SelectedPropertyProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public int MaxExpandDepth
    {
        get => (int)GetValue(MaxExpandDepthProperty);
        set => SetValue(MaxExpandDepthProperty, value);
    }

    public TextAlignment EditorTextAlignment
    {
        get => (TextAlignment)GetValue(EditorTextAlignmentProperty);
        set => SetValue(EditorTextAlignmentProperty, value);
    }

    public IList? CategoryIcons
    {
        get => (IList?)GetValue(CategoryIconsProperty);
        set => SetValue(CategoryIconsProperty, value);
    }

    public TextAlignment CategoryTextAlignment
    {
        get => (TextAlignment)GetValue(CategoryTextAlignmentProperty);
        set => SetValue(CategoryTextAlignmentProperty, value);
    }

    public bool IsCategoryBold
    {
        get => (bool)GetValue(IsCategoryBoldProperty);
        set => SetValue(IsCategoryBoldProperty, value);
    }

    public Orientation CategoryOrientation
    {
        get => (Orientation)GetValue(CategoryOrientationProperty);
        set => SetValue(CategoryOrientationProperty, value);
    }

    public string ObjectCountText
    {
        get => (string)GetValue(ObjectCountTextProperty);
        private set => SetValue(ObjectCountTextProperty, value);
    }

    public event RoutedPropertyChangedEventHandler<object> PropertyValueChanged
    {
        add => AddHandler(PropertyValueChangedEvent, value);
        remove => RemoveHandler(PropertyValueChangedEvent, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_splitter is not null)
            _splitter.DragDelta -= OnSplit;
        if (_list is not null)
        {
            _list.SelectionChanged -= OnListSelection;
            _list.MouseDoubleClick -= OnListDoubleClick;
        }
        _list = GetTemplateChild("PART_List") as ListBox;
        _splitter = GetTemplateChild("PART_Splitter") as Thumb;
        if (_splitter is not null)
            _splitter.DragDelta += OnSplit;
        if (_list is not null)
        {
            _list.SelectionChanged += OnListSelection;
            _list.MouseDoubleClick += OnListDoubleClick;
            _list.ItemsSource = _engine.VisibleRows;
        }
        ApplySource();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (SelectedProperty is null) return;
        if (e.Key is Key.Left or Key.Right)
        {
            if (SelectedProperty.CanExpand || SelectedProperty.IsCategory)
            {
                var want = e.Key == Key.Right;
                if (SelectedProperty.IsExpanded != want)
                    _engine.ToggleExpand(SelectedProperty);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Delete)
        {
            var parent = SelectedProperty.Parent;
            if (parent is { Kind: VestigiumPropertyEditorKind.Collection })
            {
                _engine.TryRemoveSelected(parent);
                e.Handled = true;
            }
        }
    }

    private void OnSplit(object sender, DragDeltaEventArgs e) =>
        NameColumnWidth = Math.Clamp(NameColumnWidth + e.HorizontalChange, 80, 480);

    private void OnListSelection(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        SelectedProperty = _list?.SelectedItem as VestigiumPropertyItem;
        _engine.SelectedProperty = SelectedProperty;
    }

    private void OnListDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SelectedProperty is { CanExpand: true } or { IsCategory: true })
            _engine.ToggleExpand(SelectedProperty);
    }

    private void RefreshList()
    {
        if (_list is null) return;
        _syncing = true;
        _list.ItemsSource = null;
        _list.ItemsSource = _engine.VisibleRows;
        if (SelectedProperty is not null && _engine.VisibleRows.Contains(SelectedProperty))
            _list.SelectedItem = SelectedProperty;
        _syncing = false;
        CommandManager.InvalidateRequerySuggested();
    }

    private void StartDrain()
    {
        _drain ??= new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(_drainMs)
        };
        _drain.Tick -= OnDrain;
        _drain.Tick += OnDrain;
        _drain.Start();
    }

    private void StopDrain()
    {
        if (_drain is null) return;
        _drain.Stop();
        _drain.Tick -= OnDrain;
        _drain = null;
    }

    private void OnDrain(object? sender, EventArgs e)
    {
        if (_flushQueued)
        {
            _flushQueued = false;
            _engine.RefreshValues();
            _drainMs = Math.Min(250, _drainMs + 25);
        }
        else
        {
            _drainMs = Math.Max(50, _drainMs - 25);
        }
        if (_drain is not null)
            _drain.Interval = TimeSpan.FromMilliseconds(_drainMs);
    }

    private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumPropertiesGrid)d).ApplySource();

    private static void OnSelectedObjectsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumPropertiesGrid)d).ApplySource();

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumPropertiesGrid)d).ApplySource();

    private static void OnSortChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumPropertiesGrid)d)._engine.Sort = (VestigiumPropertySort)e.NewValue;

    private static void OnSearchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumPropertiesGrid)d)._engine.SearchText = e.NewValue as string ?? string.Empty;

    private static void OnSelectedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (VestigiumPropertiesGrid)d;
        grid._engine.SelectedProperty = e.NewValue as VestigiumPropertyItem;
        if (grid._list is not null && !grid._syncing)
            grid._list.SelectedItem = e.NewValue;
    }

    private static void OnReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (VestigiumPropertiesGrid)d;
        grid._engine.IsReadOnly = e.NewValue is true;
        grid._engine.Rebuild();
    }

    private static void OnMaxDepthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((VestigiumPropertiesGrid)d)._engine.MaxExpandDepth = (int)e.NewValue;

    private static object CoerceMaxDepth(DependencyObject d, object baseValue) =>
        PropertyRules.CoerceMaxDepth(baseValue is int n ? n : PropertyRules.DefaultMaxExpandDepth);

    private static void OnEditorTextAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (VestigiumPropertiesGrid)d;
        grid._engine.EditorTextAlignment = (TextAlignment)e.NewValue;
        grid._engine.ApplyAlignment();
    }

    private static object CoerceEditorTextAlignment(DependencyObject d, object baseValue) =>
        PropertyRules.CoerceTextAlignment(baseValue is TextAlignment t ? t : TextAlignment.Left);

    private static void OnCategoryIconsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (VestigiumPropertiesGrid)d;
        if (e.OldValue is INotifyCollectionChanged oldNcc)
            oldNcc.CollectionChanged -= grid.OnCategoryIconsCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newNcc)
            newNcc.CollectionChanged += grid.OnCategoryIconsCollectionChanged;
        grid._engine.CategoryIcons = e.NewValue as IList;
        grid._engine.ApplyCategoryIcons();
    }

    private void OnCategoryIconsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _engine.CategoryIcons = CategoryIcons;
        _engine.ApplyCategoryIcons();
    }

    private void ApplySource()
    {
        _engine.EditorTextAlignment = EditorTextAlignment;
        _engine.CategoryIcons = CategoryIcons;
        if (ItemsSource is not null)
        {
            _engine.SetItemsSource(ItemsSource);
            ObjectCountText = string.Empty;
            return;
        }
        if (SelectedObjects is { Count: > 0 } list)
        {
            _engine.SetSelectedObjects(list);
            ObjectCountText = list.Count > 1 ? $"{list.Count} objects" : string.Empty;
            return;
        }
        if (SelectedObject is not null)
        {
            _engine.SetSelectedObjects(new[] { SelectedObject });
            ObjectCountText = string.Empty;
            return;
        }
        _engine.Clear();
        ObjectCountText = string.Empty;
    }
}
