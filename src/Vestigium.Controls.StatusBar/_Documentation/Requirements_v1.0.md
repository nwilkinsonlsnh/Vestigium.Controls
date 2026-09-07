# Vestigium.Controls.StatusBar — Software Requirements Specification

**Document ID:** VEST-CTL-SB-SRS-001  
**Version:** 1.2  
**Status:** Draft for review — not accepted, Build Mode not open  
**Date:** 6 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM

This document is the source of truth for `Vestigium.Controls.StatusBar`. The types already in the repository are a compile skeleton only.

---

## 1. Product

A reusable status bar for every Vestigium window (default form, PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost).

The bar is a **horizontal strip** of addressed slots. Workers and ViewModels write by **index** or **key**. The strip docks **Bottom** (default) or **Top**. It stretches along the width of the host. Thickness is settable.

It is a new control, not a restyle of `System.Windows.Controls.Primitives.StatusBar`. Hosts do not set `Style="{StaticResource StatusBar.Standard}"`. Vestigium.Themes.Controls may keep styling the stock bar for applications that have not adopted this library.

### 1.1 What v1 is for

Diagnostic chrome. A probe is running, a last result landed, a clock is visible, an icon says healthy or not. Updates arrive from background workers faster than the frame rate. The bar must coalesce those updates and never throw on the dispatcher.

### 1.2 What v1 is not

Not a vertical rail. Not a toolbar. Not an input surface. Not a theme engine.

Left / Right placement, rotated content, IconFlyout, TextBox / ComboBox / CheckBox slots, and an SVG parser are recorded in §16 as later work. They are not part of this contract.

---

## 2. Binding architecture

### 2.1 MVVM

- `VestigiumStatusBar` is lookless (`Control` + `Themes/Generic.xaml`).
- `VestigiumStatusBarViewModel` owns columns, position, and the update pipeline.
- The default form window ViewModel **owns** one `VestigiumStatusBarViewModel` and binds the control to it.
- Control code-behind may set `DockPanel.Dock`, wire template parts, and start/stop the clock. It does not call logging, networking, or theme APIs.

### 2.2 No dispatcher throws

Unknown enums, negative progress, null text, missing keys, posts after dispose, and malformed icons snap to a documented fallback. Coerce / convert / `Post` never throw on the UI thread.

### 2.3 Theming is assigned by the host

This assembly SHALL NOT:

- Project-reference `Vestigium.Themes`, `Vestigium.Themes.Controls`, or any palette.
- Call `ThemeManager`.
- Expose a `Theme` property.
- Ship a palette.

The host calls `ThemeManager.Initialize` in its own `OnStartup` before the first window, as required by the Vestigium.Themes Developers Guide. This control only consumes keys if they already exist in `Application.Resources`.

| Role | Dynamic resource |
|---|---|
| Bar background | `Vestigium.Brushes.Surface.StatusBar` then `Vestigium.Brushes.Surface.Header` |
| Primary text | `Vestigium.Brushes.Text.Primary` |
| Secondary / idle / clock | `Vestigium.Brushes.Text.Secondary` |
| Hairline | `Vestigium.Brushes.Stroke.Subtle` |
| Progress fill | `Vestigium.Brushes.Accent.Primary` |
| Success / warning / error / info icons | `Vestigium.Brushes.Status.Success` / `Warning` / `Error` / `Info` |

`Themes/Generic.xaml` in **this** project is WPF default-style plumbing. Fallback paints when the host has not loaded a palette:

| Token | Fallback |
|---|---|
| Background | `#0F2744` |
| Foreground | `#E2E8F0` |
| Hairline | `#1E3A5F` |
| Muted text | `#94A3B8` |
| Thickness | `28` |

`ThemeManager.Unload()` returns the bar to those fallbacks. The control keeps working.

### 2.4 No suite module references

No project reference to PingIQ, DnsIQ, TraceIQ, HttpIQ, Vestigium.Logging, Vestigium.Themes, or Vestigium.Converters. A later logging adapter lives in a different assembly.

Local value converters that this control needs (none required for docking) stay in this project.

---

## 3. Placement (Top / Bottom only)

```csharp
namespace Vestigium.Controls.StatusBar;

public enum VestigiumStatusBarPosition
{
    Bottom = 0,
    Top = 1
}
```

Default: `Bottom`.

### 3.1 Dock

The control docks **itself** when its parent is a `DockPanel`. Hosts do not attach a dock converter.

| Position | `DockPanel.Dock` | Hairline |
|---|---|---|
| Bottom | `Bottom` | Top edge of the bar |
| Top | `Top` | Bottom edge of the bar |

If the parent is not a `DockPanel`, `Position` still flips the hairline and the automation description. Layout is then the host's problem (Grid row, etc.).

### 3.2 Default form

On `VestigiumDefaultWindow` the `Menu` is always the first child docked `Top`.

- Bottom — menu, content, bar.
- Top — menu, **bar immediately under the menu**, content.

The bar never covers the menu and never replaces it.

### 3.3 Runtime change

`Position` is a bindable DP and a VM property. Changing it re-docks without recreating the window. Column state is preserved.

### 3.4 Not in this version

Left, Right, overlay, auto-hide, dual bars in one instance. A host that wants two bars creates two controls.

---

## 4. Columns

The bar is an `ItemsControl` of columns, left to right. **Index is location.** Index `0` is the left-most slot.

### 4.1 Collection

`VestigiumStatusBarViewModel.Columns` is an `ObservableCollection<StatusBarColumn>`. That collection is the source of truth. The control binds `ItemsSource` to it.

There is no `ColumnCount` set-once API. Count is `Columns.Count`.

- Hosts add columns in XAML or in the VM constructor / `Loaded` path.
- Growing at runtime is allowed.
- Removing a column at runtime is allowed. In-flight `Post` to that index or key is dropped (`DroppedMissingColumn++`).
- Reordering is allowed; workers that address by **key** keep working. Workers that address by index must follow the new order.

### 4.2 Column identity

```csharp
public sealed partial class StatusBarColumn : ObservableObject
{
    public string? Key { get; set; }          // optional, case-sensitive, unique among siblings
    public StatusBarColumnKind Kind { get; set; } // default Text
    public StatusBarColumnWidth Width { get; set; } // default Auto
    public double WidthDip { get; set; }      // used when Width == Fixed; default 120
    public string Text { get; set; }          // default ""
    public string? ToolTip { get; set; }
    public double Progress { get; set; }      // 0–100
    public bool IsIndeterminate { get; set; }
    public StatusBarIconKind Icon { get; set; } // default None
    public ImageSource? IconSource { get; set; } // host override; wins over Icon
    public string IdleText { get; set; }      // default "Idle. . ."
    public int IdleTimeoutMs { get; set; }    // default 0 = idle off
    public bool IsLiveRegion { get; set; }    // default false; message column sets true
}
```

Rules:

- `Key` null is fine. Duplicate keys in the same bar are a host error: last column with that key wins lookup; a debug-only trace is written; no throw.
- `Kind` defaults to `Text`.
- Empty / whitespace `Text` on a `Text` column displays `"Ready"` only when `IsLiveRegion` is true (the message slot). Other text columns display nothing and collapse their glyph, not the slot.
- `IconSource` wins over `Icon`. Null `IconSource` and `Icon = None` means no glyph.

### 4.3 Width

```csharp
public enum StatusBarColumnWidth
{
    Auto = 0,   // size to content, cap at remaining space
    Star = 1,   // share leftover width with other Star columns
    Fixed = 2   // WidthDip device-independent pixels
}
```

Recommended default recipe (also what `CreateStandardColumns()` builds):

| Index | Key | Kind | Width | Role |
|---|---|---|---|---|
| 0 | `message` | Text | Star | Live message |
| 1 | `progress` | Progress | Fixed 120 | Hidden until a post sets progress visible |
| 2 | `detail` | Text | Auto | Trailing fact (host, encoding, theme name) |
| 3 | `clock` | Clock | Auto | Local `HH:mm:ss` |

A Progress column with no active progress **collapses** (no gap, no separator). A Text column with `IsLiveRegion = false` and empty text **collapses**. Star columns never collapse; they keep sharing leftover width.

`CreateStandardColumns()` is a static helper on the ViewModel. Hosts may ignore it and build any list.

### 4.4 Thickness

`BarThickness` (DP + VM), default `28`. This is the cross-axis size (height of a horizontal bar). Values below `20` coerce to `20`. Values above `64` coerce to `64`. The long edge always stretches to the parent width.

---

## 5. Kinds (v1)

```csharp
public enum StatusBarColumnKind
{
    Text = 0,
    Progress = 1,
    Clock = 2,
    Icon = 3,
    Empty = 4
}
```

Presenters are `DataTemplate`s selected by `Kind`. Implementation: `ItemsControl` + `DataTemplateSelector`. No virtualization (N is small).

| Kind | Presents | Notes |
|---|---|---|
| Text | Optional icon + single-line `TextBlock` | Ellipsis + tooltip of full text when truncated |
| Progress | Determinate 0–100 bar, or indeterminate | Width from the column; height 12 |
| Clock | Local `HH:mm:ss` | Own 1 s tick; not a worker post; not a live region |
| Icon | Built-in or `IconSource` only | 16×16 dip |
| Empty | No visual | Used as an explicit spacer when the host does not want the message column to be the only Star |

`Button` is not a v1 kind. A later version may add a command column. Until then, hosts put actions on the menu or toolbar.

Input kinds (TextBox, ComboBox, CheckBox) are not v1 kinds.

---

## 6. Icons

```csharp
public enum StatusBarIconKind
{
    None = 0,
    Info = 1,
    Success = 2,
    Warning = 3,
    Error = 4,
    Busy = 5
}
```

Built-in icons ship as `DrawingImage` / `Geometry` resources **inside this assembly**. No SVG parser in v1. No third-party vector package in v1.

Paint:

- Built-in glyphs bind fill to the matching `Vestigium.Brushes.Status.*` key when present, otherwise to Generic.xaml fallbacks (`Success #3BB273`, `Warning #D83B01`, `Error #E81123`, `Info #0078D7`, `Busy` uses accent).
- `IconSource` is taken as-is. The control does not recolor host bitmaps.

Resolution order for a column glyph:

1. `IconSource` if not null.
2. Built-in `Icon` if not `None`.
3. No glyph.

A broken `ImageSource` (decode failure) is treated as no glyph. No throw.

If a later version adds SVG, the cache shape already decided is: `ConcurrentDictionary<string, WeakReference<DrawingImage>>`, `TryGetTarget`, replace dead entries, prune dead keys on miss and at most every 60 s, cap 64 live keys. That cache is **not** implemented in v1.

---

## 7. Worker API and update pipeline

Background probes must not touch dependency properties. They call `IUpdateStatusBar` on the ViewModel.

```csharp
public interface IUpdateStatusBar
{
    void Post(int index, StatusBarUpdate update);
    void Post(string key, StatusBarUpdate update);
    void PostImmediate(int index, StatusBarUpdate update);
    void PostImmediate(string key, StatusBarUpdate update);
    StatusBarSnapshot Snapshot();
}
```

`VestigiumStatusBarViewModel` implements the interface. Register it in DI as `IUpdateStatusBar` (and as itself) from `AddVestigiumStatusBar()`.

### 7.1 Update payload

```csharp
public sealed class StatusBarUpdate
{
    public string? Text { get; init; }
    public double? Progress { get; init; }
    public bool? IsIndeterminate { get; init; }
    public StatusBarIconKind? Icon { get; init; }
    public bool Immediate { get; init; }
}
```

Unset properties mean “leave the current value.” `Post(..., new StatusBarUpdate { Text = "Probe done" })` does not clear progress or icon.

### 7.2 Queue

- Thread-safe. `Post` from any thread.
- **One pending record per column.** Last write wins.
- A global inbound cap of 256 posts exists only as flood armor. On overflow, compact to the latest post per column (`Compacted++`) and drop the rest (`DroppedHighWater++`).
- A post that equals the last **applied** values for that column is dropped (`DroppedDuplicate++`).
- `Post` after `Dispose` is a no-op (`DroppedDisposed++`).
- Unknown index / unknown key: drop (`DroppedMissingColumn++`). No throw.

### 7.3 Drain timer (not 16 ms)

`DispatcherTimer`, `DispatcherPriority.Background`, interval **100–250 ms**.

- Start at 100 ms while posts are landing.
- Additive increase +25 ms per quiet tick, cap 250 ms, when the pending map stays empty.
- Multiplicative decrease ×0.5 (floor 100 ms) when a drain applied at least one column.
- `PostImmediate` bypasses backoff **and** the progress epsilon: apply on the next dispatcher turn (`Dispatcher.BeginInvoke` at `Background`).

16 ms / `Render` is forbidden. This bar is not a frame loop. The floor matches the Vestigium.Logging UI batch interval so a live probe grid and the bar do not contend.

### 7.4 Progress epsilon

On `Kind = Progress` (or a Text column that also carries progress):

- Ignore a new determinate value if `abs(new - applied) < 0.5` **and** the last apply for that column was less than 500 ms ago.
- Always apply 0, 100, and a transition into / out of indeterminate.
- `PostImmediate` skips the epsilon.

Progress values coerce to `[0, 100]`.

### 7.5 Idle (opt-in)

Each column has `IdleTimeoutMs` (default **0** = off) and `IdleText` (default `"Idle. . ."`).

When `IdleTimeoutMs > 0` and the column has not received an applied update for that many milliseconds, the drain tick writes `IdleText` into `Text` and sets `Icon` to `None` unless the column is `Kind = Clock` or `Kind = Empty`.

Idle never runs on Clock or Empty. Idle does not announce through the live region if the new text equals the last announced text.

### 7.6 Dispose / Unloaded

Order:

1. Set disposed flag. Further `Post` is a no-op.
2. `DispatcherTimer.Stop()`, detach `Tick`, field = null.
3. Stop the clock timer the same way.

The host SHOULD dispose the ViewModel from the window `Closed` handler. The control SHALL also stop both timers on `Unloaded` so a window that is not disposed does not leak.

---

## 8. Clock

A `Clock` column formats `DateTime.Now` as `HH:mm:ss` (24-hour, local time zone). Tick at most once per second, on the dispatcher.

- Not written by `Post`.
- Not a live region.
- Not subject to idle.
- Hidden when the host removes the column.

No date. No UTC toggle. No stopwatch mode.

---

## 9. Snapshot

`Snapshot()` is pull-only. It returns an immutable copy of each column's last **applied** values plus pipeline counters. Safe from any thread.

```csharp
public sealed class StatusBarSnapshot
{
    public IReadOnlyList<StatusBarColumnSnapshot> Columns { get; init; }
    public VestigiumStatusBarPosition Position { get; init; }
    public int DroppedDuplicate { get; init; }
    public int DroppedHighWater { get; init; }
    public int DroppedDisposed { get; init; }
    public int DroppedMissingColumn { get; init; }
    public int Compacted { get; init; }
    public int Applied { get; init; }
}
```

Used by tests and, later, by a Logging adapter. The bar never pushes to Logging.

---

## 10. Public surface

### 10.1 Control

```csharp
public class VestigiumStatusBar : Control
{
    public VestigiumStatusBarPosition Position { get; set; }
    public double BarThickness { get; set; }
    public IList Columns { get; set; } // bound to the VM collection
}
```

There is no `Theme` property. There is no `Message` DP on the control in v1.2 — text lives on columns. The skeleton `Message` DP is removed in Build Mode (breaking vs the skeleton, not vs any shipped package).

### 10.2 ViewModel

```csharp
public sealed partial class VestigiumStatusBarViewModel
    : ObservableObject, IUpdateStatusBar, IDisposable
{
    public VestigiumStatusBarPosition Position { get; set; }
    public double BarThickness { get; set; }
    public ObservableCollection<StatusBarColumn> Columns { get; }
    public ICommand SetPositionBottomCommand { get; }
    public ICommand SetPositionTopCommand { get; }

    public static ObservableCollection<StatusBarColumn> CreateStandardColumns();
}
```

### 10.3 XAML

`xmlns:vsb="http://schemas.vestigium.dev/controls/statusbar"`

```xml
<vsb:VestigiumStatusBar Position="{Binding Status.Position}"
                        BarThickness="{Binding Status.BarThickness}"
                        ItemsSource="{Binding Status.Columns}" />
```

Exact property name on the control (`ItemsSource` vs `Columns`) is an implementation detail as long as the VM collection is the source of truth.

### 10.4 DI

```csharp
services.AddVestigiumStatusBar(); // registers VM + IUpdateStatusBar
services.AddVestigiumControls();  // may call the above when the project is referenced
```

---

## 11. Accessibility

- Control `AutomationProperties.Name` defaults to `"Status"`.
- Exactly the columns with `IsLiveRegion = true` are polite live regions. `CreateStandardColumns()` marks only `message`.
- Clock ticks, idle rewrites that repeat the same string, and progress epsilon drops do not announce.
- A visible determinate Progress column exposes `RangeValue`.
- Built-in icons expose a text equivalent (`"Success"`, `"Warning"`, …).
- Fallback contrast meets WCAG AA against the fallback background.

---

## 12. Default form integration

`VestigiumDefaultWindow` hosts exactly one bar.

| Property | Startup value |
|---|---|
| Position | Bottom |
| BarThickness | 28 |
| Columns | `CreateStandardColumns()` |
| message.Text | `Ready` |
| progress | collapsed |
| clock | visible |

View → Status bar → Bottom / Top toggles `Position` without restart. File → Exit remains the only other live command in the skeleton menu.

The default-form demo does not initialize Vestigium.Themes.

---

## 13. Demo (`Vestigium.Controls.StatusBar.Demo`)

One window, Generic.xaml fallbacks allowed:

1. Bar Bottom, then flip to Top without restart.
2. Live message box bound through `Post` / column `Text` (not control code-behind).
3. Progress checkbox + 0–100 slider, including indeterminate.
4. Icon combo (None / Info / Success / Warning / Error / Busy) on the message or icon column.
5. Trailing detail text (`net10.0-windows` is fine).
6. Clock on/off by adding/removing or collapsing the clock column.
7. Idle opt-in: a button that posts once and a checkbox that sets `IdleTimeoutMs = 3000` so the reviewer can watch idle fire.
8. A `Snapshot()` dump (counters) on a diagnostic expander.

Proving `SwitchTheme` is a **host** concern, not this demo's.

The umbrella `Vestigium.Controls.Demo` only shows the standard columns inside `VestigiumDefaultWindow`.

---

## 14. Tests (Build Mode)

| ID | Assertion |
|---|---|
| SB-T01 | Default `Position` is `Bottom` |
| SB-T02 | `Position = Top` on a `DockPanel` parent sets `DockPanel.Dock` to `Top` |
| SB-T03 | `Position = Bottom` sets `DockPanel.Dock` to `Bottom` |
| SB-T04 | Live-region text null/whitespace displays `"Ready"` |
| SB-T05 | Progress `-10` → `0`; `140` → `100` |
| SB-T06 | Collapsed Progress leaves no gap and no stray separator |
| SB-T07 | Column property changes raise `PropertyChanged` |
| SB-T08 | StatusBar csproj has no `Vestigium.Themes*` `ProjectReference` |
| SB-T09 | `Post` from a thread-pool thread applies on the dispatcher without throw |
| SB-T10 | Two `Post`s to the same index before a drain leave only the last |
| SB-T11 | Duplicate of last applied is dropped |
| SB-T12 | Progress `50.0` then `50.2` inside 500 ms is dropped; `50.0` then `51.0` applies |
| SB-T13 | `PostImmediate` applies a 0.2 progress delta |
| SB-T14 | `Post` after `Dispose` increments `DroppedDisposed` and does not throw |
| SB-T15 | `Post` to a missing key increments `DroppedMissingColumn` |
| SB-T16 | Idle does not fire when `IdleTimeoutMs = 0` |
| SB-T17 | With `IdleTimeoutMs = 50` and no further posts, text becomes `IdleText` |
| SB-T18 | Clock tick does not change live-region content |
| SB-T19 | `BarThickness` 10 → 20; 80 → 64 |
| SB-T20 | `Unloaded` stops the drain timer |

---

## 15. Acceptance

1. Top / Bottom switch at runtime on a `DockPanel`; on the default form Top sits under the menu.
2. Hosts configure columns; workers update them through `IUpdateStatusBar` only.
3. Flood of progress posts does not run a 16 ms render loop and does not throw.
4. Generic.xaml fallbacks render with no Themes reference.
5. A host that initialized a palette and calls `SwitchTheme` repaints the bar through `{DynamicResource}`.
6. Clock and idle do not spam the screen reader.
7. Demo covers §13.
8. SB-T01…T20 pass.

---

## 16. Explicit non-goals (this version)

- Left / Right rails and any `LayoutTransform` rotation
- IconFlyout / Popup editors
- TextBox, ComboBox, CheckBox, Button column kinds
- `UserEditing` / `EditSequence` (no input kinds to protect)
- SVG parser and the SVG cache
- Subscription to `Vestigium.Logging`
- Multi-line text
- Localization of `"Ready"` / `"Idle. . ."` (en-US only)
- Designer toolbox bitmap / VSIX
- Project reference to Vestigium.Themes or a `Theme` DP
- Dual bars in one control

### 16.1 Parked, not rejected

These were useful in the earlier exploration and stay written down so they are not reinvented:

- Vertical rail: display kinds rotate; input kinds never rotate; default vertical input is IconFlyout; popup is not a child of a transform; `CustomPopupPlacementCallback` flips to stay on-screen.
- Input slots: `UserEditing` blocks worker text; `EditSequence` drops stale `Post`s; ComboBox-inside-Popup must not dismiss on drop-down click.
- SVG cache: `ConcurrentDictionary<string, WeakReference<DrawingImage>>`, cap 64, prune on miss and every 60 s.
- Icon adjacent to a future input slot; `CheckBoxTextPlacement`.

---

## 17. Judgment calls (for the reviewer)

| Topic | Call | Why |
|---|---|---|
| Placement | Top and Bottom only | You asked to keep L/R out. Vertical rail is a different control problem. |
| Slots | Named columns, not five hard-coded regions | IQ hosts do not share one layout. Index + key keeps workers dumb. |
| `ColumnCount` set-once | Rejected | Freezes at the wrong time (designer / ctor). Collection is enough. |
| Drain floor | 100 ms, not 16 ms | Same budget as Vestigium.Logging. Status text is not a game frame. |
| Idle | Opt-in (`IdleTimeoutMs = 0`) | A default 3 s rewrite would fight `Ready` and last-result text. |
| Editors / buttons | Out | Toolbar and menu already exist. A status editor fights focus. |
| SVG | Out | WPF has no parser; built-in `DrawingImage` is enough for status glyphs. |
| Dock converter | Out | The control sets dock when the parent is a `DockPanel`. |
| Convenience `Message` DP | Removed | Two sources of truth. Column `message` is the source. |
| Clock | First-class kind | Cheap, expected on diagnostic chrome, isolated from the worker queue. |

---

## 18. Document control

| Version | Change | Source |
|---|---|---|
| 1.0 | First stab: Top/Bottom, five fixed regions | 6 Sep 2026 |
| 1.1 | Host-assigned theming; no Themes reference | Stakeholder |
| 1.2 | Columns + worker pipeline. Top/Bottom only. Gemini ideas kept as parked work. Idle opt-in. Drain 100–250 ms. | Review draft |
