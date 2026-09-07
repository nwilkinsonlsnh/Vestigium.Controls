using System.Windows;
using System.Windows.Media;

namespace Vestigium.Controls.Shell;

public sealed class VestigiumNavItemSpec
{
    public VestigiumNavItemSpec()
    {
    }

    public VestigiumNavItemSpec(string header) => Header = header;

    public string Header { get; set; } = ShellRules.DefaultHome;
    public string? Key { get; set; }
    public string? Title { get; set; }
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public ImageSource? Image { get; set; }
    public string? ImageUri { get; set; }
    public object? Content { get; set; }
    public IList<VestigiumNavItemSpec> Children { get; } = new List<VestigiumNavItemSpec>();
}

public sealed class VestigiumShellSpec
{
    public string? Header { get; set; }
    public bool? ShowStatusBar { get; set; }
    public bool IsSubShell { get; set; }
    public int ShellDepth { get; set; }
    public int? NavIndent { get; set; }
    public ResourceDictionary? ThemeResources { get; set; }
    public IList<VestigiumNavItemSpec> Items { get; } = new List<VestigiumNavItemSpec>();
}
