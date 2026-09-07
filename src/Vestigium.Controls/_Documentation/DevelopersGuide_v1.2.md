# Vestigium.Controls (base) — Developers Guide

**Document ID:** VEST-CTL-BASE-DEV-001  
**Version:** 1.2  
**Status:** Implemented  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.2.md`](Requirements_v1.2.md)

## Project

| Item | Value |
|---|---|
| Assembly | `Vestigium.Controls` |
| Shell | `Vestigium.Controls.Shell.VestigiumShell` |
| Window | `Vestigium.Controls.Shell.VestigiumDefaultWindow` |
| Demo | `src/Vestigium.Controls.Demo` |

## Default form

```csharp
services.AddVestigiumControls();
var window = new VestigiumDefaultWindow();
window.Show();
```

No spec → three underlined radios (Home, Workspace, Settings). Home ships with nested radios (Overview, Live, Shortcuts → Favorites / Recent) so you can see indent immediately. Each slot has its own Under Construction page. Status bar at the bottom.

File and View are **window chrome** on `VestigiumDefaultWindow` only. A nested `VestigiumShell` never clones File / View.

## Stage named modules

```csharp
var shell = VestigiumShell.Stage(new VestigiumShellSpec
{
    Items =
    {
        new VestigiumNavItemSpec("PingIQ")
        {
            Title = "PingIQ",
            Subject = "ICMP engine",
            Description = "Live ping, history, and thresholds land here."
        },
        new VestigiumNavItemSpec("TraceIQ") { Subject = "Path discovery" },
        new VestigiumNavItemSpec("DnsIQ"),
        new VestigiumNavItemSpec("CertIQ"),
        new VestigiumNavItemSpec("Settings")
    }
});
```

Title, Subject, Description, Image, ImageUri write through to that item’s construction page.

## Replace a slot

```csharp
var inner = VestigiumShell.Stage(new VestigiumShellSpec
{
    IsSubShell = true,
    ShowStatusBar = false,
    Items =
    {
        new VestigiumNavItemSpec("Live"),
        new VestigiumNavItemSpec("History"),
        new VestigiumNavItemSpec("Thresholds")
    }
});

shell["PingIQ"].Content = new TabControl
{
    Items = { new TabItem { Header = "Workspace", Content = inner } }
};
```

PingIQ’s construction page is gone. Other slots are unchanged. The inner radios indent. No second status bar.

`shell["PingIQ"].RestorePlaceholder()` puts construction back.

## Look up by name

`shell["pingiq"]` matches `Key` then `Header`, case-insensitive, including children.

From the default window: `window.HostShell["Settings"]` or `viewModel["Settings"]`. Do not name a property `Shell` — that collides with the `Vestigium.Controls.Shell` namespace.

## Status bar

```xml
<shell:VestigiumShell ShowStatusBar="{Binding ShowStatusBar}"
                      Status="{Binding Status}"
                      StatusBarPosition="{Binding Status.Position, Mode=TwoWay}"/>
```

Nested shells leave `ShowStatusBar` false unless you set it. Columns stay on `Status.Engine` from `Vestigium.Controls.StatusBar`.

## Theme from the parent

Do this in the **calling** application, once:

```csharp
// ThemeManager lives in Vestigium.Themes, not here.
ThemeManager.Initialize(ThemeKind.LightBlue);
```

Or merge a dictionary on the window. Nested shells inherit.

Per-module override only:

```xml
<shell:VestigiumShell ThemeResources="{StaticResource Vestigium.Theme.Dracula}"/>
```

The shell never names Light Blue / Dracula.

## Host the shell without the window

```xml
<shell:VestigiumShell IsSubShell="True" ShowStatusBar="False"/>
```

Use this inside a grid cell, a `TabItem`, or another shell’s content.

## Do not

- Put `ThemeManager` in this assembly.
- TwoWay-bind `NavItems` onto a get-only CLR property.
- Expect a fourth indented nav level. Depth 3 is the cap.
- Share one Under Construction instance across items. Each slot owns one.
- Turn on an inner status bar unless that module really needs its own rail.
