using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Vestigium.Controls.PropertiesGrid.Demo;

public enum ProbeProtocol { Icmp, Tcp, Udp }

public partial class Hop : ObservableObject
{
    [DisplayName("Host"), Description("Address of this hop.")]
    public string Host { get => field; set => SetProperty(ref field, value); } = "0.0.0.0";

    [DisplayName("Port"), DefaultValue(80)]
    public int Port { get => field; set => SetProperty(ref field, value); } = 80;
}

public partial class LatencyBand : ObservableObject
{
    [DisplayName("Latency"), Description("Round-trip threshold in milliseconds."), DefaultValue(typeof(decimal), "40")]
    public decimal Latency { get => field; set => SetProperty(ref field, value); } = 40;

    [DisplayName("Loss"), DefaultValue(typeof(decimal), "1")]
    public decimal Loss { get => field; set => SetProperty(ref field, value); } = 1;
}

public partial class Thresholds : ObservableObject
{
    [Category("Bands")]
    public LatencyBand Warning { get; } = new() { Latency = 40, Loss = 1 };

    [Category("Bands")]
    public LatencyBand Critical { get; } = new() { Latency = 120, Loss = 5 };
}

public partial class ProbeSettings : ObservableObject
{
    [Browsable(false)]
    public Guid Id { get; } = Guid.NewGuid();

    [Category("General"), DisplayName("Display name"), Description("Label shown on the tile.")]
    public string DisplayName { get => field; set => SetProperty(ref field, value); } = "East probe";

    [Category("General"), Description("Enable this probe.")]
    public bool Enabled { get => field; set => SetProperty(ref field, value); } = true;

    [Category("General")]
    public ProbeProtocol Protocol { get => field; set => SetProperty(ref field, value); } = ProbeProtocol.Icmp;

    [Category("Timing"), DisplayName("TTL"), Description("Hop limit 1–255."), DefaultValue(64)]
    public int Ttl { get => field; set => SetProperty(ref field, value); } = 64;

    [Category("Timing"), DisplayName("Timeout"), Description("Seconds to wait.")]
    public decimal Timeout { get => field; set => SetProperty(ref field, value); } = 1.5m;

    [Category("Display"), Description("Accent used on the tile.")]
    public Color Accent { get => field; set => SetProperty(ref field, value); } = Color.FromRgb(0x4A, 0x90, 0xC8);

    [Category("Display"), DisplayName("Last seen")]
    public DateTime LastSeen { get => field; set => SetProperty(ref field, value); } = DateTime.Today;

    [Category("General"), ReadOnly(true), Description("Built-in kind. Not editable.")]
    public string Kind { get; } = "Probe";

    [Category("Advanced"), Description("Nested warning and critical bands.")]
    public Thresholds Thresholds { get; } = new();

    [Category("Network"), Description("Editable hop list. Add, remove, reorder.")]
    public ObservableCollection<Hop> Hops { get; } = [new() { Host = "edge.vestigium", Port = 443 }];

    [Category("Network"), Description("Fixed-size array. Items edit; Add is off.")]
    public int[] Ports { get; set; } = [80, 443];

    [Category("Advanced"), Description("Circular reference back to this probe.")]
    public ProbeSettings? Self { get; set; }

    public string Notes { get => field; set => SetProperty(ref field, value); } = "";
}
