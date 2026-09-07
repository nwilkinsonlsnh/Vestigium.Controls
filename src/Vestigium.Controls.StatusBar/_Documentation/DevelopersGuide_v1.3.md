# Vestigium.Controls.StatusBar — Developers Guide

**Document ID:** VEST-CTL-SB-DEV-001  
**Version:** 1.3  
**Status:** Current — matches the shipped library and demo  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.3.md`](Requirements_v1.3.md)

## Project

| Item | Value |
|---|---|
| Assembly | `Vestigium.Controls.StatusBar` |
| Target | `net10.0-windows` |
| Demo | `src/Vestigium.Controls.StatusBar.Demo` |
| XAML xmlns | `clr-namespace:Vestigium.Controls.StatusBar;assembly=Vestigium.Controls.StatusBar` |

## Mental model

Workers `Post`. The engine coalesces. The control paints. You do not write a debounce timer.

```
probe thread  →  Post(key, update)  →  one pending slot per column
                                      →  dispatcher drain 100–250 ms AIMD
                                      →  Left | Center | Right presenters
```

`PostImmediate` skips backoff and progress epsilon. It still marshals onto the dispatcher. It does not mean “block until painted.”

## Host a bar

```xml
xmlns:vsb="clr-namespace:Vestigium.Controls.StatusBar;assembly=Vestigium.Controls.StatusBar"

<DockPanel>
  <Menu DockPanel.Dock="Top">…</Menu>
  <vsb:VestigiumStatusBar Engine="{Binding Status.Engine}"
                          Position="{Binding Status.Position}"/>
  <!-- content last, fills -->
</DockPanel>
```

The control docks itself. Put the `Menu` first. Top placement sits **under** the menu, never over it.

```csharp
public sealed class ShellViewModel
{
    public VestigiumStatusBarViewModel Status { get; } = new();
}

// workers
Status.Engine.Post("message", new StatusBarUpdate { Text = $"Hop {n}" });
Status.Engine.Post("progress", new StatusBarUpdate
{
    Progress = percent,
    IsProgressVisible = true
});
Status.Engine.PostImmediate("message", new StatusBarUpdate
{
    Text = "Probe complete",
    Icon = StatusBarIconKind.Success
});
```

`StartRuntime` runs from the control `Loaded` handler. Nested bars each get their own `StatusBarEngine`.

Dispose the engine from `Closed` if you created it. Do not dispose an engine still shown in another pane.

## Columns and slots

`StatusBarColumns.Standard()` is the default recipe:

| Key | Slot | Kind | Notes |
|---|---|---|---|
| `message` | Left | Text | Live region. Idle on, 3 s, `Idle. . .` |
| `progress` | Center | Progress | Collapsed until `IsProgressVisible` |
| `detail` | Right | Text | Trailing fact |
| `clock` | Right | Clock | Local `HH:mm:ss` |

Move a column at runtime:

```csharp
engine.Columns.First(c => c.Key == "progress").Slot = StatusBarSlot.Right;
```

Build a custom strip with `StatusBarColumns.Create` or the Groups / Probe / Document / Icons helpers.

Kinds in v1: `Text`, `Progress`, `Clock`, `Icon`, `Empty`. No editors, no buttons.

## Idle

Live message columns idle after **3 seconds** by default.

```csharp
engine.SetIdlePolicy(3000, "Idle. . ."); // restore defaults
engine.SetIdlePolicy(0);                // off
engine.SetIdlePolicy(5000, "Waiting");  // custom
```

A `Post` that applies resets the timer and clears `IsIdle`. Clock and Empty never idle.

## Flood protection (you do not implement this)

| Rule | Behavior |
|---|---|
| Last-write-wins | One pending record per column |
| AIMD drain | 100–250 ms, Background priority |
| Auto-pause | Skip if a drain is already running |
| Duplicate drop | Same payload as last applied |
| High water | Diagnostic after 256 inbound posts since flush — does not wipe other columns |
| Threading | Pending is locked. Column INPC is dispatcher-only |

If a ViewModel starts a `DispatcherTimer` to “protect” the bar, the pipeline failed.

`Snapshot()` is pull-only. It does not flush. Use it in tests and in the demo Snapshot tab.

```csharp
var snap = engine.Snapshot();
// Applied, Compacted, DroppedDuplicate, DroppedHighWater,
// DroppedMissingColumn, DroppedDisposed, DrainIntervalMs, IdleRemainingMs
```

## Theming

This library has no `Theme` property and no reference to Vestigium.Themes. The host assigns a palette. Fallback paints (`#0F2744` / `#E2E8F0`) keep the bar usable in the demo and in any app that has not loaded ThemeManager yet.

## Default form

`VestigiumDefaultWindow` binds `Engine="{Binding Status.Engine}"`. View → Bottom / Top changes `Position`.

## Demo walkthrough

Set startup project to **Vestigium.Controls.StatusBar.Demo**.

| Tab | What to prove |
|---|---|
| Gallery | Move items Left / Center / Right. Dock the window bar Top / Bottom. Compare mini frames. Idle, ping, glyphs. |
| Nested | Child Post does not paint the chrome bar. |
| Lab | Post vs Immediate vs Flood on the window engine. |
| Settings | Idle string and timeout. Watch the chrome bar rewrite. |
| Snapshot | Flood, duplicate, missing key. Read counters, then JSON. |

Selected tabs and pressed buttons use accent fill with dark text so labels stay readable on the dark shell.

## Do not

- `Task.Run` the drain, or mutate columns from a worker.
- Call `ThemeManager` from this assembly.
- Bind `Delay` on `Value`-style workarounds — there is no `Value` DP for the message; use `Post`.
- Dock Left / Right on this control. That is not v1.
- Treat `PostImmediate` as a license to skip the engine from a background thread without marshaling — the engine already marshals.

## Tests

`Vestigium.Controls.Tests` covers position, Ready coercion, progress clamp, duplicate drop, missing key, dispose, idle, flood coalescing, slot defaults, and high-water isolation. See SRS §14.
