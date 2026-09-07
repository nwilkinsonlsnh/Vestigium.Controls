# Vestigium.Controls.StatusBar — Software Requirements Specification

**Document ID:** VEST-CTL-SB-SRS-001  
**Version:** 1.3  
**Status:** Implemented — `Vestigium.Controls.StatusBar.Demo` is the acceptance host  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM  
**Supersedes:** v1.2 (skeleton / draft)

This document is the source of truth for the shipped control. The demo project is fully functional.

---

## 1. Product

A reusable status bar for every Vestigium window (default form, PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost).

The bar is a **horizontal strip** of addressed columns grouped into three slots: **Left**, **Center**, **Right**. Workers and ViewModels write by **index** or **key**. The strip docks **Bottom** (default) or **Top**. It stretches to the host width. Thickness is settable.

It is a new control, not a restyle of `System.Windows.Controls.Primitives.StatusBar`.

### 1.1 What v1 is for

Diagnostic chrome. A probe is running, a last result landed, a clock is visible, an icon says healthy or not. Updates arrive from background workers faster than the frame rate. The bar coalesces those updates, never throws on the dispatcher, and never requires the host to write a debounce timer.

### 1.2 What v1 is not

Not a vertical rail. Not a toolbar. Not an input surface. Not a theme engine.

Left / Right **window docking**, rotated content, IconFlyout, TextBox / ComboBox / CheckBox columns, and an SVG parser are later work (§16). They are not this contract.

---

## 2. Binding architecture

### 2.1 MVVM

- `StatusBarEngine` owns columns, the flood pipeline, idle, and the clock. It implements `IUpdateStatusBar` and `IDisposable`.
- `VestigiumStatusBar` is the visual. It binds to an engine, docks itself, and lays columns into Left / Center / Right.
- `VestigiumStatusBarViewModel` wraps an engine for the default form (`Position`, `Message` convenience, `IdleRemainingMs`).
- Control code-behind may set `DockPanel.Dock`, start runtime on `Loaded`, and stop owned engines on `Unloaded`. It does not call logging, networking, or theme APIs.

Shipped shape: `UserControl` with an in-assembly template. A later revision may promote to a lookless `Control` + `Themes/Generic.xaml` without changing the worker API.

### 2.2 No dispatcher throws

Unknown enums, negative progress, null text, missing keys, posts after dispose, and missing glyphs snap to a documented fallback. Coerce / convert / `Post` never throw on the UI thread.

### 2.3 Theming is assigned by the host

This assembly SHALL NOT:

- Project-reference `Vestigium.Themes`, `Vestigium.Themes.Controls`, or any palette.
- Call `ThemeManager`.
- Expose a `Theme` property.
- Ship a palette.

The host calls `ThemeManager.Initialize` in its own `OnStartup` before the first window. This control consumes keys if they already exist in `Application.Resources`. Until those keys are present, hardcoded fallbacks paint:

| Token | Fallback |
|---|---|
| Background | `#0F2744` |
| Foreground | `#E2E8F0` |
| Hairline | `#1E3A5F` |
| Muted / idle text | `#94A3B8` |
| Accent / progress | `#4A90C8` |
| Success / warning / error / info | `#3BB273` / `#D83B01` / `#E81123` / `#4A90C8` |
| Thickness | `28` |

`ThemeManager.Unload()` returns the bar to those fallbacks. The control keeps working.

### 2.4 No suite module references

No project reference to PingIQ, DnsIQ, TraceIQ, HttpIQ, Vestigium.Logging, Vestigium.Themes, or Vestigium.Converters.

---

## 3. Placement (Top / Bottom only)

```csharp
public enum VestigiumStatusBarPosition
{
    Bottom = 0,
    Top = 1
}
```

Default: `Bottom`.

| Position | `DockPanel.Dock` | Hairline |
|---|---|---|
| Bottom | `Bottom` | Top edge of the bar |
| Top | `Top` | Bottom edge of the bar |

The control docks **itself** when its parent is a `DockPanel`. Hosts do not attach a dock converter.

On `VestigiumDefaultWindow` the `Menu` is always the first child docked `Top`.

- Bottom — menu, content, bar.
- Top — menu, **bar immediately under the menu**, content.

The bar never covers the menu and never replaces it.

`Position` is a bindable DP. Changing it re-docks without recreating the window. Column state is preserved.

Left / Right **window** rails, overlay, auto-hide, and dual bars in one instance are out of scope. A host that wants two bars creates two controls.

---

## 4. Columns and slots

The bar is three zones:

```
[ Left (pack start) | Center (middle band) | Right (pack end) ]
```

```csharp
public enum StatusBarSlot
{
    Left = 0,
    Center = 1,
    Right = 2
}
```

Each `StatusBarColumn` has a `Slot`. Index is still identity for `Post(int)`, but **visual location is `Slot`**, not list order within the full collection. Order inside a slot follows collection order among siblings in that slot.

### 4.1 Collection

`StatusBarEngine.Columns` is an `ObservableCollection<StatusBarColumn>`. That collection is the source of truth.

There is no `ColumnCount` set-once API. Count is `Columns.Count`.

- Hosts add columns in the engine constructor (`StatusBarColumns.Standard()` or a custom list).
- Duplicate keys: last column with that key wins lookup. No throw.

### 4.2 Column identity

```csharp
public sealed partial class StatusBarColumn : ObservableObject
{
    public string? Key { get; set; }
    public StatusBarColumnKind Kind { get; set; }       // default Text
    public StatusBarColumnWidth Width { get; set; }     // default Auto
    public StatusBarSlot Slot { get; set; }             // default Left
    public double WidthDip { get; set; }                // default 120
    public string Text { get; set; }
    public string? ToolTip { get; set; }
    public double Progress { get; set; }                // 0–100
    public bool IsIndeterminate { get; set; }
    public bool IsProgressVisible { get; set; }
    public StatusBarIconKind Icon { get; set; }         // default None
    public string IdleText { get; set; }                // default "Idle. . ."
    public int IdleTimeoutMs { get; set; }              // default 0 except live message
    public bool IsLiveRegion { get; set; }
    public bool IsIdle { get; set; }
}
```

- Empty / whitespace `Text` on a live-region Text column displays `"Ready"`.
- A Progress column with `IsProgressVisible = false` collapses (no gap, no separator).
- Star columns keep sharing leftover width.

### 4.3 Standard recipe

`StatusBarColumns.Standard()`:

| Key | Kind | Slot | Width | Role |
|---|---|---|---|---|
| `message` | Text | Left | Star | Live message, idle on |
| `progress` | Progress | Center | Fixed 120 | Hidden until a post shows it |
| `detail` | Text | Right | Auto | Trailing fact |
| `clock` | Clock | Right | Auto | Local `HH:mm:ss` |

Helpers also exist for document, probe, icon-strip, and groups demos.

### 4.4 Thickness

`BarThickness` default `28`. Below `20` coerces to `20`. Above `64` coerces to `64`. The long edge always stretches to parent width.

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

| Kind | Presents | Notes |
|---|---|---|
| Text | Optional icon + single-line `TextBlock` | Ellipsis + tooltip of full text |
| Progress | Determinate 0–100 or indeterminate | Height 8–12 |
| Clock | Local `HH:mm:ss` | Own 1 s tick; not a worker post |
| Icon | Built-in glyph only | 16×16 dip |
| Empty | No visual | Explicit spacer |

`Button` is not a v1 kind. Input kinds are not v1 kinds.

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

Built-in glyphs are `Geometry` / `Path` resources in this assembly. No SVG parser in v1.

Resolution: `Icon` if not `None`, else no glyph. A later `IconSource` override may be added without changing `Post`.

---

## 7. Worker API and flood pipeline

Background probes must not touch dependency properties. They call `IUpdateStatusBar`.

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

`StatusBarEngine` implements the interface. The host does **not** write debounce timers, `Dispatcher.BeginInvoke`, or a `CancellationToken` around the bar.

### 7.1 Update payload

```csharp
public sealed class StatusBarUpdate
{
    public string? Text { get; init; }
    public double? Progress { get; init; }
    public bool? IsIndeterminate { get; init; }
    public bool? IsProgressVisible { get; init; }
    public StatusBarIconKind? Icon { get; init; }
    public bool Immediate { get; init; }
}
```

Unset properties mean “leave the current value.”

### 7.2 Queue (self-contained anti-lockup)

- `Post` is safe from any thread.
- Pending map is locked. **One pending record per column.** Last write wins. That is cancellation of stale posts — no consumer-facing CTS.
- Column objects and `INotifyPropertyChanged` mutate **only on the dispatcher**.
- A post that equals last **applied** values is dropped (`DroppedDuplicate++`).
- `Post` after `Dispose` is a no-op (`DroppedDisposed++`).
- Unknown index / key: drop (`DroppedMissingColumn++`). No throw.
- Inbound high water (256 since last flush) is **diagnostic** (`DroppedHighWater++`). It does **not** wipe other columns’ pending records. Memory is already bounded by column count.

### 7.3 Drain

Interval **100–250 ms**, AIMD:

- Floor 100 ms while posts are landing.
- +25 ms per quiet tick, cap 250 ms.
- ×0.5 (floor 100) when a drain applied at least one column.
- If a drain is already running, skip and reschedule (auto-pause).
- `PostImmediate` skips backoff **and** progress epsilon. On the UI thread it drains now; from a worker it `BeginInvoke`s at `DispatcherPriority.Input`.

16 ms / `Render` is forbidden. This bar is not a frame loop.

### 7.4 Progress epsilon

- Ignore a new determinate value if `abs(new - applied) < 0.5` **and** last apply for that column was less than 500 ms ago.
- Always apply 0, 100, and indeterminate transitions.
- `PostImmediate` skips the epsilon.
- Values coerce to `[0, 100]`.

### 7.5 Idle

Live-region message columns **default to idle on**: `IdleTimeoutMs = 3000`, `IdleText = "Idle. . ."`.

Other columns default to `IdleTimeoutMs = 0` (off). `SetIdlePolicy(timeoutMs, idleText)` applies to live-region Text columns. Timeout `0` disables idle.

When armed and no apply has landed for `IdleTimeoutMs`, the drain writes `IdleText`, clears the icon, and sets `IsIdle = true`. Idle never runs on Clock or Empty. A new `Post` clears `IsIdle` and resets the timer.

### 7.6 Dispose / Unloaded

1. Disposed flag. Further `Post` is a no-op.
2. Stop drain timer and clock timer.
3. Clear pending.

The host SHOULD dispose the engine from window `Closed`. The control SHALL dispose an engine it created on `Unloaded`. Host-supplied engines are not disposed by the control (nested panes share lifetime with the demo window).

---

## 8. Clock

A `Clock` column formats `DateTime.Now` as `HH:mm:ss` (24-hour, local). Tick at most once per second on the dispatcher.

Not written by `Post`. Not a live region. Not subject to idle. No date, UTC toggle, or stopwatch.

---

## 9. Snapshot

`Snapshot()` is pull-only. It does not flush. Safe from any thread.

```csharp
public sealed class StatusBarSnapshot
{
    public IReadOnlyList<StatusBarColumnSnapshot> Columns { get; init; }
    public VestigiumStatusBarPosition Position { get; init; }
    public double BarThickness { get; init; }
    public int DroppedDuplicate { get; init; }
    public int DroppedHighWater { get; init; }
    public int DroppedDisposed { get; init; }
    public int DroppedMissingColumn { get; init; }
    public int Compacted { get; init; }
    public int Applied { get; init; }
    public int DrainIntervalMs { get; init; }
    public int IdleRemainingMs { get; init; }
}
```

| Counter | Meaning |
|---|---|
| Applied | Paints that landed on a column |
| Compacted | Last-write-wins replacements while a column already had pending |
| DroppedDuplicate | Payload equal to last applied |
| DroppedHighWater | Posts beyond 256 since last flush (flood marker) |
| DroppedMissingColumn | Unknown index or key |
| DroppedDisposed | `Post` after `Dispose` |
| DrainIntervalMs | Current AIMD interval |
| IdleRemainingMs | Milliseconds until idle on the live message; `0` if idle; `-1` if idle off |

---

## 10. Public surface

### 10.1 Control

```csharp
public partial class VestigiumStatusBar : UserControl
{
    public StatusBarEngine Engine { get; set; }
    public VestigiumStatusBarPosition Position { get; set; }
}
```

No `Theme` property. No `Message` DP — text lives on columns.

### 10.2 Engine

```csharp
public sealed class StatusBarEngine : ObservableObject, IUpdateStatusBar, IDisposable
{
    public ObservableCollection<StatusBarColumn> Columns { get; }
    public VestigiumStatusBarPosition Position { get; set; }
    public double BarThickness { get; set; }
    public void StartRuntime(Dispatcher? dispatcher = null);
    public void SetIdlePolicy(int idleTimeoutMs, string? idleText = null);
}
```

### 10.3 XAML

```xml
xmlns:vsb="clr-namespace:Vestigium.Controls.StatusBar;assembly=Vestigium.Controls.StatusBar"

<vsb:VestigiumStatusBar Engine="{Binding Status.Engine}"
                        Position="{Binding Status.Position}"/>
```

### 10.4 Worker example

```csharp
_status.Post("message", new StatusBarUpdate { Text = $"Hop {n}" });
_status.Post("progress", new StatusBarUpdate { Progress = n, IsProgressVisible = true });
_status.PostImmediate("message", new StatusBarUpdate { Text = "Probe complete", Icon = StatusBarIconKind.Success });
```

That is the whole API. No timer in the ViewModel.

---

## 11. Accessibility

- Control name defaults to `"Status"`.
- Live-region columns (`IsLiveRegion = true`) are polite. `Standard()` marks only `message`.
- Clock ticks and identical idle rewrites do not need a new announcement.
- Determinate progress exposes range values.
- Built-in icons have a text equivalent (`Success`, `Warning`, …).
- Fallback contrast meets WCAG AA against the fallback background.
- Demo selected tabs and pressed buttons use accent fill with dark foreground so labels stay readable.

---

## 12. Default form

`VestigiumDefaultWindow` hosts exactly one bar bound to `Status.Engine`.

| Property | Startup value |
|---|---|
| Position | Bottom |
| BarThickness | 28 |
| Columns | `StatusBarColumns.Standard()` |
| message.Text | `Ready` |
| message idle | 3000 ms, `Idle. . .` |
| progress | collapsed |
| clock | visible, Right slot |

View → Status bar → Bottom / Top toggles `Position` without restart.

---

## 13. Demo (`Vestigium.Controls.StatusBar.Demo`)

The demo is the acceptance host. It SHALL demonstrate:

1. **Gallery — slots.** Live Left / Center / Right playground. Move an item between slots.
2. **Gallery — placement.** Window bar Top and Bottom, plus mini frames showing each dock.
3. **Gallery — idle, probe, glyphs, standard chrome.**
4. **Nested.** Editor, PingIQ session, and an output pane inside an outer document; each owns an engine.
5. **Lab.** Post / Immediate / Ready / Flood / icons against the window chrome engine.
6. **Settings.** Idle string and timeout (defaults `Idle. . .` / 3000 ms).
7. **Snapshot.** Live counters, Flood, duplicate, missing key, capture JSON. Copy explains each counter.

The demo does not initialize Vestigium.Themes. Fallback paints are enough.

---

## 14. Tests

| ID | Assertion |
|---|---|
| SB-T01 | Default `Position` is `Bottom` |
| SB-T04 | Live-region text whitespace displays `"Ready"` |
| SB-T05 | Progress `-10` → `0`; `140` → `100` |
| SB-T08 | StatusBar csproj has no `Vestigium.Themes*` `ProjectReference` |
| SB-T09 | Parallel `Post` from worker threads does not throw; last Immediate wins |
| SB-T11 | Duplicate of last applied is dropped |
| SB-T13 | `PostImmediate` applies a 0.2 progress delta |
| SB-T14 | `Post` after `Dispose` increments `DroppedDisposed` |
| SB-T15 | `Post` to a missing key increments `DroppedMissingColumn` |
| SB-T16 | Idle does not fire when `IdleTimeoutMs = 0` |
| SB-T17 | Armed idle rewrites text to `IdleText` after timeout |
| SB-T19 | `BarThickness` 10 → 20; 80 → 64 |
| SB-T21 | Standard columns: message Left, progress Center, clock Right |
| SB-T22 | High water does not wipe a pending message while progress floods |

---

## 15. Acceptance

1. Demo §13 items 1–7 work on Generic fallbacks.
2. Hold Flood; the window still drags; Compacted rises; Applied stays far below hop count.
3. Idle rewrites the chrome bar to `Idle. . .` after 3 s.
4. Nested child Post does not paint the window bar.
5. Slot playground moves an item far left / center / far right.
6. Top dock sits under the menu.
7. SB-T01… listed tests pass.
8. No Themes project reference.

---

## 16. Later

- Lookless `Control` + `Generic.xaml` DynamicResource keys.
- `IconSource` host override.
- SVG cache (`ConcurrentDictionary<string, WeakReference<DrawingImage>>`) if a parser is ever added.
- Command / Button column.
- Input columns and IconFlyout.
- Left / Right **window** rails.
- Optional Vestigium.Logging adapter in a **different** assembly.
