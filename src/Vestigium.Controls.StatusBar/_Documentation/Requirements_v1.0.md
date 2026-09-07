# Vestigium.Controls.StatusBar — Software Requirements Specification

**Document ID:** VEST-CTL-SB-SRS-001  
**Version:** 1.0  
**Status:** First draft — awaiting acceptance before Build Mode  
**Date:** 6 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM

## 1. Objective

Ship a reusable, MVVM-first status bar for every Vestigium window. The control reports application state (ready, busy, last result, clock, optional progress) and can sit at the **top** or the **bottom** of the host client area.

This is the first feature control in `Vestigium.Controls`. The default form in `Vestigium.Controls` hosts one instance. Other IQ applications reuse the same control.

## 2. Why a custom control

`System.Windows.Controls.Primitives.StatusBar` already exists. `Vestigium.Themes.Controls` already styles it (`StatusBar.Standard`, height 28, header surface, top hairline).

That stock control does **not**:

- Expose a first-class `Position` of Top / Bottom that re-docks itself inside a shell.
- Own a documented item contract (message, progress, clock) that ViewModels can bind without assembling `StatusBarItem` trees in every window.
- Guarantee a stable automation name and live-region announcement for screen readers.

`VestigiumStatusBar` wraps those gaps. Internally it may still compose the stock `StatusBar` so Themes styles continue to apply when the host merges Vestigium.Themes.

## 3. Architectural constraints (binding)

### 3.1 MVVM

All mutable state is dependency properties on the control **and** a companion `VestigiumStatusBarViewModel` that hosts bind when they do not want to set DPs from code.

Code-behind of the control may change dock, template parts, and visual state. It does not talk to logging, networking, or application services.

### 3.2 No dispatcher throws

Invalid values (unknown enum, negative progress, null message) snap to a defined fallback. Convert / coerce never throw on the UI thread.

### 3.3 Theme coexistence

| Token | Fallback if Themes is absent |
|---|---|
| Background | `#0F2744` |
| Foreground | `#E2E8F0` |
| Border / hairline | `#1E3A5F` |
| Muted text | `#94A3B8` |
| Height | `28` |

When Themes is present, prefer `Vestigium.Brushes.Surface.Header`, `Vestigium.Brushes.Text.Primary`, `Vestigium.Brushes.Stroke.Subtle`.

### 3.4 No suite module references

The control assembly must not reference PingIQ, DnsIQ, TraceIQ, HttpIQ, or Vestigium.Logging in v1. A later revision may subscribe to `VestigiumLog.Events` behind an optional adapter. That adapter is out of scope for this document.

## 4. Positioning

### 4.1 Enumeration

```csharp
namespace Vestigium.Controls.StatusBar;

public enum VestigiumStatusBarPosition
{
    Bottom = 0,
    Top = 1
}
```

Default: `Bottom`.

### 4.2 Dock behavior

The control is responsible for docking itself when its parent is a `DockPanel`.

| Position | Dock | Edge treatment |
|---|---|---|
| Bottom | `DockPanel.Dock = Bottom` | Hairline on the **top** edge of the bar |
| Top | `DockPanel.Dock = Top` | Hairline on the **bottom** edge of the bar |

When the parent is **not** a `DockPanel` (Grid row, explicit `VerticalAlignment`, etc.), changing `Position` still flips the hairline and the automation description. The host owns layout in that case.

### 4.3 Interaction with the default form menu

On `VestigiumDefaultWindow` the top-level `Menu` is always the first child docked Top.

- `Position = Bottom` — menu at the top of the window, status bar at the bottom, content fills the middle.
- `Position = Top` — menu at the top of the window, status bar **immediately below the menu**, content fills the remainder.

The status bar never covers the menu and never replaces it.

### 4.4 Runtime change

`Position` is a bindable dependency property. Changing it at runtime (demo toggle, user preference) must re-dock without recreating the window. Existing item content is preserved.

### 4.5 Out of scope for v1

- Left / Right vertical status strips
- Floating / auto-hide / overlay
- Dual bars (one top and one bottom) in a single control instance. A host that wants two bars instantiates two controls.

## 5. Visual regions (v1)

Left to right, single row, height 28 dip.

| Region | Name | Default | Bindable |
|---|---|---|---|
| 1 | Message | `"Ready"` | `Message` (`string`) |
| 2 | Optional progress | collapsed | `IsProgressVisible` (`bool`), `ProgressValue` (`double` 0–100) |
| 3 | Flexible spacer | fills leftover width | none |
| 4 | Optional trailing text | empty / collapsed | `TrailingText` (`string`) |
| 5 | Clock | local time `HH:mm:ss` | `IsClockVisible` (`bool`, default true) |

Separators sit between visible regions. A collapsed region also collapses its leading separator.

### 5.1 Message

- Single line. Excess text ellipsizes at the right.
- Tooltip shows the full message when truncated.
- Empty or whitespace message displays `"Ready"`.

### 5.2 Progress

- `ProgressBar` width 120, height 12, `Minimum=0`, `Maximum=100`.
- Values below 0 coerce to 0. Values above 100 coerce to 100.
- `IsIndeterminate` (`bool`, default false) is supported for operations with no percent.

### 5.3 Clock

- Updates at most once per second.
- Uses the local time zone of the host machine.
- Format `HH:mm:ss` (24-hour) in v1. No date. No UTC toggle in v1.

## 6. Public surface (v1)

### 6.1 Control

```csharp
public class VestigiumStatusBar : Control
{
    public VestigiumStatusBarPosition Position { get; set; }
    public string Message { get; set; }
    public string? TrailingText { get; set; }
    public bool IsProgressVisible { get; set; }
    public double ProgressValue { get; set; }
    public bool IsIndeterminate { get; set; }
    public bool IsClockVisible { get; set; }
}
```

All listed members are dependency properties.

### 6.2 ViewModel (for hosts that bind a VM)

```csharp
public sealed partial class VestigiumStatusBarViewModel : ObservableObject
{
    public VestigiumStatusBarPosition Position { get; set; }
    public string Message { get; set; }
    public string? TrailingText { get; set; }
    public bool IsProgressVisible { get; set; }
    public double ProgressValue { get; set; }
    public bool IsIndeterminate { get; set; }
    public bool IsClockVisible { get; set; }
}
```

The default form's window ViewModel **owns** an instance of this type. The window XAML binds the control to that instance.

### 6.3 XAML namespace

`xmlns:vsb="http://schemas.vestigium.dev/controls/statusbar"`

Example:

```xml
<vsb:VestigiumStatusBar Position="{Binding Status.Position}"
                        Message="{Binding Status.Message}"
                        IsClockVisible="True"/>
```

## 7. Accessibility

- Control `AutomationProperties.Name` defaults to `"Status"`.
- Message text is a live region (`LiveSetting = Polite`). Message changes are announced; clock ticks are not.
- Progress exposes `RangeValue` pattern when visible and determinate.
- Contrast of fallback brushes must meet WCAG AA against the fallback background.

## 8. Integration with VestigiumDefaultWindow

The default form always contains exactly one `VestigiumStatusBar`.

Initial values:

| Property | Value |
|---|---|
| Position | Bottom |
| Message | `Ready` |
| IsClockVisible | true |
| IsProgressVisible | false |

The default-form demo exposes a command or toggle that flips `Position` between Top and Bottom so the requirement can be accepted visually.

## 9. Demo project requirements

`Vestigium.Controls.StatusBar.Demo` must show, on one window:

1. The control docked Bottom (default).
2. A control that flips Position to Top without restart.
3. Editable message text bound from the ViewModel.
4. A checkbox that shows/hides progress and a slider that drives 0–100.
5. A checkbox that shows/hides the clock.
6. Trailing text sample (for example `"UTF-8"` or `"net10.0-windows"`).

The umbrella demo (`Vestigium.Controls.Demo`) shows the same bar inside the default form, not a second copy of the full StatusBar laboratory.

## 10. Tests (when Build Mode starts)

| ID | Assertion |
|---|---|
| SB-T01 | Default `Position` is `Bottom` |
| SB-T02 | Setting `Position` to `Top` on a control parented by `DockPanel` sets `DockPanel.Dock` to `Top` |
| SB-T03 | Setting `Position` to `Bottom` sets `DockPanel.Dock` to `Bottom` |
| SB-T04 | `Message = null` or whitespace displays `"Ready"` |
| SB-T05 | `ProgressValue = -10` coerces to `0`; `ProgressValue = 140` coerces to `100` |
| SB-T06 | `IsProgressVisible = false` does not leave a visible gap or stray separator |
| SB-T07 | ViewModel property changes raise `PropertyChanged` for every v1 property |

UI-thread tests may live in `Vestigium.Controls.Tests` with `UseWPF=true`.

## 11. Acceptance criteria

1. A host can place one control in a `DockPanel` and switch Top / Bottom at runtime.
2. On `VestigiumDefaultWindow`, Top places the bar under the menu, never over it.
3. Message, progress, trailing text, and clock bind from a ViewModel with no code-behind.
4. The control renders with fallback brushes when Vestigium.Themes is not referenced.
5. Clock ticks do not flood accessibility live regions.
6. The dedicated StatusBar demo covers every region listed in §5.

## 12. Non-goals (v1)

- Multi-line status text
- Clickable / command status items (network icon, log tail button)
- Subscription to `Vestigium.Logging`
- Per-item templates beyond the five regions above
- Vertical (left/right) placement
- Localization of `"Ready"` (en-US only in v1)
- Designer toolbox bitmap / Visual Studio extension packaging

## 13. Open questions for acceptance

Answer these before Build Mode. Suggested defaults are in parentheses.

1. Should Top place the bar **below** the menu (recommended) or **above** the menu as a second chrome strip?
2. Is a one-second clock required in v1, or is clock a v1.1 extra?
3. Progress: keep a simple 0–100 bar, or also support a busy spinner with no bar?
4. Do we expose an `Items` collection now so hosts can inject extra `StatusBarItem`s, or lock the five regions until a consumer asks?

## 14. Document control

| Version | Change | Source |
|---|---|---|
| 1.0 | First-stab SRS: Top/Bottom position, five regions, default-form integration | Grok, 6 Sep 2026 |
