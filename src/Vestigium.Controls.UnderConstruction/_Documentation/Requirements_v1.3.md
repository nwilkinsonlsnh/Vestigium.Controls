# Vestigium.Controls.UnderConstruction — Software Requirements Specification

**Document ID:** VEST-CTL-UC-SRS-001  
**Version:** 1.3  
**Status:** Current — matches the shipped library and demo  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM  
**Supersedes:** v1.2

This document is the source of truth for `Vestigium.Controls.UnderConstruction`.

---

## 1. Product

A lookless WPF placeholder for unfinished Vestigium surfaces. One control covers **page** (the pane *is* the placeholder) and **overlay** (one cell is blocked).

Content stack, top to bottom:

1. **Image** — built-in cone + barrier, or a host PNG / SVG / geometry
2. **Title** — 75 characters default, **hard cap**
3. **Subject line** — 125 characters default, **hard cap**
4. **Description** — paragraph, unlimited unless the host sets a max

### 1.1 What v1 is for

A drop-in that fills its parent, eats input while `IsActive` is true, and tells the user what is unfinished.

### 1.2 What v1 is not

Not a theme engine. Not animated hazard stripes. Not an indeterminate progress bar. Not a second control type.

---

## 2. Architecture

Lookless `Control`. Visual tree in `Themes/Generic.xaml`. Template parts: `PART_Image`, `PART_DefaultArt`, `PART_Icon`, `PART_Title`, `PART_Subject`, `PART_Description`, `PART_Action`.

This assembly SHALL NOT project-reference `Vestigium.Themes*`.

Fallbacks: background `#0F2744` at 0.92, text `#E2E8F0`, subject `#CBD5E1`, description `#94A3B8`, cone `#E85D04`.

`Title`, `Subject`, and `Description` register with `BindsTwoWayByDefault`.

---

## 3. Property matrix

| Property | Type | Default | Notes |
|---|---|---|---|
| `Title` | `string` | `"Under Construction"` | Truncated to `TitleMaxLength` at coerce. Empty hides the line. Two-way by default. |
| `TitleMaxLength` | `int` | **75** | `0` = unlimited. Host may raise or lower. Negative coerces to 75. Changing it re-coerces `Title`. |
| `Subject` | `string` | `""` | Truncated to `SubjectMaxLength` at coerce. Empty hides the line. Two-way by default. |
| `SubjectMaxLength` | `int` | **125** | `0` = unlimited. Negative coerces to 125. Changing it re-coerces `Subject`. |
| `Description` | `string` | `""` | Paragraph. Empty hides the block. Two-way by default. |
| `DescriptionMaxLength` | `int` | **0** (unlimited) | Host may cap it. Negative coerces to 0. Changing it re-coerces `Description`. |
| `ImageSource` | `ImageSource` | `null` | PNG / JPEG / BMP. Wins over geometry and built-in art. |
| `ImageUri` | `string` | `null` | File, pack, or http URI. Raster loads into `ImageSource`. SVG that BitmapImage cannot decode leaves built-in art unless `IconData` is set. |
| `IconData` | `Geometry` | `null` | Vector override. Used when `ImageSource` is null. |
| `IsActive` | `bool` | `true` | Visibility + hit-test. |
| `Command` | `ICommand` | `null` | Button hidden until set. |
| `CommandParameter` | `object` | `null` | |
| `ActionText` | `string` | `"Learn more"` | |

Artwork order: **ImageSource → IconData → built-in cone + barrier**.

No `Theme`. No `Header` / `Message` (replaced by Title / Subject / Description). No 420 px text cap.

---

## 4. Character limits — enforcement (v1.3)

Limits are not advisory. They are a hard cut.

| Layer | What happens |
|---|---|
| Control DP coerce | `UnderConstructionRules.Limit` runs on every `SetValue`. Paste, binding, and code that push 200 characters into `Title` store **75**. Never throws. |
| Max-length change | Lowering `TitleMaxLength` re-coerces `Title` immediately. Raising it (or `0`) allows more on the next set. |
| WPF binding caveat | Coerce does **not** write the truncated value back to a OneWay source. A TextBox bound to a ViewModel string can still hold overflow unless the **host** clamps. |
| Demo / recommended host | ViewModel clamps on `OnTitleChanged` / `OnSubjectChanged` / `OnDescriptionChanged`. Lab TextBoxes set `MaxLength` to the matching max (`0` = unlimited in WPF). Live `n / max` counters. |

### 4.1 Defaults vs override

| DP | Default cap | Override | `0` means |
|---|---|---|---|
| Title | 75 | `TitleMaxLength` | Unlimited |
| Subject | 125 | `SubjectMaxLength` | Unlimited |
| Description | none | `DescriptionMaxLength` | Unlimited (the default) |

Negative max lengths snap to the default (75 / 125 / 0), not to unlimited.

### 4.2 Paste

A paste longer than the cap is dropped at the cap. No dialog. No exception. The stored string is `value[..maxLength]` after trim.

---

## 5. Layout

The content column stretches with the parent up to **720 dip**. That is why a one-sentence description no longer parks the last two words on their own line when the window is wide.

Centered column, 32/24 padding:

1. Image (built-in 120×150, or host image max 160 tall)
2. Title (24 px)
3. Subject (15 px)
4. Description (13 px, wrap, line height 20)
5. Optional button

Empty Title, Subject, or Description collapse that row. They do not leave a gap of reserved height beyond margins on visible rows.

---

## 6. Public surface

```xml
<uc:VestigiumUnderConstruction
    Title="Vestigium"
    Subject="Default form client area"
    Description="Feature views land here after their requirements are accepted."/>

<uc:VestigiumUnderConstruction
    Title="Reports"
    TitleMaxLength="40"
    Subject="Module in development"
    Description="Scheduled for Q3."
    ImageUri="pack://application:,,,/Assets/wip.png"
    IsActive="{Binding IsUnderConstruction}"
    Command="{Binding NotifyMeCommand}"/>
```

---

## 7. Demo

`Vestigium.Controls.UnderConstruction.Demo` SHALL prove the contract, not just render a picture.

1. **Page** — four-part stack. Default form copy: Title `Vestigium`, Subject `Default form client area`, Description `Feature views land here after their requirements are accepted.`
2. **Overlay** — 2×2 grid. Reports cell blocked while `IsActive`. Clicks on the live tile under the overlay do not fire.
3. **Lab**
   - Edit Title / Subject / Description with live `n / max` counters.
   - Title TextBox `MaxLength = TitleMaxLength` (default 75). Subject same at 125. Description `MaxLength = DescriptionMaxLength` (default 0 = unlimited).
   - ViewModel drops overflow on property change so the bound string never exceeds the cap even if a host skips `MaxLength`.
   - Title max / Subject max / Description max fields. Lowering a max cuts the current string.
   - Swap built-in / cone geometry / barrier geometry / browse PNG or SVG.
   - Toggle `IsActive` and Command.

---

## 8. Tests

| ID | Assertion |
|---|---|
| UC-T01 | Default Title is `"Under Construction"` |
| UC-T02 | Default Title max 75, Subject max 125 |
| UC-T03 | 90-character Title truncates to 75 |
| UC-T04 | `TitleMaxLength = 0` does not truncate |
| UC-T05 | Empty Description is hidden |
| UC-T06 | `Command == null` → no action |
| UC-T07 | `IsActive == false` → Collapsed |
| UC-T08 | csproj has no `Vestigium.Themes*` reference |
| UC-T09 | Paste of 12× `"Vestigium"` into Title is stored as 75 characters |
| UC-T10 | Host can raise Title max to 200 and keep 100 characters |

---

## 9. Acceptance

1. Page description does not force “are accepted” onto its own line on a wide window.
2. Custom PNG replaces the built-in mark.
3. Title longer than 75 is cut unless the host raises `TitleMaxLength`. Lab TextBox refuses extra keystrokes at 75.
4. Subject longer than 125 is cut the same way.
5. Description stays unlimited until `DescriptionMaxLength` is set.
6. Listed tests pass.
7. No Themes project reference.

---

## 10. Later

- Native SVG document decode on WPF (today: PNG/`ImageSource`, or Geometry)
- Optional `IsAnimated` glyph breath
