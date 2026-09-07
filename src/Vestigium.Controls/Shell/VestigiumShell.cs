using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Controls.StatusBar;

namespace Vestigium.Controls.Shell;

[TemplatePart(Name = "PART_Nav", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PART_ContentHost", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_Status", Type = typeof(FrameworkElement))]
public class VestigiumShell : Control
{
    static VestigiumShell()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(VestigiumShell),
            new FrameworkPropertyMetadata(typeof(VestigiumShell)));
    }

    private bool _showStatusBarExplicit;
    private bool _seeding;
    private bool _suppressExplicit;
    private ResourceDictionary? _mergedTheme;
    private VestigiumNavItem? _listen;

    public VestigiumShell()
    {
        SetCurrentValue(NavItemsProperty, new ObservableCollection<VestigiumNavItem>());
        SetCurrentValue(StatusProperty, CreateOwnedStatus());
        SelectItemCommand = new RelayCommand<VestigiumNavItem>(SelectItem, item => item is { IsEnabled: true });
        SeedDefaultsIfEmpty();
        SelectedItem = NavItems.OfType<VestigiumNavItem>().FirstOrDefault();
        if (SelectedItem is not null)
            SelectedItem.IsSelected = true;
        GroupId = $"nav_{GetHashCode():x}";
    }

    public ICommand SelectItemCommand { get; }

    public string GroupId { get; }

    public static readonly DependencyProperty NavItemsProperty =
        DependencyProperty.Register(nameof(NavItems), typeof(IList), typeof(VestigiumShell),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, OnNavItemsChanged));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(VestigiumNavItem), typeof(VestigiumShell),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(nameof(Content), typeof(object), typeof(VestigiumShell),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, OnContentChanged));

    public static readonly DependencyProperty DisplayContentProperty =
        DependencyProperty.Register(nameof(DisplayContent), typeof(object), typeof(VestigiumShell),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ShowNavProperty =
        DependencyProperty.Register(nameof(ShowNav), typeof(bool), typeof(VestigiumShell),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowStatusBarProperty =
        DependencyProperty.Register(nameof(ShowStatusBar), typeof(bool), typeof(VestigiumShell),
            new FrameworkPropertyMetadata(true, OnShowStatusBarChanged));

    public static readonly DependencyProperty IsSubShellProperty =
        DependencyProperty.Register(nameof(IsSubShell), typeof(bool), typeof(VestigiumShell),
            new PropertyMetadata(false, OnIsSubShellChanged));

    public static readonly DependencyProperty ShellDepthProperty =
        DependencyProperty.Register(nameof(ShellDepth), typeof(int), typeof(VestigiumShell),
            new PropertyMetadata(0, null, CoerceShellDepth));

    public static readonly DependencyProperty MaxNavDepthProperty =
        DependencyProperty.Register(nameof(MaxNavDepth), typeof(int), typeof(VestigiumShell),
            new PropertyMetadata(ShellRules.DefaultMaxNavDepth, OnMaxNavDepthChanged, CoerceMaxNavDepth));

    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(VestigiumStatusBarViewModel), typeof(VestigiumShell),
            new PropertyMetadata(null));

    public static readonly DependencyProperty StatusBarPositionProperty =
        DependencyProperty.Register(nameof(StatusBarPosition), typeof(VestigiumStatusBarPosition), typeof(VestigiumShell),
            new FrameworkPropertyMetadata(VestigiumStatusBarPosition.Bottom, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ThemeResourcesProperty =
        DependencyProperty.Register(nameof(ThemeResources), typeof(ResourceDictionary), typeof(VestigiumShell),
            new PropertyMetadata(null, OnThemeResourcesChanged));

    public static readonly DependencyProperty Level0ItemsProperty =
        DependencyProperty.Register(nameof(Level0Items), typeof(IList), typeof(VestigiumShell),
            new PropertyMetadata(null));

    public static readonly DependencyProperty Level1ItemsProperty =
        DependencyProperty.Register(nameof(Level1Items), typeof(IList), typeof(VestigiumShell),
            new PropertyMetadata(null));

    public static readonly DependencyProperty Level2ItemsProperty =
        DependencyProperty.Register(nameof(Level2Items), typeof(IList), typeof(VestigiumShell),
            new PropertyMetadata(null));

    public static readonly DependencyProperty Level0SelectedProperty =
        DependencyProperty.Register(nameof(Level0Selected), typeof(VestigiumNavItem), typeof(VestigiumShell),
            new PropertyMetadata(null));

    public static readonly DependencyProperty Level1SelectedProperty =
        DependencyProperty.Register(nameof(Level1Selected), typeof(VestigiumNavItem), typeof(VestigiumShell),
            new PropertyMetadata(null));

    public IList NavItems
    {
        get => (IList)GetValue(NavItemsProperty);
        set => SetValue(NavItemsProperty, value);
    }

    public VestigiumNavItem? SelectedItem
    {
        get => (VestigiumNavItem?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public object? DisplayContent
    {
        get => GetValue(DisplayContentProperty);
        private set => SetValue(DisplayContentProperty, value);
    }

    public bool ShowNav
    {
        get => (bool)GetValue(ShowNavProperty);
        set => SetValue(ShowNavProperty, value);
    }

    public bool ShowStatusBar
    {
        get => (bool)GetValue(ShowStatusBarProperty);
        set => SetValue(ShowStatusBarProperty, value);
    }

    public bool IsSubShell
    {
        get => (bool)GetValue(IsSubShellProperty);
        set => SetValue(IsSubShellProperty, value);
    }

    public int ShellDepth
    {
        get => (int)GetValue(ShellDepthProperty);
        set => SetValue(ShellDepthProperty, value);
    }

    public int MaxNavDepth
    {
        get => (int)GetValue(MaxNavDepthProperty);
        set => SetValue(MaxNavDepthProperty, value);
    }

    public VestigiumStatusBarViewModel Status
    {
        get => (VestigiumStatusBarViewModel)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public VestigiumStatusBarPosition StatusBarPosition
    {
        get => (VestigiumStatusBarPosition)GetValue(StatusBarPositionProperty);
        set => SetValue(StatusBarPositionProperty, value);
    }

    public ResourceDictionary? ThemeResources
    {
        get => (ResourceDictionary?)GetValue(ThemeResourcesProperty);
        set => SetValue(ThemeResourcesProperty, value);
    }

    public IList? Level0Items => (IList?)GetValue(Level0ItemsProperty);
    public IList? Level1Items => (IList?)GetValue(Level1ItemsProperty);
    public IList? Level2Items => (IList?)GetValue(Level2ItemsProperty);
    public VestigiumNavItem? Level0Selected => (VestigiumNavItem?)GetValue(Level0SelectedProperty);
    public VestigiumNavItem? Level1Selected => (VestigiumNavItem?)GetValue(Level1SelectedProperty);

    public VestigiumNavItem? this[string name] => Find(NavItems, name);

    public static VestigiumShell Stage(VestigiumShellSpec? spec = null)
    {
        var shell = new VestigiumShell();
        shell.ApplySpec(spec);
        return shell;
    }

    public void ApplySpec(VestigiumShellSpec? spec)
    {
        if (spec is null)
        {
            SeedDefaultsIfEmpty(force: true);
            RefreshTree();
            return;
        }

        IsSubShell = spec.IsSubShell;
        if (spec.IsSubShell && spec.ShellDepth == 0)
            ShellDepth = 1;
        else
            ShellDepth = spec.ShellDepth;

        if (spec.ShowStatusBar is bool show)
            ShowStatusBar = show;
        else if (spec.IsSubShell && !_showStatusBarExplicit)
            ShowStatusBar = false;

        if (spec.ThemeResources is not null)
            ThemeResources = spec.ThemeResources;

        var items = BuildItems(spec.Items, depth: 1);
        if (items.Count == 0)
            SeedDefaultsIfEmpty(force: true);
        else
            ReplaceNavItems(items);
        RefreshTree();
    }

    public void SeedDefaults() => SeedDefaultsIfEmpty(force: true);

    private static VestigiumStatusBarViewModel CreateOwnedStatus()
    {
        var status = new VestigiumStatusBarViewModel();
        status.Message = "Ready";
        status.Position = VestigiumStatusBarPosition.Bottom;
        return status;
    }

    private void SeedDefaultsIfEmpty(bool force = false)
    {
        if (_seeding) return;
        var list = NavItems;
        if (!force && list is { Count: > 0 })
            return;
        _seeding = true;
        try
        {
            ReplaceNavItems(ShellRules.DefaultHeaders.Select(h => new VestigiumNavItem(h)).ToList());
        }
        finally
        {
            _seeding = false;
        }
    }

    private void ReplaceNavItems(IList<VestigiumNavItem> items)
    {
        ObservableCollection<VestigiumNavItem> target;
        if (NavItems is ObservableCollection<VestigiumNavItem> existing)
        {
            existing.Clear();
            foreach (var item in items)
                existing.Add(item);
            target = existing;
        }
        else
        {
            target = new ObservableCollection<VestigiumNavItem>(items);
            SetCurrentValue(NavItemsProperty, target);
        }

        SelectedItem = target.FirstOrDefault();
        if (SelectedItem is not null)
            SelectedItem.IsSelected = true;
    }

    private List<VestigiumNavItem> BuildItems(IList<VestigiumNavItemSpec> specs, int depth)
    {
        var list = new List<VestigiumNavItem>();
        if (depth > MaxNavDepth)
            return list;
        foreach (var spec in specs)
        {
            var item = new VestigiumNavItem(spec.Header);
            if (!string.IsNullOrWhiteSpace(spec.Key))
                item.Key = spec.Key.Trim();
            item.SetPlaceholder(spec.Title, spec.Subject, spec.Description, spec.Image, spec.ImageUri);
            if (spec.Content is not null)
                item.Content = spec.Content;
            if (spec.Children.Count > 0 && depth < MaxNavDepth)
            {
                foreach (var child in BuildItems(spec.Children, depth + 1))
                    item.Children.Add(child);
            }
            list.Add(item);
        }
        return list;
    }

    private void SelectItem(VestigiumNavItem? item)
    {
        if (item is null || !item.IsEnabled)
            return;
        SelectedItem = item;
        item.Command?.Execute(item);
    }

    private void RefreshTree()
    {
        var roots = Clip(NavItems, 1);
        SetValue(Level0ItemsProperty, roots);

        var rootSelected = roots.FirstOrDefault(i => i.IsSelected) ?? SelectedIn(roots, SelectedItem) ?? roots.FirstOrDefault();
        SetValue(Level0SelectedProperty, rootSelected);

        var level1 = rootSelected is not null ? Clip(rootSelected.Children, 2) : new List<VestigiumNavItem>();
        SetValue(Level1ItemsProperty, level1);
        var childSelected = level1.FirstOrDefault(i => i.IsSelected) ?? SelectedIn(level1, SelectedItem);
        SetValue(Level1SelectedProperty, childSelected);

        var level2 = childSelected is not null ? Clip(childSelected.Children, 3) : new List<VestigiumNavItem>();
        SetValue(Level2ItemsProperty, level2);

        ResolveDisplay();
    }

    private List<VestigiumNavItem> Clip(IList? source, int depth)
    {
        var list = new List<VestigiumNavItem>();
        if (source is null || depth > MaxNavDepth)
            return list;
        foreach (var raw in source)
        {
            if (raw is VestigiumNavItem item)
                list.Add(item);
        }
        return list;
    }

    private static VestigiumNavItem? SelectedIn(IEnumerable<VestigiumNavItem> items, VestigiumNavItem? current)
    {
        if (current is null)
            return null;
        foreach (var item in items)
        {
            if (ReferenceEquals(item, current) || Contains(item, current))
                return item;
        }
        return null;
    }

    private static bool Contains(VestigiumNavItem parent, VestigiumNavItem needle)
    {
        foreach (var child in parent.Children)
        {
            if (ReferenceEquals(child, needle) || Contains(child, needle))
                return true;
        }
        return false;
    }

    private void ResolveDisplay()
    {
        if (Content is not null)
        {
            DisplayContent = Content;
            return;
        }
        DisplayContent = SelectedItem?.DisplayContent ?? NavItems.OfType<VestigiumNavItem>().FirstOrDefault()?.DisplayContent;
    }

    private void SyncSelection(VestigiumNavItem? item)
    {
        foreach (var root in NavItems.OfType<VestigiumNavItem>())
            ClearSelection(root);
        if (item is null)
        {
            Hook(null);
            RefreshTree();
            return;
        }
        MarkPath(NavItems.OfType<VestigiumNavItem>(), item);
        Hook(item);
        RefreshTree();
    }

    private static void ClearSelection(VestigiumNavItem item)
    {
        item.IsSelected = false;
        foreach (var child in item.Children)
            ClearSelection(child);
    }

    private static bool MarkPath(IEnumerable<VestigiumNavItem> items, VestigiumNavItem target)
    {
        foreach (var item in items)
        {
            if (ReferenceEquals(item, target))
            {
                item.IsSelected = true;
                return true;
            }
            if (MarkPath(item.Children, target))
            {
                item.IsSelected = true;
                return true;
            }
        }
        return false;
    }

    private void Hook(VestigiumNavItem? item)
    {
        if (_listen is not null)
            _listen.PropertyChanged -= OnSelectedNavPropertyChanged;
        _listen = item;
        if (_listen is not null)
            _listen.PropertyChanged += OnSelectedNavPropertyChanged;
    }

    private void OnSelectedNavPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(VestigiumNavItem.Content) or nameof(VestigiumNavItem.DisplayContent))
            ResolveDisplay();
        if (e.PropertyName is nameof(VestigiumNavItem.Children) or nameof(VestigiumNavItem.IsSelected))
            RefreshTree();
    }

    public static VestigiumNavItem? Find(IList? items, string? name)
    {
        var key = ShellRules.NormalizeKey(name);
        if (key.Length == 0 || items is null)
            return null;
        foreach (var raw in items)
        {
            if (raw is not VestigiumNavItem item)
                continue;
            if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Header, key, StringComparison.OrdinalIgnoreCase))
                return item;
            var child = Find(item.Children, key);
            if (child is not null)
                return child;
        }
        return null;
    }

    private static void OnNavItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not VestigiumShell shell)
            return;
        if (e.OldValue is INotifyCollectionChanged oldN)
            oldN.CollectionChanged -= shell.OnNavCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newN)
            newN.CollectionChanged += shell.OnNavCollectionChanged;
        if (shell.NavItems is null || shell.NavItems.Count == 0)
            shell.SeedDefaultsIfEmpty();
        shell.RefreshTree();
    }

    private void OnNavCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshTree();

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VestigiumShell shell)
            shell.SyncSelection(e.NewValue as VestigiumNavItem);
    }

    private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VestigiumShell shell)
            shell.ResolveDisplay();
    }

    private static void OnShowStatusBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VestigiumShell shell && !shell._suppressExplicit)
            shell._showStatusBarExplicit = true;
    }

    private static void OnIsSubShellChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not VestigiumShell shell)
            return;
        if (e.NewValue is true)
        {
            if (shell.ShellDepth == 0)
                shell.SetCurrentValue(ShellDepthProperty, 1);
            if (!shell._showStatusBarExplicit)
            {
                shell._suppressExplicit = true;
                shell.SetCurrentValue(ShowStatusBarProperty, false);
                shell._suppressExplicit = false;
            }
        }
    }

    private static void OnMaxNavDepthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VestigiumShell shell)
            shell.RefreshTree();
    }

    private static object CoerceMaxNavDepth(DependencyObject d, object baseValue) =>
        ShellRules.CoerceMaxNavDepth(baseValue is int i ? i : ShellRules.DefaultMaxNavDepth);

    private static object CoerceShellDepth(DependencyObject d, object baseValue) =>
        ShellRules.CoerceShellDepth(baseValue is int i ? i : 0);

    private static void OnThemeResourcesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not VestigiumShell shell)
            return;
        if (shell._mergedTheme is not null && shell.Resources.MergedDictionaries.Contains(shell._mergedTheme))
            shell.Resources.MergedDictionaries.Remove(shell._mergedTheme);
        shell._mergedTheme = e.NewValue as ResourceDictionary;
        if (shell._mergedTheme is not null)
            shell.Resources.MergedDictionaries.Add(shell._mergedTheme);
    }
}
