# Vestigium.Controls — Developers Guide

**Document ID:** VEST-CTL-DEV-000  
**Version:** 1.0  
**Status:** Skeleton  
**Date:** 6 September 2026

## Open the solution

1. Clone `https://github.com/nwilkinsonlsnh/Vestigium.Controls`.
2. Open `Vestigium.Controls.slnx` in Visual Studio 2026.
3. Set `Vestigium.Controls.Demo` as the startup project.
4. F5 on Windows.

## Where things live

| Need | Place |
|---|---|
| Default window | `src/Vestigium.Controls/Shell/VestigiumDefaultWindow.xaml` |
| DI registration | `src/Vestigium.Controls/DependencyInjection/ServiceCollectionExtensions.cs` |
| StatusBar control | `src/Vestigium.Controls.StatusBar/VestigiumStatusBar.xaml` |
| StatusBar requirements | `src/Vestigium.Controls.StatusBar/_Documentation/Requirements_v1.0.md` |
| Placeholder surface | `src/Vestigium.Controls.UnderConstruction/VestigiumUnderConstruction.cs` |

## Conventions copied from the suite

- `.slnx` (not legacy `.sln`)
- `Directory.Build.props` sets nullable, implicit usings, latest C#
- MIT license, same copyright line as Converters / Logging / Themes
- GitHub Actions `windows-latest` + `dotnet-version: 10.0.x`
- Demo windows use a dark surface (`#0B1A30`) until Themes is referenced

## Adding a new control later

1. New class library `src/Vestigium.Controls.{Name}` targeting `net10.0-windows` / WPF.
2. Sibling `src/Vestigium.Controls.{Name}.Demo`.
3. `_Documentation/Requirements_v1.0.md` and `DevelopersGuide_v1.0.md` **before** implementation.
4. Add both projects to `Vestigium.Controls.slnx`.
5. Register an `AddVestigium{Name}` extension. Wire it from `AddVestigiumControls` only if the base project references that library.

## Build mode

StatusBar implementation is intentionally thin. Treat the StatusBar SRS as the source of truth. Do not grow the control past Position + Message until that document is accepted.
