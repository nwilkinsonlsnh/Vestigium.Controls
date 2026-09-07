# Vestigium.Controls (base) — Software Requirements Specification

**Document ID:** VEST-CTL-BASE-SRS-001  
**Version:** 1.2  
**Status:** Implemented  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM  
**Supersedes:** v1.0 / v1.1

Source of truth for the shared assembly and the reusable Vestigium form.

---

## 1. Product

`Vestigium.Controls` holds types every feature control may depend on, DI (`AddVestigiumControls`), and the **master form**:

| Type | Role |
|---|---|
| `VestigiumShell` | Lookless `Control`. Nav + content + status bar. Nestable. |
| `VestigiumDefaultWindow` | Thin `Window` wrapper (File → Exit, View chrome) that hosts one `VestigiumShell`. |

A `Window` cannot nest. Hosts put `VestigiumShell` in any cell. The startup app uses `VestigiumDefaultWindow`.

This assembly MAY reference `Vestigium.Controls.StatusBar` and `Vestigium.Controls.UnderConstruction`. It SHALL NOT reference `Vestigium.Themes*`.

---

## 2. Shell layout

Top to bottom when the status bar is Bottom:

1. Underlined radio nav (optional second / third row for children)
2. Content host
3. `VestigiumStatusBar`

When position is Top: nav, status bar, content.

Nav is always above the status bar. **File → Exit and View live only on `VestigiumDefaultWindow`.** Nested shells do not get another File / View menu. Module navigation is the underlined radio strip on the shell.

---

## 3. Navigation

Not a Win32 menu bar. Module navigation is `RadioButton`s, one selected per row, 2 dip accent underline on the selected header.

Each item **owns** its content. Selecting an item shows that item’s `DisplayContent`.

| Slot | Default |
|---|---|
| `Content` set | That object (view, `TabControl`, nested `VestigiumShell`) |
| `Content` null | That item’s own `VestigiumUnderConstruction` |

Three default top-level items when the host passes no spec. **Home includes nested children** so a new form shows indent without extra staging:

| Header | Children |
|---|---|
| Home | Overview, Live, Shortcuts (Favorites, Recent) |
| Workspace | — |
| Settings | — |

Each placeholder is a **separate** instance. Renaming `Header` updates the radio and, until Title is overridden, the placeholder Title.

Children of an item render on the next row, indented `NavIndent × (ShellDepth + row)` dip. `NavIndent` default is **20**, range **0–80**. Maximum tree depth is **3**. A fourth level is not created.

---

## 4. Staging factory

```csharp
var shell = VestigiumShell.Stage(new VestigiumShellSpec
{
    ShowStatusBar = true,
    Items =
    {
        new VestigiumNavItemSpec("PingIQ")
        {
            Title = "PingIQ",
            Subject = "ICMP engine",
            Description = "Live ping, history, and thresholds land here."
        }
    }
});

shell["PingIQ"].Content = new TabControl { /* inner VestigiumShell */ };
```

`Stage(null)` / `Stage()` seeds Home / Workspace / Settings.

Indexer is case-insensitive on `Key` then `Header`, depth-first.

`VestigiumNavItemSpec` fields map onto the Under Construction DPs: Title, Subject, Description, Image, ImageUri.

Replacing `Content` drops the construction page from the presenter. `RestorePlaceholder()` brings it back.

Shell-level `Content` (pinned) wins over the selected item. Used when the host wants a single view regardless of nav.

---

## 5. Nesting

| Property | Default | Behavior |
|---|---|---|
| `IsSubShell` | false | Indent as submenu. `ShellDepth` becomes 1 if still 0. |
| `ShellDepth` | 0 | Extra indent. Coerced 0–2. |
| `ShowStatusBar` | true | **false** when `IsSubShell` unless the host set ShowStatusBar explicitly. |
| `ShowNav` | true | Hide the radio row. |
| `MaxNavDepth` | 3 | Coerced 1–3. |

A shell inside PingIQ’s `TabControl` is a real inner form. Outer slots stay on their own placeholders until replaced.

---

## 6. Status bar

Reuse `VestigiumStatusBar`. No fork.

The shell docks it and exposes `Status` (`VestigiumStatusBarViewModel`) and `StatusBarPosition`. Hosts configure columns through the existing status API.

Two bars (outer + inner) are opt-in. Nested default is off.

---

## 7. Theming — parent down

No `Theme` enum. No `ThemeManager` call inside this assembly.

Host merges theme dictionaries at `Application` or `Window`. Children inherit via WPF resource lookup.

Optional `ThemeResources` (`ResourceDictionary`) merges onto **that** shell instance only. The shell does not interpret the dictionary.

Chrome uses `DynamicResource` keys with Generic.xaml fallbacks:

`Vestigium.Brushes.Background`, `Chrome`, `Text`, `Muted`, `Hairline`, `Accent`.

---

## 8. Property matrix (`VestigiumShell`)

| Property | Type | Default |
|---|---|---|
| `NavItems` | `IList` | Seeded Home / Workspace / Settings |
| `SelectedItem` | `VestigiumNavItem?` | First item. TwoWay. |
| `Content` | `object?` | null. Pinned override. |
| `DisplayContent` | `object?` | Read-only presenter value. |
| `ShowNav` | `bool` | true |
| `ShowStatusBar` | `bool` | true (false on sub-shell) |
| `IsSubShell` | `bool` | false |
| `ShellDepth` | `int` | 0 |
| `MaxNavDepth` | `int` | 3 |
| `NavIndent` | `int` | 20 (0–80) |
| `Status` | `VestigiumStatusBarViewModel` | Owned "Ready" / Bottom |
| `StatusBarPosition` | `VestigiumStatusBarPosition` | Bottom |
| `ThemeResources` | `ResourceDictionary?` | null |

`SelectedObject`-style inspection does not apply. Nav items are OneWay from the host except `SelectedItem` and `IsSelected`.

---

## 9. Demo

`Vestigium.Controls.Demo` SHALL open a gallery window that:

1. Hosts a live `VestigiumShell` (radios + construction pages + status bar).
2. Keeps File / View on the **gallery window** only, with a caption that the live shell has no File menu.
3. Shows an always-nested inner shell (`IsSubShell`, no status bar).
4. Lets the host stage PingIQ / TraceIQ / DnsIQ / CertIQ / Settings.
5. Replaces PingIQ with a `TabControl` + inner `VestigiumShell`.
6. Restores the selected placeholder.
7. Toggles status bar visibility, Top / Bottom, nested submenu, and `NavIndent` (slider 0–80).
8. Resets to Home / Workspace / Settings including Home's nested pages.

The demo does not initialize Vestigium.Themes.

---

## 10. Tests

| ID | Assertion |
|---|---|
| SH-T01 | `Stage()` seeds Home, Workspace, Settings |
| SH-T02 | Each seed has a distinct Placeholder instance |
| SH-T03 | Named stage + indexer (case-insensitive) |
| SH-T04 | Content replace / RestorePlaceholder |
| SH-T05 | Header rename updates Title until overridden |
| SH-T06 | `IsSubShell` hides status bar unless ShowStatusBar is set |
| SH-T07 | Fourth nav level is clipped |
| SH-T08 | Pinned `Content` wins over the selected item |
| SH-T09 | Default window exposes a shell with three items |

---

## 11. Non-goals

- Ribbon / Fluent NavigationView clone
- Custom caption buttons
- Required icons on nav items
- Auto-building menus by reflecting assemblies
- Embedding ThemeManager

---

## 12. Document control

| Version | Change |
|---|---|
| 1.0 | Skeleton window + DI |
| 1.1 | Host assigns theme |
| 1.2 | `VestigiumShell`, staging factory, nested forms, per-item construction, parent-down theme |
