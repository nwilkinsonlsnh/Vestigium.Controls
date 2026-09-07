# Vestigium.Controls.UnderConstruction — Software Requirements Specification

**Document ID:** VEST-CTL-UC-SRS-001  
**Version:** 1.2  
**Status:** Current — matches the shipped library and demo  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM  
**Supersedes:** v1.1

This document is the source of truth for `Vestigium.Controls.UnderConstruction`.

---

## 1. Product

A lookless WPF placeholder for unfinished Vestigium surfaces. One control covers **page** (the pane *is* the placeholder) and **overlay** (one cell is blocked).

v1.2 content stack, top to bottom:

1. **Image** — built-in cone + barrier, or a host PNG / SVG / geometry
2. **Title** — 75 characters default
3. **Subject line** — 125 characters default
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

---

## 3. Property matrix

| Property | Type | Default | Notes |
|---|---|---|---|
| `Title` | `string` | `"Under Construction"` | Truncated to `TitleMaxLength`. Empty hides the line. |
| `TitleMaxLength` | `int` | **75** | `0` = unlimited. Host may raise or lower. Negative coerces to 75. |
| `Subject` | `string` | `""` | Truncated to `SubjectMaxLength`. Empty hides the line. |
| `SubjectMaxLength` | `int` | **125** | `0` = unlimited. Negative coerces to 125. |
| `Description` | `string` | `""` | Paragraph. Empty hides the block. |
| `DescriptionMaxLength` | `int` | **0** (unlimited) | Host may cap it. |
| `ImageSource` | `ImageSource` | `null` | PNG / JPEG / BMP. Wins over geometry and built-in art. |
| `ImageUri` | `string` | `null` | File, pack, or http URI. Raster loads into `ImageSource`. SVG that BitmapImage cannot decode leaves built-in art unless `IconData` is set. |
| `IconData` | `Geometry` | `null` | Vector override. Used when `ImageSource` is null. |
| `IsActive` | `bool` | `true` | Visibility + hit-test. |
| `Command` | `ICommand` | `null` | Button hidden until set. |
| `CommandParameter` | `object` | `null` | |
| `ActionText` | `string` | `"Learn more"` | |

Artwork order: **ImageSource → IconData → built-in cone + barrier**.

No `Theme`. No `Header` / `Message` (replaced by Title / Description). No 420 px text cap.

---

## 4. Layout

The content column stretches with the parent up to **720 dip**. That is why a one-sentence description no longer parks the last two words on their own line when the window is wide.

Centered column, 32/24 padding:

1. Image (built-in 120×150, or host image max 160 tall)
2. Title (24 px)
3. Subject (15 px)
4. Description (13 px, wrap, line height 20)
5. Optional button

Empty Title, Subject, or Description collapse that row. They do not leave a gap of reserved height beyond margins on visible rows.

---

## 5. Limits

Limits apply at coerce time. They never throw.

| DP | Default cap | Override |
|---|---|---|
| Title | 75 | `TitleMaxLength` |
| Subject | 125 | `SubjectMaxLength` |
| Description | none | `DescriptionMaxLength` (0 = off) |

Changing a max length re-coerces the matching string.

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

1. Page — four-part stack, description stays on one line at normal window widths.
2. Overlay — 2×2 grid.
3. Lab — edit Title / Subject / Description, raise or lower max lengths, swap built-in / geometry / browse PNG or SVG, toggle `IsActive` and Command.

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

---

## 9. Acceptance

1. Page description does not force “are accepted” onto its own line on a wide window.
2. Custom PNG replaces the built-in mark.
3. Title longer than 75 is cut unless the host raises `TitleMaxLength`.
4. Listed tests pass.
5. No Themes project reference.

---

## 10. Later

- Native SVG document decode on WPF (today: PNG/`ImageSource`, or Geometry)
- Optional `IsAnimated` glyph breath
