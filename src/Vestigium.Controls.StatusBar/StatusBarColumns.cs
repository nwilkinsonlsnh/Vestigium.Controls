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
            c.Width = StatusBarColumnWidth.Star;
            c.Text = StatusBarDefaults.ReadyText;
            c.IsLiveRegion = true;
            c.IdleTimeoutMs = StatusBarDefaults.IdleTimeoutMs;
            c.IdleText = StatusBarDefaults.IdleText;
        }),
        Create(StatusBarColumnKind.Progress, c =>
        {
            c.Key = "progress";
            c.Width = StatusBarColumnWidth.Fixed;
            c.WidthDip = 120;
        }),
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "detail";
            c.Text = "net10.0-windows";
        }),
        Create(StatusBarColumnKind.Clock, c => c.Key = "clock")
    ];

    public static IList<StatusBarColumn> Document() =>
    [
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "message";
            c.Width = StatusBarColumnWidth.Star;
            c.Text = "Ln 42, Col 8";
            c.IsLiveRegion = true;
            c.IdleTimeoutMs = StatusBarDefaults.IdleTimeoutMs;
        }),
        Create(StatusBarColumnKind.Text, c => { c.Key = "encoding"; c.Text = "UTF-8"; }),
        Create(StatusBarColumnKind.Text, c => { c.Key = "ending"; c.Text = "CRLF"; }),
        Create(StatusBarColumnKind.Clock, c => c.Key = "clock")
    ];

    public static IList<StatusBarColumn> Probe() =>
    [
        Create(StatusBarColumnKind.Text, c =>
        {
            c.Key = "message";
            c.Width = StatusBarColumnWidth.Star;
            c.Text = "Waiting for probe";
            c.Icon = StatusBarIconKind.Info;
            c.IsLiveRegion = true;
            c.IdleTimeoutMs = StatusBarDefaults.IdleTimeoutMs;
        }),
        Create(StatusBarColumnKind.Progress, c =>
        {
            c.Key = "progress";
            c.Width = StatusBarColumnWidth.Fixed;
            c.WidthDip = 100;
            c.IsProgressVisible = true;
        }),
        Create(StatusBarColumnKind.Text, c => { c.Key = "rtt"; c.Text = "— ms"; })
    ];
}
