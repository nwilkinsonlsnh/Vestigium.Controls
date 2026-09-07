# Vestigium.Controls.UnderConstruction — Software Requirements Specification

**Document ID:** VEST-CTL-UC-SRS-001  
**Version:** 1.1  
**Status:** Current — matches the shipped library and demo  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM  
**Supersedes:** v1.0 (skeleton UserControl)

This document is the source of truth for `Vestigium.Controls.UnderConstruction`.

---

## 1. Product

A lookless WPF placeholder for unfinished Vestigium surfaces. One control covers two host uses:

| Use | Where | What the user sees |
|---|---|---|
| **Page** | Default form client area, unfinished menu destination | The pane *is* the placeholder |
| **Overlay** | One cell in a module grid that is not shipped | Content underneath is blocked |

v1 ships **one** type: `VestigiumUnderConstruction`. Not a View plus an Overlay.

### 1.1 What v1 is for

A drop-in that fills its parent, shows a construction glyph plus title, optionally a message and one command, and eats input while `IsActive` is true.

### 1.2 What v1 is not

Not a theme engine. Not animated hazard stripes. Not an indeterminate progress bar (nothing is progressing). Not notify-me / issue-tracker integration. Not a second control type. Not `ImageSource` / font-glyph icons.

---

## 2. Architecture

- Lookless `Control`. Visual tree in `Themes/Generic.xaml`. Template parts: `PART_Icon`, `PART_Header`, `PART_Message`, `PART_Action`.
- `OnApplyTemplate` may wire the action button. No probe logic, no `ThemeManager`, no logging.
- Demo / host ViewModels use `CommunityToolkit.Mvvm`. This assembly does not require that package.
- This assembly SHALL NOT project-reference `Vestigium.Themes*`, call `ThemeManager`, or expose a `Theme` property.

`Generic.xaml` fallbacks:

| Token | Fallback |
|---|---|
| Background | `#0F2744` at 0.92 opacity |
| Text | `#E2E8F0` |
| Muted | `#94A3B8` |
| Accent / glyph | `#E8A317` |
| Button | `#132744` / `#4A90C8` on hover |

High Contrast: `SystemColors` triggers in the template.

---

## 3. Property matrix

| Property | Type | Default | Two-way | Notes |
|---|---|---|---|---|
| `Header` | `string` | `"Under Construction"` | OneWay | Empty hides the title. |
| `Message` | `string` | `""` | OneWay | Whitespace coerces to empty; empty hides the subtitle. |
| `IconData` | `Geometry` | Built-in cone | OneWay | Host may replace. One way in. |
| `IsActive` | `bool` | **true** | OneWay | Visibility + hit-test. |
| `Command` | `ICommand` | `null` | OneWay | Button hidden until set. |
| `CommandParameter` | `object` | `null` | OneWay | |
| `ActionText` | `string` | `"Learn more"` | OneWay | Caption when the button is shown. |

No `Theme`. No `IsAnimated`. No `Progress`. No `StripeSpeed`. No `ImageSource`.

---

## 4. Behavior

### 4.1 IsActive

| Value | Visibility | Hits |
|---|---|---|
| `true` (default) | Visible, stretches to parent | Eats mouse and keys. Tab stop is the action button only, and only if `Command` is set. |
| `false` | `Collapsed` | Nothing. Underlying content is reachable. |

Hosts do not need an inverse-visibility converter on the real view. Overlay hosts bind `IsActive`. Page hosts leave the default.

Unreleased modules SHOULD show **only** this control. Overlay-on-mock is a demo pattern, not required at runtime.

### 4.2 Action

The button is visible if and only if `Command != null`. No orphan “Learn more.”

### 4.3 Layout

`HorizontalAlignment` / `VerticalAlignment` Stretch. Centered column:

1. Glyph in a `Viewbox` (~64 dip, scales down in a tight cell)
2. Header
3. Message (wrap, max width 420)
4. Optional button

Resize from ~160×120 to a full window without clipping the glyph or wrapping into unreadability.

### 4.4 Animation

**None in v1.** Static glyph. A later `IsAnimated` that breathes the glyph is out of scope.

### 4.5 Localization

Bindable strings only. This library does not ship `x:Static` dictionaries. Hosts bind `Header` / `Message` / `ActionText` from their own resources.

---

## 5. Public surface

```xml
xmlns:uc="clr-namespace:Vestigium.Controls.UnderConstruction;assembly=Vestigium.Controls.UnderConstruction"

<uc:VestigiumUnderConstruction/>

<uc:VestigiumUnderConstruction
    Header="Reporting"
    Message="Scheduled for Q3."
    IsActive="{Binding IsUnderConstruction}"
    Command="{Binding NotifyMeCommand}"
    ActionText="Notify me"/>
```

---

## 6. Demo

`Vestigium.Controls.UnderConstruction.Demo` SHALL show:

1. Full-window page (default-form case).
2. 2×2 module grid: three live cells, one placeholder overlay.
3. `IsActive` toggle — overlay vanishes, that cell is clickable.
4. Header / Message / Action Command.
5. Replace `IconData`.
6. Generic dark fallbacks, no Themes init.
7. Resize without clipping.

The default Vestigium form uses this control as its empty client area.

---

## 7. Tests

| ID | Assertion |
|---|---|
| UC-T01 | Default `Header` is `"Under Construction"` |
| UC-T02 | Default `IsActive` is `true` |
| UC-T03 | Empty / whitespace `Message` does not show a subtitle |
| UC-T04 | `Command == null` → no action |
| UC-T05 | `Command != null` → action shown |
| UC-T06 | `IsActive == false` → Collapsed |
| UC-T07 | csproj has no `Vestigium.Themes*` reference |

---

## 8. Acceptance

1. Demo §6 items 1–7 work on Generic fallbacks.
2. Default form client area shows the glyph, not an empty pane.
3. Overlay cell blocks clicks while active and does not when `IsActive` is false.
4. Listed tests pass.
5. No Themes project reference.

---

## 9. Later

- Optional `IsAnimated` glyph breath
- `ImageSource` icon
- Built-in resource dictionaries per language
