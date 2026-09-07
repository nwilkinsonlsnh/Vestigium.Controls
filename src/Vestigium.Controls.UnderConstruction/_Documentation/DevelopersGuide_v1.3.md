# Vestigium.Controls.UnderConstruction — Developers Guide

**Document ID:** VEST-CTL-UC-DEV-001  
**Version:** 1.3  
**Status:** Current  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.3.md`](Requirements_v1.3.md)

## Project

| Item | Value |
|---|---|
| Assembly | `Vestigium.Controls.UnderConstruction` |
| Type | `VestigiumUnderConstruction` |
| Rules | `UnderConstructionRules` |
| Demo | `src/Vestigium.Controls.UnderConstruction.Demo` |

`Header` and `Message` from v1.1 are gone. Use `Title`, `Subject`, and `Description`.

v1.3 is the Lab enforcement pass. Limits were already on the control DPs in v1.2. Hosts that bound a TextBox to a ViewModel still saw overflow because WPF coerce does not write back to a OneWay source. The demo now clamps the ViewModel and puts `MaxLength` on the boxes.

## Host a page

```xml
<uc:VestigiumUnderConstruction
    Title="Vestigium"
    Subject="Default form client area"
    Description="Feature views land here after their requirements are accepted."/>
```

The content column grows with the window up to 720 dip. Do not wrap the control in a 420-wide panel unless you want a narrow column.

## Host an overlay

```xml
<Grid>
  <local:LiveTile/>
  <uc:VestigiumUnderConstruction
      Title="Reports"
      Subject="Module in development"
      Description="Scheduled for Q3."
      IsActive="{Binding ReportsUnreleased}"
      Command="{Binding NotifyMeCommand}"/>
</Grid>
```

## Image

| What you have | Property |
|---|---|
| Built-in cone + barrier | Leave `ImageSource` and `IconData` null |
| PNG / JPEG | `ImageSource` or `ImageUri` (file, pack, or http) |
| Path geometry | `IconData` (`VestigiumUnderConstructionGlyphs.CreateCone()` / `CreateBarrier()`) |
| SVG file | Prefer rasterize to PNG for WPF. `ImageUri` to an `.svg` is accepted; BitmapImage may not decode it. |

Image wins over geometry. Geometry wins over built-in art.

## Limits

Defaults: Title **75**, Subject **125**, Description **unlimited**.

```xml
TitleMaxLength="40"
SubjectMaxLength="80"
DescriptionMaxLength="400"
```

`0` turns the cap off. Negative values snap back to the default (75 / 125 / 0).

The control truncates at coerce:

```csharp
UnderConstructionRules.Limit(value, maxLength);
```

Paste is not special-cased. A 400-character paste into Title becomes 75 characters on the DP.

### Host a TextBox against these DPs

WPF will not push the coerced value back to a OneWay ViewModel. Do both:

```xml
<TextBox Text="{Binding Title, UpdateSourceTrigger=PropertyChanged}"
         MaxLength="{Binding TitleMaxLength}"/>
```

```csharp
partial void OnTitleChanged(string value)
{
    var limited = UnderConstructionRules.Limit(value, TitleMaxLength);
    if (limited != value)
        Title = limited;
}
```

`TextBox.MaxLength = 0` is unlimited, same convention as the control.

`Title`, `Subject`, and `Description` are `BindsTwoWayByDefault` so a TwoWay bind to the control is enough for the visual. The ViewModel clamp is what keeps the **source** honest.

Lowering a max re-coerces the matching string. The demo also re-limits the ViewModel in `OnTitleMaxLengthChanged` so the TextBox shrinks with the cap.

## Demo Lab

Counters show `n / max` (or `n / ∞`). At the cap the count is the signal that extra keystrokes / paste were dropped.

| Field | Default max | Lab box |
|---|---|---|
| Title | 75 | Stops at 75 |
| Subject | 125 | Stops at 125 |
| Description | 0 | Unlimited until you type a max |

## Do not

- Call `ThemeManager` from this assembly.
- Put the words “UNDER CONSTRUCTION” in the bitmap — Title already says it.
- Ship a checkerboard background in a PNG. Export transparency on a real alpha channel.
- Rely on the control coerce alone when the user types into a ViewModel-bound TextBox.
