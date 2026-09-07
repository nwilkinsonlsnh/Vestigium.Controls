# Vestigium.Controls (base) — Developers Guide

**Document ID:** VEST-CTL-BASE-DEV-001  
**Version:** 1.4  
**Status:** Implemented  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.4.md`](Requirements_v1.4.md)

How to host, stage, nest, and theme the master form. Sibling controls have their own guides.

---

## Project

| Item | Value |
|---|---|
| Assembly | `Vestigium.Controls` |
| Shell | `Vestigium.Controls.Shell.VestigiumShell` |
| Window | `Vestigium.Controls.Shell.VestigiumDefaultWindow` |
| Window VM | `Vestigium.Controls.Shell.VestigiumDefaultWindowViewModel` |
| Menu | `Shell/MenuChrome.xaml` |
| Demo | `src/Vestigium.Controls.Demo` (`MainWindow` + `GalleryViewModel`) |
| Tests | `src/Vestigium.Controls.Tests` (`ShellTests`, `[StaFact]`) |
| DI | `AddVestigiumControls()` |

Do not name a property `Shell`. That collides with `Vestigium.Controls.Shell`. Use `HostShell` / `RootShell`.

---

## 1. Default form

```csharp
services.AddVestigiumControls();
var window = new VestigiumDefaultWindow();
window.Show();
```

Or with a VM (DataContext is set **before** `InitializeComponent`):

```csharp
var vm = new VestigiumDefaultWindowViewModel();
var window = new VestigiumDefaultWindow(vm);
window.HostShell["Home"].SetPlaceholder(subject: "Start here");
```

No spec → Home / Workspace / Settings. Home already has Overview, Live, Shortcuts (Favorites, Recent). Each slot has its own Under Construction page. Status bar Bottom. File / View on the **window** only.

Merge menu chrome if you roll your own window:

```xml
<ResourceDictionary Source="pack://application:,,,/Vestigium.Controls;component/Shell/MenuChrome.xaml"/>
```

Without that dictionary, Win11 draws a light popup and white labels disappear.

---

## 2. Demo gallery

`Vestigium.Controls.Demo` starts a gallery, not a bare form.

| Region | What it is |
|---|---|
| File / View | Gallery window chrome (`MenuChrome`). Not cloned onto inner shells. |
| Left, live | `VestigiumShell` — radios, construction, status. Caption: no File menu on this control. |
| Left, bottom | Always-nested sample. `IsSubShell`, no status bar. Overview / Activity / Alerts. |
| Right | Staging, independence, indent slider 0–80, status Bottom / Top, contract. |

Walkthrough: Home → Shortcuts → Favorites (three rows). Stage PingIQ suite. Replace PingIQ with inner form. Toggle “Treat as nested submenu”. Slide indent. Dock status Top (under the radios, never over File / View).

---

## 3. Stage named modules

```csharp
var shell = VestigiumShell.Stage(new VestigiumShellSpec
{
    NavIndent = 24,
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

`Title`, `Subject`, `Description`, `Image`, `ImageUri` write through to that item’s construction page. `Children` on a spec become the next radio row.

`Stage()` with an empty or null spec rebuilds the default Home tree (`SeedDefaults()` does the same on an existing shell).

---

## 4. Replace a slot

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
    Items =
    {
        new TabItem { Header = "Workspace", Content = inner },
        new TabItem { Header = "Notes", Content = new VestigiumUnderConstruction
        {
            Title = "Notes",
            Subject = "Tab inside PingIQ"
        }}
    }
};
```

PingIQ’s construction page is gone. Other slots are unchanged. Inner radios indent. No second status bar. No File / View on the inner form.

`shell["PingIQ"].RestorePlaceholder()` puts construction back. The same `Placeholder` instance is reused.

Pinned shell content (rare):

```csharp
shell.Content = dashboard; // radios still work; presenter stays on dashboard
```

---

## 5. Look up by name

```csharp
var ping = shell["pingiq"];          // Key then Header, case-insensitive, depth-first
var fav  = shell["Favorites"];       // child of Home / Shortcuts
var set  = window.HostShell["Settings"];
var also = viewModel["Settings"];    // forwards to RootShell
```

Missing name → `null`.

---

## 6. Indent

```xml
<shell:VestigiumShell NavIndent="28"/>
```

Formula: `NavIndent × (ShellDepth + row)`. Default 20. Range 0–80. View menu steps by 4. `0` is flush left.

Margins are `Level0Margin`, `Level1Margin`, `Level2Margin` (read-only Thickness). They update when `NavIndent` or `ShellDepth` changes.

---

## 7. Status bar

```xml
<shell:VestigiumShell ShowStatusBar="{Binding ShowStatusBar}"
                      Status="{Binding Status}"
                      StatusBarPosition="{Binding Status.Position, Mode=TwoWay}"/>
```

`Status` is never null (coerced to an owned VM). Nested shells leave `ShowStatusBar` false unless you set it.

Columns, idle (`Idle. . .` / 3000 ms), AIMD, slots Left / Center / Right — all on `Status.Engine` from `Vestigium.Controls.StatusBar`. This assembly only docks the rail.

Set DataContext before `InitializeComponent` or the `Status` binding NRE’s during load.

---

## 8. Theme from the parent

In the **calling** application, once:

```csharp
// ThemeManager lives in Vestigium.Themes, not here.
ThemeManager.Initialize(ThemeKind.LightBlue);
```

Or merge a dictionary on the window. Nested shells inherit.

Per-module override only:

```xml
<shell:VestigiumShell ThemeResources="{StaticResource Vestigium.Theme.Dracula}"/>
```

The shell never names Light Blue / Dracula. Fallback brushes live in Generic.xaml. Nav labels bind `OnChrome` / `OnChromeMuted`.

---

## 9. Host the shell without the window

```xml
<shell:VestigiumShell IsSubShell="True"
                      ShowStatusBar="False"
                      ShellDepth="1"/>
```

Use this inside a grid cell, a `TabItem`, or another shell’s content.

---

## 10. Construction pages in tight cells

`VestigiumUnderConstruction` (sibling) is compact here: ~72×88 default glyph, padding 16,12, inner `ScrollViewer`. The shell content host also scrolls. Nested samples must still show Title, Subject, and Description — not a clipped cone.

Default Title max 75, Subject max 125, Description unlimited with newlines. Limits are on the construction control, not on the shell.

---

## 11. Binding contract

| Bind | Mode |
|---|---|
| `SelectedItem`, `IsSelected`, `ShowStatusBar`, `NavIndent`, `StatusBarPosition` | TwoWay is fine |
| `NavItems` | OneWay from the host. Do not TwoWay onto a get-only CLR property |
| `Status` | OneWay or omit (owned instance) |
| `Content` (shell or item) | OneWay |

Same lesson as PropertiesGrid `GridTarget`: a get-only VM property cannot be TwoWay.

---

## 12. Tests

`Vestigium.Controls.Tests` uses xunit + **Xunit.StaFact**. Anything that `new`s `VestigiumShell`, `VestigiumDefaultWindow`, `VestigiumUnderConstruction`, or `VestigiumPropertiesGrid` is `[StaFact]`. Engine-only tests stay `[Fact]`.

---

## 13. Do not

- Put `ThemeManager` in this assembly.
- TwoWay-bind `NavItems` (or `GridTarget`-style objects) onto a get-only CLR property.
- Name a view-model property `Shell`.
- Expect a fourth indented nav level.
- Share one Under Construction instance across items.
- Turn on an inner status bar unless that module really needs its own rail.
- Rely on SystemColors for File / View — merge `MenuChrome.xaml`.
- Put File / View inside `VestigiumShell`.
- Skip DataContext-before-`InitializeComponent` on the default window.
