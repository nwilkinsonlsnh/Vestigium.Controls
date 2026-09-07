# Vestigium.Controls.StatusBar — Developers Guide

**Document ID:** VEST-CTL-SB-DEV-001  
**Version:** 1.0  
**Status:** Skeleton — do not treat the current control as the finished implementation  
**Date:** 6 September 2026

## Read this first

The requirements document is the source of truth:

[`Requirements_v1.0.md`](Requirements_v1.0.md)

The types in this project exist so the solution compiles and the default form has a bar to host. They implement only:

- `VestigiumStatusBarPosition` (`Bottom`, `Top`)
- `VestigiumStatusBar` with `Position` and `Message`
- `VestigiumStatusBarViewModel` with the same two properties plus placeholders

Do not add progress, clock, trailing text, or live-region work until the SRS is accepted and Build Mode is declared.

## Project

| Item | Value |
|---|---|
| Assembly | `Vestigium.Controls.StatusBar` |
| Target | `net10.0-windows` |
| XAML xmlns | `http://schemas.vestigium.dev/controls/statusbar` |
| Demo | `src/Vestigium.Controls.StatusBar.Demo` |

## How the skeleton docks

`VestigiumStatusBar` is a `UserControl` in the skeleton (may become a lookless `Control` in Build Mode). A property-changed handler writes `DockPanel.SetDock` when `Position` changes. That is enough to prove Top / Bottom on the default form.

## Build Mode checklist (later)

1. Promote to a lookless `Control` with `Themes/Generic.xaml` template parts.
2. Implement the five regions from the SRS.
3. Coerce progress. Collapse separators with regions.
4. Clock with a one-second dispatcher timer that does not announce to screen readers.
5. Tests SB-T01 … SB-T07.
6. Optional Themes resource keys; keep fallback brushes.
