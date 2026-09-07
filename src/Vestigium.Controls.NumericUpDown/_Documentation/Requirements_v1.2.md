# Vestigium.Controls.NumericUpDown — Software Requirements Specification

**Document ID:** VEST-CTL-NUD-SRS-001  
**Version:** 1.2  
**Status:** Current — matches the shipped library and `Vestigium.Controls.NumericUpDown.Demo`  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM  
**Supersedes:** v1.1

This document is the source of truth for `Vestigium.Controls.NumericUpDown`.

---

## 1. Product

A lookless WPF numeric spinner for Vestigium windows (PingIQ hop counts, TTL, ports, later money). Users change a `decimal` by typing, RepeatButtons, focused mouse wheel, and keys. The control owns throttling, snap, clamp, sign, and commit. Hosts bind `Value`. They do not write debounce timers.

### 1.1 What v1 is for

A single numeric field that stays responsive while the user holds a button across a large range, never throws on the dispatcher, and never requires ViewModel-side `DispatcherTimer` / `Binding.Delay` / `CancellationToken` to stay safe.

### 1.2 What v1 is not

Not a slider. Not a theme engine. Not a nullable / empty-to-null field. Not hex, engineering notation, or unit-suffix parsing. Not wrap-around. Not hover-wheel. Not `RangeBase` (that type is `double`).

---

## 2. Binding architecture

### 2.1 MVVM

- `NumericEngine` owns Display vs committed `Value`, snap, clamp, sign, parse, hold, and the Immediate drain flag.
- `VestigiumNumericUpDown` is a lookless `Control`. Template in `Themes/Generic.xaml`. Template parts: `PART_TextBox`, `PART_UpButton`, `PART_DownButton`.
- Demo ViewModels bind DPs. Control code-behind wires parts, RepeatButton delay, wheel, keys, and the dispatcher drain. It does not call logging, networking, or theme APIs.

### 2.2 No dispatcher throws

Invalid text, empty text, out-of-range bindings, `Increment <= 0`, `Min > Max`, unknown enums snap to a documented fallback. Coerce / parse / commit never throw on the UI thread.

### 2.3 Theming is assigned by the host

This assembly SHALL NOT project-reference `Vestigium.Themes*`, call `ThemeManager`, expose a `Theme` property, or ship a palette.

`Generic.xaml` fallbacks:

| Token | Fallback |
|---|---|
| Background | `#0F2744` |
| Text | `#E2E8F0` |
| Border | `#1E3A5F` |
| Accent / focus | `#4A90C8` |
| Muted | `#94A3B8` |

### 2.4 Two clocks

| Clock | Updates | Bound |
|---|---|---|
| **DisplayValue** | Every RepeatButton tick, wheel, key, digit | No |
| **Value** (DP) | Commit only (see §7) | Yes |

---

## 3. Types

```csharp
public enum VestigiumNumericUpdateMode { Immediate = 0, Deferred = 1 }
public enum VestigiumNumericCommitMode { Auto = 0, Explicit = 1 }
public enum VestigiumNumericInputMode { Full = 0, SpinOnly = 1, ReadOnly = 2 }
public enum VestigiumNumericSnapMode { Round = 0, Floor = 1, Ceiling = 2 }
public enum VestigiumNumericSignMode { Signed = 0, Unsigned = 1 }
```

All public numeric properties are `decimal`. `double` is not on the public surface.

---

## 4. Property matrix

| Property | Type | Default | Two-way | Notes |
|---|---|---|---|---|
| `Value` | `decimal` | `0` | Yes | Committed value. Only this writes the ViewModel. |
| `Minimum` | `decimal` | `decimal.MinValue` | OneWay | Unbounded until the host sets a domain. |
| `Maximum` | `decimal` | `decimal.MaxValue` | OneWay | |
| `Increment` | `decimal` | `1` | OneWay | `<= 0` coerces to `1`. |
| `PageIncrement` | `decimal` | `10` | OneWay | PageUp/PageDown and accelerated step. |
| `DecimalPlaces` | `int` | `0` | OneWay | Clamp `0–28`. Integer switch. Applied after snap. |
| `FormatString` | `string` | `"N0"` | OneWay | Display only. CurrentCulture. Bad format → general. |
| `UpdateMode` | enum | **Immediate** | OneWay | Spin/wheel/keys. Not typing. |
| `CommitMode` | enum | **Auto** | OneWay | Typed text accept. |
| `InputMode` | enum | **Full** | OneWay | Full / SpinOnly / ReadOnly. |
| `SignMode` | enum | **Signed** | OneWay | See §5.1. |
| `SnapToIncrement` | `bool` | **false** | OneWay | Off until the host turns it on. |
| `SnapMode` | enum | `Round` | OneWay | Round / Floor / Ceiling. Used only when snap is on. |
| `SnapBase` | `decimal` | `0` | OneWay | Grid origin. |
| `Delay` | `int` | `400` | OneWay | RepeatButton start delay (ms). |
| `Interval` | `int` | `33` | OneWay | RepeatButton visual tick (ms). Not the DP drain. |
| `AccelerationDelay` | `int` | `2000` | OneWay | Hold ms before step becomes `PageIncrement`. `0` = off. |

No public `DebounceInterval`. No `IsUpdating`. No `Theme`. No `AllowNegative` bool — use `SignMode`.

`Minimum` cannot exceed `Maximum`: last write wins, the other bound is coerced. No throw.

If a bound change, snap change, or `SignMode` change makes current `Value` illegal, `Value` coerces.

---

## 5. Input modes and sign

| Mode | Text | RepeatButtons | Wheel | Up/Down / Page | Copy |
|---|---|---|---|---|---|
| Full | Edit buffer | On | Focused | On | Yes |
| SpinOnly | Read-only display, no paste | On | Focused | On | Yes |
| ReadOnly | Display | Disabled | Off | Off | Yes |

Buttons that would not change the value (already at a bound) disable.

### 5.1 SignMode

| Mode | Lower bound | Typing `-` | Down at 0 |
|---|---|---|---|
| **Signed** (default) | Host `Minimum` (unbounded by default) | Allowed | Continues negative |
| **Unsigned** | `max(0, Minimum)` | Rejected in `PreviewTextInput` / paste | Button disables; value stays 0 |

Effective minimum is `SignMode == Unsigned && Minimum < 0 ? 0 : Minimum`. Host `Minimum` of `1` on an unsigned TTL field stays `1`, not `0`.

Switching a negative `Value` to Unsigned coerces it to the effective minimum. No throw.

Unsigned is not a separate integer type. The DP remains `decimal`. It only forbids values below zero.

---

## 6. UpdateMode and CommitMode

### 6.1 UpdateMode — spin, wheel, keys, acceleration

| Mode | Display while held | `Value` while held |
|---|---|---|
| Immediate (default) | Every visual tick | Last-write-wins on an internal AIMD drain **50–250 ms** |
| Deferred | Every visual tick | Frozen until the gesture **ends** |

Immediate is not “write the DP every 33 ms.” Display may tick at RepeatButton speed. `Value` coalesces.

### 6.2 CommitMode — typed text

Typing always uses an edit buffer. Intermediate strings (`-`, `1.`, empty) are not numbers. Unsigned never accepts `-` as a buffer.

| Mode | Enter | Tab / LostFocus |
|---|---|---|
| Auto (default) | Commit | Commit |
| Explicit | Commit | **Revert** |

Empty text always reverts to last committed `Value`.

**Spin-end always commits** in both CommitModes. Mouse-up after a hold is accept. Explicit does not require Enter after a spin. Escape always reverts Display to `Value` and drops pending Immediate flushes.

Immediate + Explicit: Explicit cannot rewind spin writes that already landed. Documented; allowed.

---

## 7. Encapsulated anti-lockup

Same contract as StatusBar: the control owns the drain. Hosts do not.

- One pending committed candidate (`decimal`). Last write wins. That is cancellation. No consumer-facing `CancellationToken`.
- No `Task.Run`. `Value` and `ValueChanged` are STA.
- RepeatButton `Delay` / `Interval` drive **DisplayValue** only.
- Immediate drain: `DispatcherTimer`, `Background`, 50–250 ms AIMD (+25 ms quiet, ×0.5 under load, floor 50). Re-entrancy skip if a flush is already running.
- Deferred: Display moves; `Value` writes on EndHold / Enter / (Auto) LostFocus.
- Auto-pause = skip overlapping dispatcher flushes. The control does not inspect Dispatcher queue depth.
- Unloaded / Dispose: stop RepeatButtons, stop drain timer, drop pending.

If a ViewModel starts a timer to “protect” this control, the pipeline failed.

---

## 8. Snap, parse, keys, wheel, acceleration

**Snap** is off by default (`SnapToIncrement = false`). Turning it on is a host / demo setting, not a hidden behavior.

Grid is `SnapBase + n × Increment`.

| SnapMode | Typed / bound `11` with Increment `5`, SnapBase `0` |
|---|---|
| Round (default) | `10` |
| Floor | `10` |
| Ceiling | `15` |

- Typed / bound / paste commits: apply `SnapMode` then `DecimalPlaces` then clamp (including Unsigned).
- Buttons / wheel / keys with snap on: next grid point **in the travel direction**. Up from `7` with Increment `5` → `10`, never down to `5`.
- Changing `SnapToIncrement` or `SnapMode` coerces the current `Value` onto the new grid when snap is on.
- Acceleration uses the same grid with `PageIncrement` as the step magnitude.

**Parse:** `NumberStyles.Number`, `CultureInfo.CurrentCulture`. Reject extra characters in `PreviewTextInput` / paste. Failed parse on commit → revert.

**Keys:** Up/Down = Increment; PageUp/PageDown = PageIncrement; Enter = commit text; Escape = revert; Tab = Auto commit or Explicit revert.

**Wheel:** only when the control has keyboard focus. No hover-wheel.

**Acceleration:** after `AccelerationDelay` ms of continuous hold, step becomes `PageIncrement`. `0` disables. Direction change or mouse-up resets. No extra multiplier DP; hosts that want 100× set `PageIncrement`.

---

## 9. Events and validation

Routed event `ValueChanged` (`RoutedPropertyChangedEventArgs<decimal>`) fires when the **DP** changes, not when Display ticks.

`INotifyDataErrorInfo` lives on the ViewModel. The control shows `Validation.HasError`. The control does not implement INDEI.

---

## 10. Public surface

```csharp
public class VestigiumNumericUpDown : Control
{
    public decimal Value { get; set; }
    public VestigiumNumericSignMode SignMode { get; set; }
    public bool SnapToIncrement { get; set; }
    public VestigiumNumericSnapMode SnapMode { get; set; }
    // remaining DPs from §4
    public event RoutedPropertyChangedEventHandler<decimal> ValueChanged;
}
```

```xml
xmlns:nud="clr-namespace:Vestigium.Controls.NumericUpDown;assembly=Vestigium.Controls.NumericUpDown"

<nud:VestigiumNumericUpDown Value="{Binding Ttl}"
                            SignMode="Unsigned"
                            Minimum="1"
                            Maximum="255"/>

<nud:VestigiumNumericUpDown Value="{Binding Offset}"
                            SignMode="Signed"
                            SnapToIncrement="True"
                            SnapMode="Round"
                            Increment="5"/>
```

---

## 11. Accessibility

Automation peer exposes name, value, min, max, small change. Announce **committed** `Value`, not every Display tick during a hold.

---

## 12. Demo

`Vestigium.Controls.NumericUpDown.Demo` SHALL show:

1. Default Immediate + Auto spinner with a live `Value` readout and a commit counter.
2. Deferred vs Immediate side by side during a hold.
3. Auto vs Explicit typing (click away vs Enter) on the Lab tab.
4. InputMode Full / SpinOnly / ReadOnly.
5. **Snap playground:** Snap on / Snap off, Round / Floor / Ceiling, Increment 5, live Value. Type `11` to prove each mode.
6. **Signed vs Unsigned** pair: Signed starts negative; Unsigned refuses to go below 0.
7. Bounded TTL 1–255.
8. `DecimalPlaces = 2` with FormatString `N2`.
9. Lab + Settings expose SignMode, SnapToIncrement, and SnapMode.
10. Nested form of several spinners.
11. Hold stress: window remains draggable; Immediate commit count stays far below visual ticks.

The demo does not initialize Vestigium.Themes.

---

## 13. Tests

| ID | Assertion |
|---|---|
| NUD-T01 | Default `Value` is `0`, range unbounded |
| NUD-T02 | Progress-style clamp: bind 999 with Max 50 → 50 |
| NUD-T03 | `Increment <= 0` → `1` |
| NUD-T04 | Empty commit reverts Display to `Value` |
| NUD-T05 | Deferred Step does not change `Value` until EndHold |
| NUD-T06 | Immediate Flush copies Display to `Value` |
| NUD-T07 | Snap Round 11, Increment 5, SnapBase 0 → 10 |
| NUD-T08 | Snap Up from 7 Increment 5 → 10 |
| NUD-T09 | Explicit LostFocus reverts typed draft |
| NUD-T10 | Auto LostFocus commits typed draft |
| NUD-T11 | DecimalPlaces 2 rounds 1.239 → 1.24 |
| NUD-T12 | ReadOnly Step is a no-op |
| NUD-T13 | Min > Max last write wins |
| NUD-T14 | csproj has no `Vestigium.Themes*` reference |
| NUD-T15 | Unsigned `SetCommitted(-8)` → `0`; Step down stays `0` |
| NUD-T16 | Signed `SetCommitted(-8)` → `-8` |
| NUD-T17 | Snap Floor 11 → 10; Ceiling 11 → 15 |

---

## 14. Acceptance

1. Demo §12 items 1–11 work on Generic fallbacks.
2. Hold increment across a large range; window still drags.
3. Immediate: bound readout follows; commit count << tick count.
4. Deferred: bound readout frozen until mouse-up.
5. Explicit: Tab after typing restores the old value.
6. Escape during hold restores last `Value`.
7. Gallery snap buttons change mode; type `11` matches the table in §8.
8. Unsigned spinner cannot display or bind a negative.
9. Listed tests pass.
10. No Themes project reference.

---

## 15. Later

- Nullable `decimal?`
- Wrap-around
- Hover-wheel
- Acceleration curve beyond PageIncrement
- `IconSource` / unit suffix
- Lookless DynamicResource keys once Themes is loaded by the host (fallbacks stay)
