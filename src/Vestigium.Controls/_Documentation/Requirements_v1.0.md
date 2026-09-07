# Vestigium.Controls (base) — Software Requirements Specification

**Document ID:** VEST-CTL-BASE-SRS-001  
**Version:** 1.0  
**Status:** Skeleton / planning  
**Date:** 6 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM

## 1. Objective

`Vestigium.Controls` is the shared assembly for the control umbrella. It holds:

- Types every feature control is allowed to take a dependency on.
- DI registration (`AddVestigiumControls`).
- The **default Vestigium form** — an empty `Window` with a top-level horizontal menu (currently Under Construction) and a hosted `VestigiumStatusBar`.

Feature visuals (StatusBar internals, NumericUpDown, PropertiesGrid, UnderConstruction glyph) live in sibling assemblies. The base assembly may reference those siblings when the default form needs to host them.

## 2. Default form

### 2.1 Type

`Vestigium.Controls.Shell.VestigiumDefaultWindow` derives from `Window`.

Hosts either:

- Use it directly as the main window, or
- Derive from it and replace the content presenter.

### 2.2 Layout (top to bottom when status bar is Bottom)

1. Horizontal `Menu` spanning the window width.
2. Content presenter (`PART_ContentHost`) — empty in the skeleton.
3. `VestigiumStatusBar` docked Bottom.

When the bound status-bar position is Top:

1. Horizontal `Menu`.
2. `VestigiumStatusBar`.
3. Content presenter.

### 2.3 Menu (v1 skeleton)

Menus exist so chrome is visible. Every item that does not yet have a command shows the Under Construction surface or is disabled with header `"Under Construction"`.

Minimum items:

| Menu | Items |
|---|---|
| File | Under Construction, Exit (`ApplicationCommands.Close` / `Application.Shutdown`) |
| View | Status bar position: Bottom, Top (radio / check, bound to the status ViewModel) |
| Help | Under Construction |

File → Exit is the only real command in the skeleton besides the View position toggle.

### 2.4 ViewModel

`VestigiumDefaultWindowViewModel`:

- `Status` → `VestigiumStatusBarViewModel`
- `Content` → `object?` (null in the skeleton)
- `SetStatusBarPositionCommand` or two-way bound `Status.Position`

No networking, no logging, no IQ-specific state.

## 3. Shared contracts (future)

Reserved namespaces, empty in the skeleton except DI:

| Namespace | Future content |
|---|---|
| `Vestigium.Controls.DependencyInjection` | `AddVestigiumControls` |
| `Vestigium.Controls.Shell` | Default window + VM |
| `Vestigium.Controls.Primitives` | Shared DP helpers, coerce callbacks |

## 4. Demo

`Vestigium.Controls.Demo` starts `VestigiumDefaultWindow`. It is the Visual Studio startup project for this repository.

## 5. Non-goals

- Ribbon / Fluent navigation in v1
- Window chrome / caption-button restyle (Themes will own that)
- Documenting NumericUpDown or PropertiesGrid behavior here

## 6. Document control

| Version | Change | Source |
|---|---|---|
| 1.0 | Default form + shared assembly scope | Grok, 6 Sep 2026 |
