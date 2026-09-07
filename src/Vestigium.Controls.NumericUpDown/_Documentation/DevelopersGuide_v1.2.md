# Vestigium.Controls.NumericUpDown — Developers Guide

**Document ID:** VEST-CTL-NUD-DEV-001  
**Version:** 1.2  
**Status:** Current — matches the shipped library and demo  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.2.md`](Requirements_v1.2.md)

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
                            SignMode="Unsigned"
                            Minimum="1"
                            Maximum="255"
                            Increment="1"
                            PageIncrement="10"/>
```

Defaults are Immediate + Auto + Full + **Signed** + unbounded + snap **off**. That is the consumer-grade spinner.

When the setter is expensive (starts a probe):

```xml
<nud:VestigiumNumericUpDown Value="{Binding HopCount}"
                            UpdateMode="Deferred"
                            CommitMode="Explicit"
                            SignMode="Unsigned"
                            Minimum="1"
                            Maximum="64"/>
```

Offset / delta that may go negative:

```xml
<nud:VestigiumNumericUpDown Value="{Binding Offset}"
                            SignMode="Signed"
                            SnapToIncrement="True"
                            SnapMode="Round"
                            Increment="5"/>
```

## Modes

| Property | Default | When to change |
|---|---|---|
| `UpdateMode` | Immediate | Deferred if the setter is not free |
| `CommitMode` | Auto | Explicit if typed text must press Enter |
| `InputMode` | Full | SpinOnly = picker; ReadOnly = frozen |
| `SignMode` | Signed | Unsigned if the field cannot go below 0 |
| `SnapToIncrement` | false | true to land on the Increment grid |
| `SnapMode` | Round | Floor or Ceiling when snap is on |
| `AccelerationDelay` | 2000 | `0` to disable; after this hold, step is `PageIncrement` |

`IsReadOnly` is not a separate DP. Use `InputMode="ReadOnly"`.  
`AllowNegative` is not a DP. Use `SignMode`.

## SignMode

`Signed` — host `Minimum` is the floor (unbounded by default). Minus is a legal first character.

`Unsigned` — effective floor is `max(0, Minimum)`. The minus key and a negative paste are rejected. Down at 0 disables. Switching a negative bound value to Unsigned coerces it to 0 (or to host Min if Min > 0).

TTL, hop count, port, timeout: Unsigned.  
Offset, delta, signed probe correction: Signed.

## Snap

Off until `SnapToIncrement="True"`.

Grid is `SnapBase + n × Increment`. Default SnapBase `0`. Anchor at min with `SnapBase="{Binding Minimum}"` when you need `1, 6, 11…`.

| SnapMode | Type `11`, Increment `5` |
|---|---|
| Round | 10 |
| Floor | 10 |
| Ceiling | 15 |

Buttons with snap on move to the next grid point in that direction (7 Up 5 → 10). Changing mode in the demo coerces the current value onto the new grid.

## Theming

No `Theme` property. No Vestigium.Themes reference. Host `ThemeManager.Initialize` assigns tokens. Fallback paints keep the demo usable.

## Demo walkthrough

Set startup project to **Vestigium.Controls.NumericUpDown.Demo**.

| Tab | Prove |
|---|---|
| Gallery | Default hold, Deferred vs Immediate, **Snap on/off + Round/Floor/Ceiling**, **Signed vs Unsigned**, TTL, N2, SpinOnly/ReadOnly |
| Lab | UpdateMode, CommitMode, InputMode, SignMode, Snap on/off, SnapMode — watch committed Value |
| Settings | Increment, PageIncrement, AccelerationDelay, DecimalPlaces, Snap checkbox + SnapMode, SignMode |
| Nested | Several spinners on one form |

How to prove snap: Gallery → Snap card → type `11` → Tab. Click Ceiling, value becomes 15. Click Snap off, type `11`, it stays 11.

How to prove sign: Gallery → Signed spinner, hold Down past 0. Unsigned spinner, hold Down — it stops at 0 and will not take `-`.

Hold the default spinner. The window must still drag. Immediate commit count must stay far below visual ticks.

## Do not

- `Task.Run` a value commit.
- Bind `Delay` on the `Value` binding to fake Deferred.
- Mutate `Value` from a worker thread.
- Call `ThemeManager` from this assembly.
- Inherit `RangeBase`.
- Treat Immediate as a license to write the DP at 33 ms.
- Use a bool `AllowNegative` — `SignMode` is the setting.

## Tests

`Vestigium.Controls.Tests` covers clamp, snap Round/Floor/Ceiling, Deferred EndHold, Immediate flush, empty revert, Explicit LostFocus, DecimalPlaces, ReadOnly, Signed negatives, Unsigned clamp at 0. See SRS §13.
