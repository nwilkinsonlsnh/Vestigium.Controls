namespace Vestigium.Controls.StatusBar;

public static class StatusBarColumns
{
    public static StatusBarColumn Create(StatusBarColumnKind kind, Action<StatusBarColumn>? configure = null)
    {
        var column = new StatusBarColumn { Kind = kind };
        configure?.Invoke(column);
        return column;
    }

    public static IList<StatusBarColumn> Standard() =>
    [
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "message";
            c.Slot = StatusBarSlot.Left;
            c.Width = StatusBarColumnWidth.Star;
            c.Text = StatusBarDefaults.ReadyText;
            c.IsLiveRegion = true;
            c.IdleTimeoutMs = StatusBarDefaults.IdleTimeoutMs;
            c.IdleText = StatusBarDefaults.IdleText;
        }),
        Create(StatusBarColumnKind.Progress, c =>
        {
            c.Key = "progress";
            c.Slot = StatusBarSlot.Center;
            c.Width = StatusBarColumnWidth.Fixed;
            c.WidthDip = 120;
        }),
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "detail";
            c.Slot = StatusBarSlot.Right;
            c.Text = "net10.0-windows";
        }),
        Create(StatusBarColumnKind.Clock, c =>
        {
            c.Key = "clock";
            c.Slot = StatusBarSlot.Right;
        })
    ];

    public static IList<StatusBarColumn> Document() =>
    [
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "message";
            c.Slot = StatusBarSlot.Left;
            c.Width = StatusBarColumnWidth.Star;
            c.Text = "Ln 42, Col 8";
            c.IsLiveRegion = true;
            c.IdleTimeoutMs = StatusBarDefaults.IdleTimeoutMs;
        }),
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "encoding";
            c.Slot = StatusBarSlot.Right;
            c.Text = "UTF-8";
        }),
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "ending";
            c.Slot = StatusBarSlot.Right;
            c.Text = "CRLF";
        }),
        Create(StatusBarColumnKind.Clock, c =>
        {
            c.Key = "clock";
            c.Slot = StatusBarSlot.Right;
        })
    ];

    public static IList<StatusBarColumn> Probe() =>
    [
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "message";
            c.Slot = StatusBarSlot.Left;
            c.Width = StatusBarColumnWidth.Star;
            c.Text = "Waiting for probe";
            c.Icon = StatusBarIconKind.Info;
            c.IsLiveRegion = true;
            c.IdleTimeoutMs = StatusBarDefaults.IdleTimeoutMs;
        }),
        Create(StatusBarColumnKind.Progress, c =>
        {
            c.Key = "progress";
            c.Slot = StatusBarSlot.Center;
            c.Width = StatusBarColumnWidth.Fixed;
            c.WidthDip = 100;
            c.IsProgressVisible = true;
        }),
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "rtt";
            c.Slot = StatusBarSlot.Right;
            c.Text = "— ms";
        })
    ];

    public static IList<StatusBarColumn> Groups() =>
    [
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "left";
            c.Slot = StatusBarSlot.Left;
            c.Text = "Far left";
            c.Icon = StatusBarIconKind.Info;
            c.IsLiveRegion = true;
        }),
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "center";
            c.Slot = StatusBarSlot.Center;
            c.Text = "Centered";
            c.Icon = StatusBarIconKind.Success;
        }),
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "right";
            c.Slot = StatusBarSlot.Right;
            c.Text = "Far right";
            c.Icon = StatusBarIconKind.Warning;
        })
    ];

    public static IList<StatusBarColumn> Icons() =>
    [
        Create(StatusBarColumnKind.Icon, c => { c.Key = "ok"; c.Slot = StatusBarSlot.Left; c.Icon = StatusBarIconKind.Success; }),
        Create(StatusBarColumnKind.Icon, c => { c.Key = "warn"; c.Slot = StatusBarSlot.Left; c.Icon = StatusBarIconKind.Warning; }),
        Create(StatusBarColumnKind.Icon, c => { c.Key = "err"; c.Slot = StatusBarSlot.Left; c.Icon = StatusBarIconKind.Error; }),
        Create(StatusBarColumnKind.Icon, c => { c.Key = "info"; c.Slot = StatusBarSlot.Left; c.Icon = StatusBarIconKind.Info; }),
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "message";
            c.Slot = StatusBarSlot.Left;
            c.Text = "Channel health";
            c.IsLiveRegion = true;
            c.IdleTimeoutMs = StatusBarDefaults.IdleTimeoutMs;
        }),
        Create(StatusBarColumnKind.Clock, c => { c.Key = "clock"; c.Slot = StatusBarSlot.Right; })
    ];
}
