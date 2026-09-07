# Vestigium.Controls.NumericUpDown — Developers Guide

**Document ID:** VEST-CTL-NUD-DEV-001  
**Version:** 1.1  
**Status:** Current — matches the shipped library and demo  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.1.md`](Requirements_v1.1.md)

## Project

| Item | Value |
|---|---|
| Assembly | `Vestigium.Controls.NumericUpDown` |
| Target | `net10.0-windows` |
| Demo | `src/Vestigium.Controls.NumericUpDown.Demo` |
| XAML xmlns | `clr-namespace:Vestigium.Controls.NumericUpDown;assembly=Vestigium.Controls.NumericUpDown` |

## Mental model

Two numbers. One of them is bound.

```
user hold / wheel / keys     →  DisplayValue (every tick)
                                Value DP  ← Immediate AIMD 50–250 ms
                                         ← Deferred: mouse-up / EndHold only

user types                   →  edit buffer
                                Auto: Enter or leaving the field commits
                                Explicit: Enter commits; leaving reverts
```

Spin-end always commits. Escape always restores last `Value`.

Hosts bind `Value`. They do not write a debounce timer.

## Host a spinner

```xml
xmlns:nud="clr-namespace:Vestigium.Controls.NumericUpDown;assembly=Vestigium.Controls.NumericUpDown"

<nud:VestigiumNumericUpDown Value="{Binding Ttl, Mode=TwoWay}"
                            Minimum="1"
                            Maximum="255"
                            Increment="1"
                            PageIncrement="10"/>
```

Defaults are Immediate + Auto + Full + unbounded + snap off. That is the consumer-grade spinner.

When the setter is expensive (starts a probe):

```xml
<nud:VestigiumNumericUpDown Value="{Binding HopCount}"
                            UpdateMode="Deferred"
                            CommitMode="Explicit"
                            Minimum="1"
                            Maximum="64"/>
```

## Modes

| Property | Default | When to change |
|---|---|---|
| `UpdateMode` | Immediate | Deferred if the setter is not free |
| `CommitMode` | Auto | Explicit if typed text must press Enter |
| `InputMode` | Full | SpinOnly = picker; ReadOnly = frozen |
| `SignMode` | Signed | Unsigned if the field cannot go below 0 |
| `SnapToIncrement` | false | true to land on Increment grid |
| `SnapMode` | Round | Floor / Ceiling when snap is on |
| `SnapToIncrement` | false | true to land on Increment grid |
| `AccelerationDelay` | 2000 | `0` to disable; after this hold, step is `PageIncrement` |

`IsReadOnly` is not a separate DP. Use `InputMode="ReadOnly"`.

## Snap

Grid is `SnapBase + n × Increment`. Default SnapBase `0`. Anchor at min with `SnapBase="{Binding Minimum}"` when you need `1, 6, 11…`.

Typed 11 + Increment 5 + Round → 10. Buttons with snap on move to the next grid point in that direction.

## Theming

No `Theme` property. No Vestigium.Themes reference. Host `ThemeManager.Initialize` assigns tokens. Fallback paints keep the demo usable.

## Demo walkthrough

Set startup project to **Vestigium.Controls.NumericUpDown.Demo**.

| Tab | Prove |
|---|---|
| Gallery | Default, Deferred vs Immediate hold, snap, TTL bounds, N2, modes |
| Lab | Toggle UpdateMode / CommitMode; watch Value vs Display and commit count |
| Settings | Increment, PageIncrement, AccelerationDelay, DecimalPlaces |
| Nested | Several spinners on one form |

Hold the default spinner. The window must still drag. Immediate commit count must stay far below visual ticks.

## Do not

- `Task.Run` a value commit.
- Bind `Delay` on the `Value` binding to fake Deferred.
- Mutate `Value` from a worker thread.
- Call `ThemeManager` from this assembly.
- Inherit `RangeBase`.
- Treat Immediate as a license to write the DP at 33 ms.

## Tests

`Vestigium.Controls.Tests` covers clamp, snap, Deferred EndHold, Immediate flush, empty revert, Explicit LostFocus, DecimalPlaces, ReadOnly. See SRS §13.
