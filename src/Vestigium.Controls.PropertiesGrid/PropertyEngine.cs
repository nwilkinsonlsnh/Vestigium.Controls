using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace Vestigium.Controls.PropertiesGrid;

public sealed class PropertyEngine
{
    private readonly List<VestigiumPropertyItem> _roots = [];
    private readonly List<VestigiumPropertyItem> _visible = [];
    private readonly List<object> _targets = [];
    private readonly HashSet<string> _collapsedCats = [];
    private readonly HashSet<string> _expandedPaths = [];
    private readonly HashSet<INotifyPropertyChanged> _inpc = [];
    private readonly HashSet<INotifyCollectionChanged> _incc = [];
    private readonly Dictionary<string, VestigiumPropertyItem> _categoryRows = new(StringComparer.Ordinal);
    private bool _hostMode;
    private string _typeStamp = "";
    private string _search = "";
    private VestigiumPropertySort _sort = VestigiumPropertySort.Categorized;

    public IReadOnlyList<VestigiumPropertyItem> RootItems => _roots;
    public IReadOnlyList<VestigiumPropertyItem> VisibleRows => _visible;

    public VestigiumPropertyItem? SelectedProperty { get; set; }
    public bool IsReadOnly { get; set; }
    public int MaxExpandDepth { get; set; } = PropertyRules.DefaultMaxExpandDepth;
    public TextAlignment EditorTextAlignment { get; set; } = TextAlignment.Left;
    public IList? CategoryIcons { get; set; }

    public VestigiumPropertySort Sort
    {
        get => _sort;
        set
        {
            if (_sort == value) return;
            _sort = value;
            RebuildVisible();
        }
    }

    public string SearchText
    {
        get => _search;
        set
        {
            value ??= string.Empty;
            if (_search == value) return;
            _search = value;
            RebuildVisible();
        }
    }

    public event EventHandler? RowsChanged;
    public event EventHandler<VestigiumPropertyValueChangedEventArgs>? ValueChanged;
    public event EventHandler? NeedsRefresh;

    public void SetSelectedObjects(IEnumerable? sources)
    {
        _hostMode = false;
        UnsubscribeAll();
        _roots.Clear();
        _targets.Clear();
        if (sources is not null)
        {
            foreach (var item in sources)
            {
                if (item is not null)
                    _targets.Add(item);
            }
        }

        var stamp = string.Join(';', _targets.Select(t => t.GetType().FullName).Distinct().OrderBy(s => s, StringComparer.Ordinal));
        if (!string.Equals(stamp, _typeStamp, StringComparison.Ordinal))
        {
            _expandedPaths.Clear();
            _collapsedCats.Clear();
            _typeStamp = stamp;
        }

        if (_targets.Count > 0)
        {
            SubscribeTargets(_targets);
            var ancestors = new HashSet<object>(_targets);
            _roots.AddRange(Merge(_targets, 0, "", ancestors));
            RestoreExpanded(_roots);
        }

        RebuildVisible();
    }

    public void SetItemsSource(IEnumerable? items)
    {
        _hostMode = true;
        UnsubscribeAll();
        _roots.Clear();
        _targets.Clear();
        _typeStamp = "#items";
        if (items is not null)
        {
            foreach (var item in items)
            {
                if (item is not VestigiumPropertyItem row) continue;
                row.Engine = this;
                row.IsReadOnly = IsReadOnly || row.IsReadOnly;
                row.EffectiveTextAlignment = row.TextAlignment ?? EditorTextAlignment;
                _roots.Add(row);
            }
        }
        RebuildVisible();
    }

    public void Clear()
    {
        UnsubscribeAll();
        _roots.Clear();
        _targets.Clear();
        _visible.Clear();
        _typeStamp = "";
        SelectedProperty = null;
        RaiseRows();
    }

    public void Rebuild()
    {
        if (_hostMode)
        {
            foreach (var row in _roots)
                row.IsReadOnly = IsReadOnly || row.IsReadOnly;
            RebuildVisible();
            return;
        }

        var snapshot = _targets.ToList();
        SetSelectedObjects(snapshot);
    }

    public void RefreshValues() => RefreshTree(_roots);

    public bool TryCommit(VestigiumPropertyItem item, object? value)
    {
        if (IsReadOnly || item.IsReadOnly || item.IsCategory) return false;

        if (item.List is not null && item.Index >= 0 && item.Descriptor is null)
        {
            var elementType = item.PropertyType ?? item.List[item.Index]?.GetType() ?? typeof(object);
            if (!PropertyRules.TryChangeType(value, elementType, out var converted)) return false;
            var old = item.List[item.Index];
            if (ValuesEqual(old, converted)) return false;
            item.List[item.Index] = converted;
            item.Value = converted;
            item.IsMixed = false;
            RaiseValue(item.Path, old, converted, item.Owners.Count > 0 ? item.Owners[0] : null);
            item.RefreshCommands();
            return true;
        }

        if (item.Descriptor is null)
        {
            var old = item.Value;
            object? converted = value;
            if (item.PropertyType is not null && !PropertyRules.TryChangeType(value, item.PropertyType, out converted))
                return false;
            if (ValuesEqual(old, converted)) return false;
            item.Value = converted;
            item.IsMixed = false;
            RaiseValue(item.Path, old, converted, null);
            item.RefreshCommands();
            return true;
        }

        var wrote = false;
        for (var i = 0; i < item.Owners.Count; i++)
        {
            var owner = item.Owners[i];
            var old = SafeGet(item.Descriptor, owner);
            if (!PropertyRules.TryChangeType(value, item.PropertyType ?? item.Descriptor.PropertyType, out var converted))
                return false;
            if (ValuesEqual(old, converted)) continue;
            try
            {
                item.Descriptor.SetValue(owner, converted);
            }
            catch
            {
                continue;
            }
            RaiseValue(item.Path, old, converted, owner);
            wrote = true;
        }

        if (!wrote) return false;
        item.Value = value is null ? null : SafeGet(item.Descriptor, item.Owners[0]);
        item.IsMixed = false;
        UpdateCanReset(item);
        item.RefreshCommands();
        return true;
    }

    public bool TryReset(VestigiumPropertyItem item)
    {
        if (!item.CanReset || !item.HasDefault) return false;
        return TryCommit(item, item.DefaultValue);
    }

    public void ResetAll()
    {
        foreach (var item in _roots.ToList())
        {
            if (item.CanReset)
                TryReset(item);
        }
    }

    public void ToggleExpand(VestigiumPropertyItem item)
    {
        if (item.IsCategory)
        {
            if (_collapsedCats.Contains(item.Category))
                _collapsedCats.Remove(item.Category);
            else
                _collapsedCats.Add(item.Category);
            RebuildVisible();
            return;
        }

        if (item.IsCircular)
        {
            item.Children ??= new List<VestigiumPropertyItem>();
            item.CanExpand = false;
            item.IsExpanded = false;
            RebuildVisible();
            return;
        }

        if (!item.CanExpand) return;
        item.IsExpanded = !item.IsExpanded;
        if (item.IsExpanded)
        {
            EnsureChildren(item);
            _expandedPaths.Add(item.Path);
        }
        else
        {
            _expandedPaths.Remove(item.Path);
        }
        item.RefreshCommands();
        RebuildVisible();
    }

    public bool CanAdd(VestigiumPropertyItem item) =>
        !IsReadOnly && !item.IsReadOnly && item.Kind == VestigiumPropertyEditorKind.Collection
        && item.Owners.Count <= 1 && item.List is { IsReadOnly: false, IsFixedSize: false };

    public bool CanRemove(VestigiumPropertyItem item) =>
        CanAdd(item) && SelectedChild(item) is { Index: >= 0 };

    public bool CanMove(VestigiumPropertyItem item, int delta)
    {
        if (!CanAdd(item) || item.List is null) return false;
        var child = SelectedChild(item);
        if (child is null) return false;
        var next = child.Index + delta;
        return next >= 0 && next < item.List.Count;
    }

    public bool TryAdd(VestigiumPropertyItem item)
    {
        if (item.List is null && item.Value is IList bound)
            item.List = bound;
        if (!CanAdd(item) || item.List is null) return false;
        var type = PropertyRules.ElementType(item.PropertyType ?? item.List.GetType(), item.List);
        var created = PropertyRules.CreateElement(type);
        if (created is null && type != typeof(string)) return false;
        try
        {
            item.List.Add(created);
        }
        catch
        {
            return false;
        }
        item.Value = item.List;
        InvalidateChildren(item);
        RebuildVisible();
        return true;
    }

    public bool TryRemoveSelected(VestigiumPropertyItem item)
    {
        if (!CanRemove(item) || item.List is null) return false;
        var child = SelectedChild(item);
        if (child is null) return false;
        try
        {
            item.List.RemoveAt(child.Index);
        }
        catch
        {
            return false;
        }
        if (ReferenceEquals(SelectedProperty, child))
            SelectedProperty = item;
        item.Value = item.List;
        InvalidateChildren(item);
        RebuildVisible();
        return true;
    }

    public bool TryMove(VestigiumPropertyItem item, int delta)
    {
        if (!CanMove(item, delta) || item.List is null) return false;
        var child = SelectedChild(item)!;
        var next = child.Index + delta;
        var tmp = item.List[child.Index];
        item.List[child.Index] = item.List[next];
        item.List[next] = tmp;
        SelectedProperty = null;
        InvalidateChildren(item);
        RebuildVisible();
        return true;
    }

    internal void EnsureChildren(VestigiumPropertyItem item)
    {
        if (item.Children is not null) return;
        item.Children = new List<VestigiumPropertyItem>();
        if (item.IsCircular) return;

        if (item.Kind == VestigiumPropertyEditorKind.Collection)
        {
            if (item.Owners.Count != 1) return;
            if (item.Value is not IList list) return;
            item.List = list;
            SubscribeCollection(list);
            var elType = PropertyRules.ElementType(item.PropertyType ?? list.GetType(), list);
            for (var i = 0; i < list.Count; i++)
                item.Children.Add(CreateCollectionChild(item, list, i, elType));
            return;
        }

        if (item.Kind != VestigiumPropertyEditorKind.Expandable || item.Descriptor is null)
            return;

        var nested = new List<object>();
        foreach (var owner in item.Owners)
        {
            var value = SafeGet(item.Descriptor, owner);
            if (value is null) return;
            nested.Add(value);
        }

        var ancestors = new HashSet<object>(item.Ancestors);
        foreach (var value in nested)
        {
            if (ancestors.Contains(value))
            {
                item.IsCircular = true;
                item.CanExpand = false;
                return;
            }
            ancestors.Add(value);
        }

        foreach (var child in Merge(nested, item.Depth + 1, item.Path, ancestors))
        {
            child.Parent = item;
            item.Children.Add(child);
        }
    }

    private VestigiumPropertyItem CreateCollectionChild(VestigiumPropertyItem parent, IList list, int index, Type elType)
    {
        var value = list[index];
        var expandable = value is not null && PropertyRules.IsExpandableType(value.GetType());
        var circular = expandable && parent.Ancestors.Contains(value!);
        var kind = expandable
            ? VestigiumPropertyEditorKind.Expandable
            : PropertyRules.Classify(elType, readOnly: false);
        var child = new VestigiumPropertyItem
        {
            Name = $"[{index}]",
            Category = parent.Category,
            Description = $"Item {index}",
            Path = $"{parent.Path}[{index}]",
            Depth = parent.Depth + 1,
            Kind = kind,
            IsReadOnly = IsReadOnly,
        };
        child.Engine = this;
        child.Parent = parent;
        child.List = list;
        child.Index = index;
        child.PropertyType = elType;
        child.Value = value;
        child.IsCircular = circular;
        child.CanExpand = expandable && !circular && child.Depth < MaxExpandDepth && value is not null;
        child.IsUnsigned = PropertyRules.IsUnsigned(elType);
        child.DecimalPlaces = PropertyRules.IsInteger(elType) ? 0 : 4;
        child.EffectiveTextAlignment = EditorTextAlignment;
        foreach (var a in parent.Ancestors)
            child.Ancestors.Add(a);
        if (value is not null)
        {
            child.Ancestors.Add(value);
            child.Owners.Add(value);
            if (value is INotifyPropertyChanged inpc)
                SubscribeInpc(inpc);
        }
        else
        {
            foreach (var owner in parent.Owners)
                child.Owners.Add(owner);
        }
        return child;
    }

    private List<VestigiumPropertyItem> Merge(IReadOnlyList<object> owners, int depth, string path, HashSet<object> ancestors)
    {
        var maps = new List<Dictionary<string, PropertyDescriptor>>();
        foreach (var owner in owners)
        {
            var map = new Dictionary<string, PropertyDescriptor>(StringComparer.Ordinal);
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(owner))
            {
                if (!pd.IsBrowsable || PropertyRules.IsIndexer(pd)) continue;
                map[pd.Name] = pd;
            }
            maps.Add(map);
        }

        var names = maps[0].Keys
            .Where(name => maps.All(m => m.ContainsKey(name) && PropertyRules.Compatible(maps[0][name].PropertyType, m[name].PropertyType)))
            .ToList();

        var rows = new List<VestigiumPropertyItem>();
        foreach (var name in names)
        {
            var pds = maps.Select(m => m[name]).ToList();
            rows.Add(CreateRow(pds, owners, depth, path, ancestors));
        }
        return rows;
    }

    private VestigiumPropertyItem CreateRow(
        IReadOnlyList<PropertyDescriptor> pds,
        IReadOnlyList<object> owners,
        int depth,
        string path,
        HashSet<object> ancestors)
    {
        var pd = pds[0];
        var values = new object?[owners.Count];
        for (var i = 0; i < owners.Count; i++)
            values[i] = SafeGet(pds[i], owners[i]);

        var mixed = values.Skip(1).Any(v => !ValuesEqual(v, values[0]));
        var type = pd.PropertyType;
        var attrReadOnly = pd.Attributes[typeof(ReadOnlyAttribute)] is ReadOnlyAttribute ra && ra.IsReadOnly;
        var kind = PropertyRules.Classify(type, attrReadOnly || (pd.IsReadOnly && !PropertyRules.IsCollection(type)));
        var defaultAttr = pd.Attributes[typeof(DefaultValueAttribute)] as DefaultValueAttribute;
        var value = mixed ? null : values[0];
        var circular = kind == VestigiumPropertyEditorKind.Expandable
            && value is not null
            && ancestors.Contains(value);

        var item = new VestigiumPropertyItem
        {
            Name = pd.DisplayName,
            Category = string.IsNullOrWhiteSpace(pd.Category) ? PropertyRules.MiscCategory : pd.Category,
            Description = pd.Description ?? string.Empty,
            Path = string.IsNullOrEmpty(path) ? pd.Name : $"{path}.{pd.Name}",
            Depth = depth,
            Kind = kind,
            IsReadOnly = IsReadOnly || attrReadOnly || (pd.IsReadOnly && kind != VestigiumPropertyEditorKind.Collection),
            Choices = type.IsEnum ? Enum.GetNames(PropertyRules.Unwrap(type)) : null,
        };
        item.Engine = this;
        item.Descriptor = pd;
        item.PropertyType = type;
        item.HasDefault = defaultAttr is not null;
        item.DefaultValue = defaultAttr?.Value;
        item.IsMixed = mixed;
        item.Value = value;
        item.IsCircular = circular;
        item.IsUnsigned = PropertyRules.IsUnsigned(type);
        item.DecimalPlaces = PropertyRules.IsInteger(type) ? 0 : 4;
        var alignAttr = pd.Attributes[typeof(VestigiumTextAlignmentAttribute)] as VestigiumTextAlignmentAttribute;
        item.TextAlignment = alignAttr?.Alignment;
        item.EffectiveTextAlignment = item.TextAlignment ?? EditorTextAlignment;
        foreach (var owner in owners)
            item.Owners.Add(owner);
        foreach (var a in ancestors)
            item.Ancestors.Add(a);

        if (kind == VestigiumPropertyEditorKind.Collection && value is IList list)
        {
            item.List = list;
            SubscribeCollection(list);
        }

        var canExpandCollection = kind == VestigiumPropertyEditorKind.Collection
            && owners.Count == 1
            && !mixed
            && value is IList
            && depth < MaxExpandDepth;
        var canExpandObject = kind == VestigiumPropertyEditorKind.Expandable
            && value is not null
            && !circular
            && !values.Any(v => v is null)
            && depth < MaxExpandDepth;
        item.CanExpand = canExpandCollection || canExpandObject;
        UpdateCanReset(item);
        return item;
    }

    private void RestoreExpanded(IEnumerable<VestigiumPropertyItem> items)
    {
        foreach (var item in items)
        {
            if (!_expandedPaths.Contains(item.Path) || !item.CanExpand) continue;
            item.IsExpanded = true;
            EnsureChildren(item);
            if (item.Children is not null)
                RestoreExpanded(item.Children);
        }
    }

    private void RebuildVisible()
    {
        _visible.Clear();
        _categoryRows.Clear();
        var search = _search.Trim();
        if (_sort == VestigiumPropertySort.Alphabetical)
        {
            foreach (var item in _roots.OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                if (Matches(item, search))
                    AddVisible(item, search);
            }
        }
        else
        {
            var cats = _roots.Select(r => r.Category).Distinct().OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase);
            foreach (var cat in cats)
            {
                var items = _roots
                    .Where(r => r.Category == cat && Matches(r, search))
                    .OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
                if (items.Count == 0) continue;
                var header = new VestigiumPropertyItem
                {
                    Name = cat.ToUpperInvariant(),
                    Category = cat,
                    Path = $"#cat:{cat}",
                    IsCategory = true,
                    Kind = VestigiumPropertyEditorKind.ReadOnly,
                    IsReadOnly = true,
                    CanExpand = true,
                    IsExpanded = !_collapsedCats.Contains(cat),
                    EffectiveTextAlignment = EditorTextAlignment,
                };
                header.Engine = this;
                ApplyCategoryIcon(header);
                _categoryRows[cat] = header;
                _visible.Add(header);
                if (!header.IsExpanded) continue;
                foreach (var item in items)
                    AddVisible(item, search);
            }
        }
        RaiseRows();
    }

    private void AddVisible(VestigiumPropertyItem item, string search)
    {
        if (!string.IsNullOrEmpty(search) && item.CanExpand)
        {
            item.IsExpanded = true;
            EnsureChildren(item);
        }
        _visible.Add(item);
        if (!item.IsExpanded) return;
        EnsureChildren(item);
        if (item.Children is null) return;
        foreach (var child in item.Children)
        {
            if (string.IsNullOrEmpty(search) || Matches(child, search))
                AddVisible(child, search);
        }
    }

    public void ApplyAlignment()
    {
        foreach (var item in Walk(_roots))
            item.EffectiveTextAlignment = item.TextAlignment ?? EditorTextAlignment;
        foreach (var header in _categoryRows.Values)
            header.EffectiveTextAlignment = EditorTextAlignment;
    }

    public void ApplyCategoryIcons() => RebuildVisible();

    private void ApplyCategoryIcon(VestigiumPropertyItem header)
    {
        header.CategoryImage = null;
        header.CategoryGeometry = null;
        var icon = PropertyRules.FindCategoryIcon(CategoryIcons, header.Category);
        if (icon is null) return;
        header.CategoryImage = icon.ResolveImage();
        header.CategoryGeometry = header.CategoryImage is null ? icon.ResolveGeometry() : null;
        if (header.CategoryGeometry is { CanFreeze: true } geo)
            geo.Freeze();
    }

    private bool Matches(VestigiumPropertyItem item, string search)
    {
        if (string.IsNullOrEmpty(search)) return true;
        if (Contains(item.Name, search) || Contains(item.Category, search) || Contains(item.Description, search) || Contains(item.Path, search))
            return true;
        if (!item.CanExpand && item.Children is null) return false;
        EnsureChildren(item);
        return item.Children?.Any(c => Matches(c, search)) == true;
    }

    private static bool Contains(string text, string search) =>
        text.Contains(search, StringComparison.CurrentCultureIgnoreCase);

    private void RefreshTree(IEnumerable<VestigiumPropertyItem> items)
    {
        foreach (var item in items)
        {
            if (item.IsCategory || item.Descriptor is null || item.Owners.Count == 0)
            {
                if (item.Kind == VestigiumPropertyEditorKind.Collection && item.List is not null)
                    item.Value = item.List;
                if (item.Children is not null)
                    RefreshTree(item.Children);
                continue;
            }

            var values = item.Owners.Select(o => SafeGet(item.Descriptor, o)).ToList();
            var mixed = values.Skip(1).Any(v => !ValuesEqual(v, values[0]));
            item.IsMixed = mixed;
            item.Value = mixed ? null : values[0];
            if (item.Kind == VestigiumPropertyEditorKind.Collection && item.Value is IList list)
                item.List = list;
            UpdateCanReset(item);
            item.RefreshCommands();
            if (item.Children is not null)
                RefreshTree(item.Children);
        }
    }

    private void UpdateCanReset(VestigiumPropertyItem item)
    {
        item.CanReset = item.HasDefault && !item.IsMixed && !IsReadOnly && !item.IsReadOnly
            && !ValuesEqual(item.Value, item.DefaultValue);
    }

    private VestigiumPropertyItem? SelectedChild(VestigiumPropertyItem collection)
    {
        if (SelectedProperty is null) return null;
        if (ReferenceEquals(SelectedProperty.Parent, collection)) return SelectedProperty;
        if (SelectedProperty.List is not null && ReferenceEquals(SelectedProperty.List, collection.List) && SelectedProperty.Index >= 0)
            return SelectedProperty;
        return null;
    }

    private void InvalidateChildren(VestigiumPropertyItem item)
    {
        item.Children = null;
        if (item.IsExpanded)
            EnsureChildren(item);
        item.RefreshCommands();
    }

    private void SubscribeTargets(IEnumerable<object> targets)
    {
        foreach (var target in targets)
        {
            if (target is INotifyPropertyChanged inpc)
                SubscribeInpc(inpc);
        }
    }

    private void SubscribeInpc(INotifyPropertyChanged inpc)
    {
        if (!_inpc.Add(inpc)) return;
        inpc.PropertyChanged += OnTargetPropertyChanged;
    }

    private void SubscribeCollection(object list)
    {
        if (list is not INotifyCollectionChanged ncc) return;
        if (!_incc.Add(ncc)) return;
        ncc.CollectionChanged += OnCollectionChanged;
    }

    private void UnsubscribeAll()
    {
        foreach (var inpc in _inpc)
            inpc.PropertyChanged -= OnTargetPropertyChanged;
        _inpc.Clear();
        foreach (var ncc in _incc)
            ncc.CollectionChanged -= OnCollectionChanged;
        _incc.Clear();
    }

    private void OnTargetPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        NeedsRefresh?.Invoke(this, EventArgs.Empty);

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var item in Walk(_roots))
        {
            if (item.Kind == VestigiumPropertyEditorKind.Collection && ReferenceEquals(item.List, sender))
            {
                item.Value = item.List;
                InvalidateChildren(item);
            }
        }
        RebuildVisible();
    }

    private static IEnumerable<VestigiumPropertyItem> Walk(IEnumerable<VestigiumPropertyItem> items)
    {
        foreach (var item in items)
        {
            yield return item;
            if (item.Children is null) continue;
            foreach (var child in Walk(item.Children))
                yield return child;
        }
    }

    private void RaiseRows() => RowsChanged?.Invoke(this, EventArgs.Empty);

    private void RaiseValue(string path, object? oldValue, object? newValue, object? target) =>
        ValueChanged?.Invoke(this, new VestigiumPropertyValueChangedEventArgs(path, oldValue, newValue, target));

    private static object? SafeGet(PropertyDescriptor pd, object owner)
    {
        try { return pd.GetValue(owner); }
        catch { return null; }
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (Equals(a, b)) return true;
        if (a is null || b is null) return false;
        if (PropertyRules.IsNumeric(a.GetType()) && PropertyRules.IsNumeric(b.GetType()))
        {
            try { return Convert.ToDecimal(a) == Convert.ToDecimal(b); }
            catch { return false; }
        }
        return false;
    }
}
