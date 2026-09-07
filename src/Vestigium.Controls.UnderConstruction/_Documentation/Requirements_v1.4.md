# Vestigium.Controls.UnderConstruction — Software Requirements Specification

**Document ID:** VEST-CTL-UC-SRS-001  
**Version:** 1.4  
**Status:** Current — matches the shipped library and demo  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM  
**Supersedes:** v1.3

This document is the source of truth for `Vestigium.Controls.UnderConstruction`.

---

## 1. Product

A lookless WPF placeholder for unfinished Vestigium surfaces. One control covers **page** (the pane *is* the placeholder) and **overlay** (one cell is blocked).

Content stack, top to bottom:

1. **Image** — built-in cone + barrier, or a host PNG / SVG / geometry
2. **Title** — 75 characters default, **hard cap**, **single line**
3. **Subject line** — 125 characters default, **hard cap**, **single line**
4. **Description** — **multi-line memo**, unlimited unless the host sets a max. Newlines are data.

### 1.1 What v1 is for

A drop-in that fills its parent, eats input while `IsActive` is true, and tells the user what is unfinished.

### 1.2 What v1 is not

Not a theme engine. Not animated hazard stripes. Not an indeterminate progress bar. Not a second control type. Not a rich-text / Markdown editor.

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
| `Title` | `string` | `"Under Construction"` | Single line. Newlines flatten to spaces. Truncated to `TitleMaxLength`. Empty hides the line. Two-way by default. |
| `TitleMaxLength` | `int` | **75** | `0` = unlimited. Negative coerces to 75. Changing it re-coerces `Title`. |
| `Subject` | `string` | `""` | Single line. Newlines flatten to spaces. Truncated to `SubjectMaxLength`. Empty hides the line. Two-way by default. |
| `SubjectMaxLength` | `int` | **125** | `0` = unlimited. Negative coerces to 125. Changing it re-coerces `Subject`. |
| `Description` | `string` | `""` | **Multi-line.** `\n` is a paragraph break. Empty / whitespace-only hides the block. Two-way by default. |
| `DescriptionMaxLength` | `int` | **0** (unlimited) | Counts every character including newlines. Negative coerces to 0. Changing it re-coerces `Description`. |
| `ImageSource` | `ImageSource` | `null` | PNG / JPEG / BMP. Wins over geometry and built-in art. |
| `ImageUri` | `string` | `null` | File, pack, or http URI. Raster loads into `ImageSource`. SVG that BitmapImage cannot decode leaves built-in art unless `IconData` is set. |
| `IconData` | `Geometry` | `null` | Vector override. Used when `ImageSource` is null. |
| `IsActive` | `bool` | `true` | Visibility + hit-test. |
| `Command` | `ICommand` | `null` | Button hidden until set. |
| `CommandParameter` | `object` | `null` | |
| `ActionText` | `string` | `"Learn more"` | |

Artwork order: **ImageSource → IconData → built-in cone + barrier**.

No `Theme`. No `Header` / `Message`. No 420 px text cap.

---

## 4. Description is a memo, not a single line (v1.4)

| Rule | Requirement |
|---|---|
| Input | Host editors SHALL accept Enter as a new line (`AcceptsReturn`, wrap, vertical scroll). Tab moves focus. |
| Storage | Newlines are preserved. `\r\n` and `\r` normalize to `\n`. Trailing Enter is kept so a new last paragraph can be started. |
| Display | `PART_Description` is a wrapping `TextBlock`. Bound `\n` renders as a line break. |
| Trim | Description is **not** `Trim()`’d on each keystroke. Title and Subject still flatten to one line. |
| Visibility | Whitespace-only Description still hides the block. |
| Caps | `DescriptionMaxLength` (default 0) counts `\n`. Cut is a hard slice, never a throw. |
| Not in scope | Bold, lists, Markdown, spell-check inside the control itself. The **demo Lab** may enable WPF `SpellCheck` on its editor. |

Title / Subject pasted with a newline become a single line (`Hello\nworld` → `Hello world`).

---

## 5. Character limits — enforcement

Limits are not advisory. They are a hard cut.

| Layer | What happens |
|---|---|
| Control DP coerce | `UnderConstructionRules.Limit` on every `SetValue`. Description uses `multiline: true`. |
| Max-length change | Lowering a max re-coerces the matching string immediately. |
| WPF binding caveat | Coerce does **not** write the truncated value back to a OneWay source. |
| Demo / recommended host | ViewModel clamps. Lab Title/Subject TextBoxes use `MaxLength`. Description Lab is a memo editor (`AcceptsReturn`, wrap, scroll, ~180 dip tall). Live `n / max` counters. |

| DP | Default cap | Line mode | `0` means |
|---|---|---|---|
| Title | 75 | Single | Unlimited |
| Subject | 125 | Single | Unlimited |
| Description | none | Multi | Unlimited (the default) |

Negative max lengths snap to the default (75 / 125 / 0).

---

## 6. Layout

The content column stretches with the parent up to **720 dip**.

Centered column, 32/24 padding:

1. Image (built-in 120×150, or host image max 160 tall)
2. Title (24 px)
3. Subject (15 px)
4. Description (13 px, wrap, line height 20, **preserves paragraphs**)
5. Optional button

Empty Title, Subject, or Description collapse that row.

---

## 7. Public surface

```xml
<uc:VestigiumUnderConstruction
    Title="Vestigium"
    Subject="Default form client area"
    Description="Feature views land here after their requirements are accepted.&#10;&#10;Use this paragraph for schedule, owner, or a short note to the user."/>
```

```xml
<TextBox Text="{Binding Description, UpdateSourceTrigger=PropertyChanged}"
         AcceptsReturn="True"
         TextWrapping="Wrap"
         VerticalScrollBarVisibility="Auto"
         MinHeight="160"
         MaxLength="{Binding DescriptionMaxLength}"/>
```

---

## 8. Demo

`Vestigium.Controls.UnderConstruction.Demo` SHALL prove the contract.

1. **Page** — four-part stack.
2. **Overlay** — 2×2 grid. Reports cell blocked while `IsActive`.
3. **Lab**
   - Title / Subject single-line boxes with `MaxLength` and `n / max`.
   - **Description memo** — Enter inserts a newline, wrap, vertical scroll, ~220 dip, spell check. Counter includes newline characters.
   - Title max / Subject max / Description max fields.
   - Swap built-in / cone / barrier / browse PNG or SVG.
   - Toggle `IsActive` and Command.

Lab default Description is two paragraphs so the line break is visible without typing.

---

## 9. Tests

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
| UC-T11 | Description `"Line one.\n\nLine two."` round-trips with newlines intact |
| UC-T12 | Description `"Line one.\n"` keeps the trailing Enter |
| UC-T13 | Title `"Hello\nworld"` becomes `"Hello world"` |

---

## 10. Acceptance

1. Page description does not force “are accepted” onto its own line on a wide window.
2. Custom PNG replaces the built-in mark.
3. Title longer than 75 is cut unless the host raises `TitleMaxLength`. Lab TextBox refuses extra keystrokes at 75.
4. Subject longer than 125 is cut the same way.
5. Description stays unlimited until `DescriptionMaxLength` is set.
6. **Enter in the Description Lab starts a new line. The control shows that line break.**
7. Listed tests pass.
8. No Themes project reference.

---

## 11. Later

- Native SVG document decode on WPF (today: PNG/`ImageSource`, or Geometry)
- Optional `IsAnimated` glyph breath
