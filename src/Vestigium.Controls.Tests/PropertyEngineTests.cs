using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Vestigium.Controls.PropertiesGrid;

namespace Vestigium.Controls.Tests;

public class PropertyEngineTests
{
    [Fact]
    public void Browsable_false_is_omitted()
    {
        var engine = EngineOf(new Probe());
        Assert.DoesNotContain(engine.RootItems, r => r.Name == "Id");
    }

    [Fact]
    public void Missing_category_is_Misc()
    {
        var engine = EngineOf(new Probe());
        Assert.Contains(engine.RootItems, r => r.Name == "Notes" && r.Category == "Misc");
    }

    [Fact]
    public void DisplayName_is_the_row_label()
    {
        var engine = EngineOf(new Probe());
        Assert.Contains(engine.RootItems, r => r.Name == "Display name");
    }

    [Fact]
    public void No_setter_is_read_only()
    {
        var engine = EngineOf(new Probe());
        var row = engine.RootItems.Single(r => r.Name == "Kind");
        Assert.Equal(VestigiumPropertyEditorKind.ReadOnly, row.Kind);
        Assert.True(row.IsReadOnly);
    }

    [Fact]
    public void Enum_kind_has_names()
    {
        var engine = EngineOf(new Probe());
        var row = engine.RootItems.Single(r => r.Name == "Protocol");
        Assert.Equal(VestigiumPropertyEditorKind.Enum, row.Kind);
        Assert.Contains("Icmp", row.Choices!);
    }

    [Fact]
    public void Int_and_decimal_are_numeric()
    {
        var engine = EngineOf(new Probe());
        Assert.Equal(VestigiumPropertyEditorKind.Numeric, engine.RootItems.Single(r => r.Name == "TTL").Kind);
        Assert.Equal(VestigiumPropertyEditorKind.Numeric, engine.RootItems.Single(r => r.Name == "Timeout").Kind);
    }

    [Fact]
    public void Search_hides_non_matches()
    {
        var engine = EngineOf(new Probe());
        engine.SearchText = "ttl";
        Assert.Contains(engine.VisibleRows, r => r.Name == "TTL");
        Assert.DoesNotContain(engine.VisibleRows, r => r.Name == "Display name");
    }

    [Fact]
    public void Alphabetical_has_no_category_headers()
    {
        var engine = EngineOf(new Probe());
        engine.Sort = VestigiumPropertySort.Alphabetical;
        Assert.DoesNotContain(engine.VisibleRows, r => r.IsCategory);
    }

    [Fact]
    public void Bad_hex_color_reverts()
    {
        var probe = new Probe { Accent = Color.FromRgb(1, 2, 3) };
        var engine = EngineOf(probe);
        var row = engine.RootItems.Single(r => r.Name == "Accent");
        Assert.False(engine.TryCommit(row, "not-a-color"));
        Assert.Equal(Color.FromRgb(1, 2, 3), probe.Accent);
    }

    [Fact]
    public void Inpc_updates_row_value()
    {
        var probe = new Probe { DisplayName = "A" };
        var engine = EngineOf(probe);
        probe.DisplayName = "B";
        engine.RefreshValues();
        Assert.Equal("B", engine.RootItems.Single(r => r.Name == "Display name").Value);
    }

    [Fact]
    public void ItemsSource_wins_over_SelectedObject()
    {
        var engine = new PropertyEngine();
        engine.SetSelectedObjects(new[] { new Probe() });
        engine.SetItemsSource(new[]
        {
            new VestigiumPropertyItem { Name = "Host", Kind = VestigiumPropertyEditorKind.Text }
        });
        Assert.Single(engine.RootItems);
        Assert.Equal("Host", engine.RootItems[0].Name);
    }

    [Fact]
    public void Null_selected_object_is_empty()
    {
        var engine = new PropertyEngine();
        engine.SetSelectedObjects(null);
        Assert.Empty(engine.RootItems);
        Assert.Empty(engine.VisibleRows);
    }

    [Fact]
    public void Mixed_values_when_two_objects_differ()
    {
        var a = new Probe { DisplayName = "East" };
        var b = new Probe { DisplayName = "West" };
        var engine = new PropertyEngine();
        engine.SetSelectedObjects(new object[] { a, b });
        var row = engine.RootItems.Single(r => r.Name == "Display name");
        Assert.True(row.IsMixed);
    }

    [Fact]
    public void Commit_on_mixed_writes_both_targets()
    {
        var a = new Probe { DisplayName = "East" };
        var b = new Probe { DisplayName = "West" };
        var engine = new PropertyEngine();
        engine.SetSelectedObjects(new object[] { a, b });
        var row = engine.RootItems.Single(r => r.Name == "Display name");
        Assert.True(engine.TryCommit(row, "Both"));
        Assert.Equal("Both", a.DisplayName);
        Assert.Equal("Both", b.DisplayName);
        Assert.False(row.IsMixed);
    }

    [Fact]
    public void Intersection_drops_unique_properties()
    {
        var engine = new PropertyEngine();
        engine.SetSelectedObjects(new object[] { new Probe(), new Slim() });
        Assert.DoesNotContain(engine.RootItems, r => r.Name == "TTL");
        Assert.Contains(engine.RootItems, r => r.Name == "Display name");
    }

    [Fact]
    public void List_is_collection_kind()
    {
        var engine = EngineOf(new Probe());
        Assert.Equal(VestigiumPropertyEditorKind.Collection, engine.RootItems.Single(r => r.Name == "Hops").Kind);
    }

    [Fact]
    public void Add_on_list_increases_count()
    {
        var probe = new Probe();
        var engine = EngineOf(probe);
        var hops = engine.RootItems.Single(r => r.Name == "Hops");
        Assert.True(engine.TryAdd(hops));
        Assert.Equal(1, probe.Hops.Count);
    }

    [Fact]
    public void Add_on_array_is_a_no_op()
    {
        var probe = new Probe();
        var engine = EngineOf(probe);
        var ports = engine.RootItems.Single(r => r.Name == "Ports");
        Assert.False(engine.TryAdd(ports));
        Assert.Equal(2, probe.Ports.Length);
    }

    [Fact]
    public void Reset_writes_default_value()
    {
        var probe = new Probe { Ttl = 9 };
        var engine = EngineOf(probe);
        var ttl = engine.RootItems.Single(r => r.Name == "TTL");
        Assert.True(ttl.CanReset);
        Assert.True(engine.TryReset(ttl));
        Assert.Equal(64, probe.Ttl);
        Assert.False(ttl.CanReset);
    }

    [Fact]
    public void Expand_depth_three_is_reachable()
    {
        var engine = EngineOf(new Probe());
        var thresholds = engine.RootItems.Single(r => r.Name == "Thresholds");
        engine.ToggleExpand(thresholds);
        var warning = thresholds.Children!.Single(r => r.Name == "Warning");
        engine.ToggleExpand(warning);
        var latency = warning.Children!.Single(r => r.Name == "Latency");
        Assert.Equal(2, latency.Depth);
        Assert.Equal(VestigiumPropertyEditorKind.Numeric, latency.Kind);
    }

    [Fact]
    public void Circular_instance_does_not_expand_forever()
    {
        var probe = new Probe();
        probe.Self = probe;
        var engine = EngineOf(probe);
        var self = engine.RootItems.Single(r => r.Name == "Self");
        engine.ToggleExpand(self);
        Assert.True(self.IsCircular);
        Assert.False(self.CanExpand);
        Assert.Empty(self.Children!);
    }

    [Fact]
    public void Multi_select_collection_does_not_expand()
    {
        var engine = new PropertyEngine();
        engine.SetSelectedObjects(new object[] { new Probe(), new Probe() });
        var hops = engine.RootItems.Single(r => r.Name == "Hops");
        Assert.False(hops.CanExpand);
        engine.ToggleExpand(hops);
        Assert.False(hops.IsExpanded);
    }

    [Fact]
    public void Csproj_has_no_themes_or_winforms_reference()
    {
        var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "Vestigium.Controls.PropertiesGrid", "Vestigium.Controls.PropertiesGrid.csproj"));
        if (!System.IO.File.Exists(path))
        {
            path = "/tmp/Vestigium.Controls/src/Vestigium.Controls.PropertiesGrid/Vestigium.Controls.PropertiesGrid.csproj";
        }
        var text = System.IO.File.ReadAllText(path);
        Assert.DoesNotContain("Vestigium.Themes", text);
        Assert.DoesNotContain("System.Windows.Forms", text);
    }

    [Fact]
    public void Editor_text_alignment_defaults_to_left()
    {
        var engine = EngineOf(new Probe());
        Assert.Equal(TextAlignment.Left, engine.EditorTextAlignment);
        Assert.Equal(TextAlignment.Left, engine.RootItems.First().EffectiveTextAlignment);
    }

    [Fact]
    public void Grid_alignment_applies_to_rows()
    {
        var engine = EngineOf(new Probe());
        engine.EditorTextAlignment = TextAlignment.Center;
        engine.ApplyAlignment();
        Assert.Equal(TextAlignment.Center, engine.RootItems.Single(r => r.Name == "TTL").EffectiveTextAlignment);
    }

    [Fact]
    public void Alignment_attribute_overrides_grid()
    {
        var engine = EngineOf(new Probe());
        engine.EditorTextAlignment = TextAlignment.Left;
        engine.ApplyAlignment();
        Assert.Equal(TextAlignment.Right, engine.RootItems.Single(r => r.Name == "Timeout").EffectiveTextAlignment);
    }

    [Fact]
    public void Category_icon_attaches_to_header()
    {
        var engine = EngineOf(new Probe());
        engine.CategoryIcons = VestigiumCategoryGlyphs.CreateStandard();
        engine.ApplyCategoryIcons();
        var timing = engine.VisibleRows.Single(r => r.IsCategory && r.Category == "Timing");
        Assert.True(timing.HasCategoryGlyph);
        Assert.NotNull(timing.CategoryGeometry);
    }

    [Fact]
    public void Missing_category_icon_leaves_header_bare()
    {
        var engine = EngineOf(new Probe());
        engine.CategoryIcons = new VestigiumCategoryIconCollection
        {
            new() { Category = "Timing", IconData = VestigiumCategoryGlyphs.Timing }
        };
        engine.ApplyCategoryIcons();
        var general = engine.VisibleRows.Single(r => r.IsCategory && r.Category == "General");
        Assert.False(general.HasCategoryGlyph);
        Assert.True(engine.VisibleRows.Single(r => r.IsCategory && r.Category == "Timing").HasCategoryGlyph);
    }

    [Fact]
    public void Svg_markup_yields_geometry()
    {
        var icon = new VestigiumCategoryIcon
        {
            Category = "Timing",
            Svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M2,2 H14 V14 H2 Z\"/></svg>"
        };
        Assert.NotNull(icon.ResolveGeometry());
        Assert.True(icon.HasGlyph);
    }

    [Fact]
    public void Bad_icon_data_does_not_throw()
    {
        var icon = new VestigiumCategoryIcon { Category = "Timing", IconData = "not-a-path" };
        Assert.Null(icon.ResolveGeometry());
        Assert.False(icon.HasGlyph);
    }

    [Fact]
    public void Category_header_defaults_are_left_bold_horizontal()
    {
        var grid = new VestigiumPropertiesGrid();
        Assert.Equal(TextAlignment.Left, grid.CategoryTextAlignment);
        Assert.True(grid.IsCategoryBold);
        Assert.Equal(System.Windows.Controls.Orientation.Horizontal, grid.CategoryOrientation);
        Assert.Equal(TextAlignment.Left, grid.EditorTextAlignment);
    }

    [Fact]
    public void Category_alignment_is_independent_of_editors()
    {
        var grid = new VestigiumPropertiesGrid
        {
            EditorTextAlignment = TextAlignment.Right,
            CategoryTextAlignment = TextAlignment.Center,
            IsCategoryBold = false,
            CategoryOrientation = System.Windows.Controls.Orientation.Vertical
        };
        Assert.Equal(TextAlignment.Right, grid.EditorTextAlignment);
        Assert.Equal(TextAlignment.Center, grid.CategoryTextAlignment);
        Assert.False(grid.IsCategoryBold);
        Assert.Equal(System.Windows.Controls.Orientation.Vertical, grid.CategoryOrientation);
    }

    private static PropertyEngine EngineOf(object target)
    {
        var engine = new PropertyEngine();
        engine.SetSelectedObjects(new[] { target });
        return engine;
    }

    private sealed class Slim
    {
        [Category("General"), DisplayName("Display name")]
        public string DisplayName { get; set; } = "Slim";
    }

    private sealed class Probe : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        [Browsable(false)]
        public Guid Id { get; init; } = Guid.NewGuid();

        [Category("General"), DisplayName("Display name"), Description("Label shown on the tile.")]
        public string DisplayName
        {
            get;
            set
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
            }
        } = "East";

        [Category("General")]
        public ProbeProtocol Protocol { get; set; } = ProbeProtocol.Icmp;

        [Category("General")]
        public bool Enabled { get; set; } = true;

        [Category("Timing"), DisplayName("TTL"), Description("Hop limit 1–255."), DefaultValue(64)]
        public int Ttl { get; set; } = 64;

        [Category("Timing"), DisplayName("Timeout")]
        [VestigiumTextAlignment(TextAlignment.Right)]
        public decimal Timeout { get; set; } = 1.5m;

        [Category("Display")]
        public Color Accent { get; set; } = Color.FromRgb(0x4A, 0x90, 0xC8);

        [Category("Display")]
        public DateTime LastSeen { get; set; } = DateTime.Today;

        [ReadOnly(true)]
        public string Kind { get; } = "Probe";

        public string Notes { get; set; } = "";

        [Category("Advanced")]
        public Thresholds Thresholds { get; set; } = new();

        [Category("Network")]
        public ObservableCollection<Hop> Hops { get; } = [];

        [Category("Network")]
        public int[] Ports { get; set; } = [80, 443];

        [Category("Advanced")]
        public Probe? Self { get; set; }
    }

    private sealed class Thresholds
    {
        public LatencyBand Warning { get; set; } = new() { Latency = 40 };
        public LatencyBand Critical { get; set; } = new() { Latency = 120 };
    }

    private sealed class LatencyBand
    {
        [DisplayName("Latency"), DefaultValue(typeof(decimal), "40")]
        public decimal Latency { get; set; }
    }

    private sealed class Hop
    {
        public string Host { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 80;
    }

    private enum ProbeProtocol { Icmp, Tcp, Udp }
}
