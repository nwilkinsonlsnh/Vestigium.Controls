# Vestigium.Controls.UnderConstruction — Developers Guide

**Document ID:** VEST-CTL-UC-DEV-001  
**Version:** 1.2  
**Status:** Current  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.2.md`](Requirements_v1.2.md)

## Project

| Item | Value |
|---|---|
| Assembly | `Vestigium.Controls.UnderConstruction` |
| Type | `VestigiumUnderConstruction` |
| Demo | `src/Vestigium.Controls.UnderConstruction.Demo` |

`Header` and `Message` from v1.1 are gone. Use `Title`, `Subject`, and `Description`.

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
| Path geometry | `IconData` |
| SVG file | Prefer rasterize to PNG for WPF. `ImageUri` to an `.svg` is accepted; BitmapImage may not decode it. |

Image wins over geometry. Geometry wins over built-in art.

## Limits

Defaults: Title 75, Subject 125, Description unlimited.

```xml
TitleMaxLength="40"
SubjectMaxLength="80"
DescriptionMaxLength="400"
```

`0` turns the cap off. Negative values snap back to the default.

## Do not

- Call `ThemeManager` from this assembly.
- Put the words “UNDER CONSTRUCTION” in the bitmap — Title already says it.
- Ship a checkerboard background in a PNG. Export transparency on a real alpha channel.
