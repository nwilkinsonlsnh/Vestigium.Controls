# Vestigium.Controls.StatusBar — Developers Guide

**Document ID:** VEST-CTL-SB-DEV-001  
**Version:** 1.2  
**Status:** Skeleton — do not treat the current control as the finished implementation  
**Date:** 6 September 2026

## Read this first

The requirements document is the source of truth:

[`Requirements_v1.0.md`](Requirements_v1.0.md) (contract **v1.2**)

The types in this project exist so the solution compiles and the default form has a bar to host. They do **not** implement columns, `IUpdateStatusBar`, idle, icons, or the drain timer.

Do not start that work until the SRS is accepted and Build Mode is declared.

## Project

| Item | Value |
|---|---|
| Assembly | `Vestigium.Controls.StatusBar` |
| Target | `net10.0-windows` |
| XAML xmlns | `http://schemas.vestigium.dev/controls/statusbar` |
| Demo | `src/Vestigium.Controls.StatusBar.Demo` |

## What the skeleton does today

`VestigiumStatusBar` is still a `UserControl`. A property-changed handler writes `DockPanel.SetDock` when `Position` changes. That is enough to prove Top / Bottom on the default form.

The skeleton `Message` property is temporary. v1.2 removes it in favor of a `message` column.

## Build Mode checklist (after acceptance)

1. Promote to a lookless `Control` with `Generic.xaml` templates per `StatusBarColumnKind`.
2. `StatusBarColumn` + `ObservableCollection` + selector.
3. `IUpdateStatusBar` last-write-wins pipeline, 100–250 ms drain, epsilon, `PostImmediate`.
4. Idle opt-in, clock column, built-in `DrawingImage` icons.
5. Control docks itself; no Themes project reference; `{DynamicResource}` with Generic.xaml fallbacks.
6. Stop timers on `Dispose` and `Unloaded`.
7. Tests SB-T01 … SB-T20.
8. StatusBar demo covers SRS §13.
