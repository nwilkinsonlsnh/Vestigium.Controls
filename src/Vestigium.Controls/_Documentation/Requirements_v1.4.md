# Vestigium.Controls (base) — Software Requirements Specification

**Document ID:** VEST-CTL-BASE-SRS-001  
**Version:** 1.4  
**Status:** Implemented  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF, MVVM (`CommunityToolkit.Mvvm`)  
**Supersedes:** v1.0 / v1.1 / v1.2 / v1.3

Source of truth for the shared assembly and the reusable Vestigium form. Sibling control libraries (StatusBar, NumericUpDown, UnderConstruction, PropertiesGrid) have their own SRS. This document does not replace those.

---

## 1. Product

`Vestigium.Controls` is the umbrella assembly. It holds types every feature control may depend on, DI (`AddVestigiumControls`), and the **master form**.

| Type | Role |
|---|---|
| `VestigiumShell` | Lookless `Control`. Underlined radio nav + content host + status bar. Nestable. |
| `VestigiumDefaultWindow` | Thin `Window` wrapper. File / View chrome + one hosted `VestigiumShell`. |
| `VestigiumDefaultWindowViewModel` | Window chrome VM (`Status`, `ShowStatusBar`, `NavIndent`, commands). |
| `VestigiumNavItem` | One radio slot. Owns a distinct `VestigiumUnderConstruction` plus optional `Content`. |
| `VestigiumNavItemSpec` / `VestigiumShellSpec` | Staging factory. No XAML required to stand up placeholders. |
| `ShellRules` | Depth, indent, default headers, placeholder copy. |
| `MenuChrome.xaml` | Dark File / View templates. Win11 light popups SHALL NOT win. |

A `Window` cannot nest. Hosts put `VestigiumShell` in any cell, `TabItem`, or another shell’s content. The startup app uses `VestigiumDefaultWindow`.

This assembly MAY reference `Vestigium.Controls.StatusBar` and `Vestigium.Controls.UnderConstruction`. It SHALL NOT reference `Vestigium.Themes*`.

Do **not** name a public property `Shell`. That collides with namespace `Vestigium.Controls.Shell`. Use `HostShell` on the window and `RootShell` on view models.

---

## 2. Architecture

- Lookless `CustomControl` (`VestigiumShell`). Visual tree in `Themes/Generic.xaml`.
- MVVM. No business logic in code-behind beyond `Exit_Click` and wiring `HostShell`.
- `CommunityToolkit.Mvvm` for `ObservableObject` / `RelayCommand`.
- Status rail is `VestigiumStatusBar` from the sibling assembly. No fork.
- Placeholder page is `VestigiumUnderConstruction` from the sibling assembly. Each nav item **owns** its own instance.
- Theme is parent-down. This assembly never calls `ThemeManager`.

---

## 3. Shell layout

When status bar position is **Bottom** (default), top to bottom:

1. Underlined radio nav (up to three visible rows)
2. Content host (`ScrollViewer` + `ContentPresenter`)
3. `VestigiumStatusBar`

When position is **Top**: nav, status bar, content. Nav is always above the status bar. Status never covers File / View.

**File and View live only on the Window** (`VestigiumDefaultWindow` or a host gallery). Nested `VestigiumShell` instances SHALL NOT clone File / View. Module navigation is the radio strip on the shell.

### 3.1 Radio chrome

- One selected radio per visible row (`GroupName` unique per shell instance).
- Accent underline is **inside** the radio cell, bottom edge, 3 dip. It SHALL NOT sit in extra height below the row (that clips).
- Selected state follows `IsSelected` **and** `IsChecked`.
- Idle label uses `Vestigium.Brushes.OnChromeMuted`. Selected / hover uses `OnChrome`. Body `Muted` is too dark on chrome.

### 3.2 File / View chrome

`MenuChrome.xaml` owns Menu / MenuItem / Separator templates:

| Part | Color |
|---|---|
| Bar | `#0F2744` |
| Popup | `#132744` |
| Text | `#F8FAFC` |
| Hover | `#1B4F86` |
| Hairline | `#1E3A5F` |

SystemColors SHALL NOT be relied on. Win11 keeps a light popup; white item `Foreground` on that popup is unreadable. The custom popup paints both background and text.

Checkable items show a `✓` in a 22-dip gutter.

---

## 4. Navigation

Not a Win32 menu bar. Each item **owns** its content. Selecting an item shows that item’s `DisplayContent`.

| Slot | Presenter shows |
|---|---|
| Shell `Content` set (pinned) | That object, regardless of selection |
| Item `Content` set | That object (view, `TabControl`, nested `VestigiumShell`) |
| Item `Content` null | That item’s own `VestigiumUnderConstruction` |

`DisplayContent` on the item is `Content ?? Placeholder`. Shell `DisplayContent` is pinned `Content` else selected item’s `DisplayContent`.

### 4.1 Default seed (`Stage()` / no spec)

| Header | Children | Placeholder subject |
|---|---|---|
| Home | Overview, Live, Shortcuts → Favorites, Recent | Home-specific copy (nested demo) |
| Workspace | — | `This area is a placeholder` |
| Settings | — | `This area is a placeholder` |

Home’s nested pages exist so a new form shows indent without extra staging. Depth of Favorites / Recent is 3 (the cap).

Each placeholder is a **separate** instance. Changing Home’s subject SHALL NOT change Workspace’s subject.

Renaming `Header` updates the radio and, until `SetPlaceholder(title:)` has been called (`TitleOverridden`), the placeholder Title. After override, Header rename leaves Title alone.

Default description: `Replace this item's Content when the module is ready. Title, subject, description, and image are settable on the placeholder.`

### 4.2 Indent

`LevelNMargin.Left = NavIndent × (ShellDepth + N)` for rows 0, 1, 2.

| Property | Default | Coerce |
|---|---|---|
| `NavIndent` | 20 | 0–80 |
| `NavIndentStep` (View menu) | 4 | — |
| `ShellDepth` | 0 | 0–2 |
| `MaxNavDepth` | 3 | 1–3 |

`0` stacks nested rows flush left. A fourth tree level is **not created**. Staging a child at depth 4 is clipped.

---

## 5. Staging factory

```csharp
var shell = VestigiumShell.Stage(new VestigiumShellSpec
{
    ShowStatusBar = true,
    NavIndent = 20,
    Items =
    {
        new VestigiumNavItemSpec("PingIQ")
        {
            Title = "PingIQ",
            Subject = "ICMP engine",
            Description = "Live ping, history, and thresholds land here.",
            Children = { new VestigiumNavItemSpec("Live") }
        }
    }
});

shell["PingIQ"].Content = new TabControl { /* inner VestigiumShell */ };
```

`Stage()` / `Stage(null)` / `SeedDefaults()` rebuilds the default Home tree.

### 5.1 `VestigiumShellSpec`

| Field | Meaning |
|---|---|
| `Items` | Root radios. Empty → default seed. |
| `IsSubShell` | Indent as submenu. If `ShellDepth` is 0, it becomes 1. Status bar off unless `ShowStatusBar` is set. |
| `ShellDepth` | Extra indent. |
| `ShowStatusBar` | `bool?`. Null → default (true, or false on sub-shell). |
| `NavIndent` | `int?`. Null → leave default 20. |
| `ThemeResources` | Merged onto that instance only. |
| `Header` | Reserved / unused by the control (host label). |

### 5.2 `VestigiumNavItemSpec`

`Header`, `Key`, `Title`, `Subject`, `Description`, `Image`, `ImageUri`, `Content`, `Children`.

Unset Title / Subject / Description keep the item constructor defaults (Title = Header, Subject = placeholder sentence). Spec values write through `SetPlaceholder`.

### 5.3 Indexer

`shell["pingiq"]` matches `Key` then `Header`, case-insensitive, depth-first, including children. Missing → `null`.

Window: `window.HostShell["Settings"]`. View model: `viewModel["Settings"]` forwards to `RootShell`.

### 5.4 Replace / restore

`item.Content = view` drops the construction page from the presenter. Other slots are unchanged.

`item.RestorePlaceholder()` sets `Content = null`. The owned `Placeholder` instance is kept, never recreated, never shared.

Shell-level pinned `Content` wins over the selected item.

---

## 6. Nesting

| Property | Default | Behavior |
|---|---|---|
| `IsSubShell` | false | Treat as submenu. `ShellDepth` becomes 1 if still 0. |
| `ShowStatusBar` | true | **false** on sub-shell unless the host set it explicitly. |
| `ShowNav` | true | Hide the radio rows. |

A shell inside PingIQ’s `TabControl` is a real inner form. It does not get File / View. Outer slots stay on their own placeholders until replaced.

Independence: each shell has its own `Status` VM, `ShowStatusBar`, `NavIndent`, `NavItems`. Turning off the inner rail does not hide the outer one.

---

## 7. Status bar

Reuse `VestigiumStatusBar`. The shell docks it and exposes:

- `Status` (`VestigiumStatusBarViewModel`). Null is coerced to an owned instance (`Message = "Ready"`, position Bottom) so startup cannot NRE.
- `StatusBarPosition` (TwoWay, default Bottom). Top sits **under** the radios, never over File / View.

Hosts configure columns through the existing StatusBar API (`Status.Engine`). This assembly does not reimplement columns, idle, or AIMD.

DataContext of `VestigiumDefaultWindow` SHALL be assigned **before** `InitializeComponent` so `Status="{Binding Status}"` does not hit a null VM.

Two bars (outer + inner) are opt-in. Nested default is off.

---

## 8. Theming — parent down

No `Theme` enum. No `ThemeManager` in this assembly.

Host merges dictionaries at `Application` or `Window`. Nested shells inherit via WPF resource lookup.

Optional `ThemeResources` merges onto **that** shell instance only. The shell does not interpret keys.

Generic.xaml fallbacks (DynamicResource):

`Vestigium.Brushes.Background`, `Chrome`, `Text`, `Muted`, `Hairline`, `Accent`, `OnChrome`, `OnChromeMuted`.

Nav uses OnChrome / OnChromeMuted.

---

## 9. Property matrix

### 9.1 `VestigiumShell`

| Property | Type | Default | Notes |
|---|---|---|---|
| `NavItems` | `IList` | Seeded Home tree | OneWay from host. Do not TwoWay onto a get-only CLR property. |
| `SelectedItem` | `VestigiumNavItem?` | First item | TwoWay. |
| `Content` | `object?` | null | Pinned override. |
| `DisplayContent` | `object?` | — | Read-only presenter value. |
| `ShowNav` | `bool` | true | |
| `ShowStatusBar` | `bool` | true / false on sub-shell | Explicit set wins over sub-shell coerce. |
| `IsSubShell` | `bool` | false | |
| `ShellDepth` | `int` | 0 | Coerced 0–2. |
| `MaxNavDepth` | `int` | 3 | Coerced 1–3. |
| `NavIndent` | `int` | 20 | Coerced 0–80. |
| `Level0Items` / `Level1Items` / `Level2Items` | `IList?` | — | Read-only visible rows. |
| `Level0Selected` / `Level1Selected` | `VestigiumNavItem?` | — | Read-only. |
| `Level0Margin` / `Level1Margin` / `Level2Margin` | `Thickness` | 0 / 20 / 40 | Derived. |
| `Status` | `VestigiumStatusBarViewModel` | Owned Ready / Bottom | Never null. |
| `StatusBarPosition` | `VestigiumStatusBarPosition` | Bottom | TwoWay. |
| `ThemeResources` | `ResourceDictionary?` | null | Instance merge. |
| `SelectItemCommand` | `ICommand` | — | Radio click. |

### 9.2 `VestigiumNavItem`

| Member | Notes |
|---|---|
| `Key` | Lookup key. Follows Header until independently set. |
| `Header` | Radio label. |
| `Content` | Optional replacement view. |
| `Placeholder` | Owned `VestigiumUnderConstruction`. |
| `DisplayContent` | `Content ?? Placeholder`. |
| `Children` | `ObservableCollection<VestigiumNavItem>`. |
| `Command` | Optional extra command. |
| `IsEnabled` / `IsSelected` | `IsSelected` TwoWay from the radio. |
| `TitleOverridden` | Header no longer writes Title. |
| `SetPlaceholder(...)` | Title, Subject, Description, Image, ImageUri. |
| `RestorePlaceholder()` | `Content = null`. |

### 9.3 `VestigiumDefaultWindow`

| Member | Notes |
|---|---|
| `HostShell` | The inner `VestigiumShell`. |
| `ViewModel` | `VestigiumDefaultWindowViewModel`. |
| File → Exit | Closes the window. |
| View | Status visible, Bottom / Top, indent ±4, optional staging commands if present on the VM. |

---

## 10. Demo

`Vestigium.Controls.Demo` SHALL open a **gallery window** (`MainWindow` + `GalleryViewModel`), not a bare form.

1. Live `VestigiumShell` (radios + construction + status bar) with caption that this control has no File menu.
2. File / View on the **gallery window** only, using `MenuChrome.xaml`.
3. Always-nested sample (`IsSubShell`, no status bar, Overview / Activity / Alerts).
4. Stage PingIQ / TraceIQ / DnsIQ / CertIQ / Settings.
5. Replace PingIQ with a `TabControl` + inner `VestigiumShell` (Live / History / Thresholds) and a Notes construction tab.
6. Restore selected placeholder.
7. Toggle status bar visibility, Top / Bottom, nested submenu, `NavIndent` slider 0–80 step 4.
8. Reset to Home / Workspace / Settings including Home’s nested pages.
9. Title, subject, and description remain readable in the nested sample (compact construction glyph + `ScrollViewer`).

The demo SHALL NOT initialize Vestigium.Themes.

---

## 11. Tests

Tests that construct `FrameworkElement` / `Window` SHALL be `[StaFact]` (Xunit.StaFact). Engine-only tests stay `[Fact]`.

| ID | Assertion |
|---|---|
| SH-T01 | `Stage()` seeds Home, Workspace, Settings; Home has Overview / Live / Shortcuts; Shortcuts has Favorites / Recent |
| SH-T02 | Distinct Placeholder instances; Workspace subject stays `PlaceholderSubject` after Home subject changes |
| SH-T03 | Named stage + indexer case-insensitive |
| SH-T04 | Content replace / `RestorePlaceholder` |
| SH-T05 | Header rename updates Title until `SetPlaceholder(title:)` |
| SH-T06 | `IsSubShell` hides status bar unless `ShowStatusBar` is set; `ShellDepth` becomes 1 |
| SH-T07 | Fourth nav level is clipped |
| SH-T08 | Pinned `Content` wins over the selected item |
| SH-T09 | Default window exposes `HostShell` with three items |
| SH-T10 | `NavIndent` default 20, coerced 0–80, `LevelNMargin` updates |

---

## 12. Non-goals

- Ribbon / Fluent NavigationView clone
- Custom caption buttons
- Required icons on nav items
- Auto-building menus by reflecting assemblies
- Embedding ThemeManager or a Theme DP that names Light Blue / Dracula
- Left / Right status rails on the shell (status is Top / Bottom only)
- Fourth nav indent level
- Sharing one Under Construction instance across items

---

## 13. Document control

| Version | Change |
|---|---|
| 1.0 | Skeleton window + DI |
| 1.1 | Host assigns theme |
| 1.2 | `VestigiumShell`, staging factory, nested forms, per-item construction, parent-down theme |
| 1.3 | Gallery demo, `NavIndent`, nested Home pages, on-chrome nav, compact construction, STA tests |
| 1.4 | Lossless: MenuChrome, HostShell/RootShell, Status coerce, DataContext order, spec matrix, radio underline, compact construction, gallery contract |
