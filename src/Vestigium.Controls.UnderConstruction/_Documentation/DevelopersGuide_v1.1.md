# Vestigium.Controls.UnderConstruction — Developers Guide

**Document ID:** VEST-CTL-UC-DEV-001  
**Version:** 1.1  
**Status:** Current — matches the shipped library and demo  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.1.md`](Requirements_v1.1.md)

## Project

| Item | Value |
|---|---|
| Assembly | `Vestigium.Controls.UnderConstruction` |
| Target | `net10.0-windows` |
| Type | `VestigiumUnderConstruction` (lookless `Control`) |
| Demo | `src/Vestigium.Controls.UnderConstruction.Demo` |
| XAML xmlns | `clr-namespace:Vestigium.Controls.UnderConstruction;assembly=Vestigium.Controls.UnderConstruction` |

The v1.0 `UnderConstructionView` UserControl is gone. Use `VestigiumUnderConstruction`.

## Host it as a page

```xml
<uc:VestigiumUnderConstruction
    Header="Vestigium"
    Message="This module is not built yet."/>
```

`IsActive` defaults to `true`. That is the empty-client case.

## Host it as an overlay

```xml
<Grid>
  <local:LiveTile .../>
  <uc:VestigiumUnderConstruction
      IsActive="{Binding ReportsUnreleased}"
      Header="Reports"
      Message="Scheduled for Q3."
      Command="{Binding NotifyMeCommand}"
      ActionText="Notify me"/>
</Grid>
```

Declare the placeholder **last** (or raise `Panel.ZIndex`) so it sits on top. Do not keep an unreleased feature view loaded underneath in production — show only this control.

## Properties

| Property | Default | When to change |
|---|---|---|
| `Header` | Under Construction | Module name |
| `Message` | empty | Schedule / reason. Empty hides the line. |
| `IconData` | Built-in cone | Host `Geometry` if you have a product glyph |
| `IsActive` | true | Bind when used as an overlay |
| `Command` | null | Set to show the button |
| `ActionText` | Learn more | Caption for that button |

No `Theme` property. Host `ThemeManager` paints tokens. Fallback brushes keep the demo usable.

## Theming

This assembly does not reference Vestigium.Themes. High Contrast is template `SystemColors` triggers.

## Demo walkthrough

Set startup project to **Vestigium.Controls.UnderConstruction.Demo**.

| Tab | Prove |
|---|---|
| Page | Full client area, default glyph |
| Overlay | 2×2 grid; one cell blocked |
| Lab | Toggle `IsActive`, edit Header/Message, attach a command, swap icon |

Resize the window. The glyph must scale. Toggle `IsActive` on the overlay cell and click the tile underneath.

## Do not

- Call `ThemeManager` from this assembly.
- Ship Storyboard stripes or an indeterminate bar.
- Require an inverse-visibility converter on the real view.
- Show a button when `Command` is null.
