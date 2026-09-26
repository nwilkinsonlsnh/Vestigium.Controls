# Vestigium.Controls — Developers Guide

**Document ID:** VEST-CTL-DEV-000  
**Version:** 1.1  
**Status:** Active  
**Date:** 25 September 2026

## Open the solution

1. Clone `https://github.com/nwilkinsonlsnh/Vestigium.Controls`.
2. Open `Vestigium.Controls.slnx` in Visual Studio 2026.
3. Restore NuGet and build.
4. Run `dotnet test Vestigium.Controls.slnx -c Release`.

This solution has no startup executable. Consume the libraries from a host application.

## Where things live

| Need | Place |
|---|---|
| Default window | `src/Vestigium.Controls/Shell/VestigiumDefaultWindow.xaml` |
| DI registration | `src/Vestigium.Controls/DependencyInjection/ServiceCollectionExtensions.cs` |
| StatusBar control | `src/Vestigium.Controls.StatusBar/VestigiumStatusBar.xaml` |
| StatusBar requirements | `src/Vestigium.Controls.StatusBar/_Documentation/Requirements_v1.3.md` |
| Placeholder surface | `src/Vestigium.Controls.UnderConstruction/VestigiumUnderConstruction.cs` |
| License / package readme | Repo-root `LICENSE` and `README.md`, linked by `Directory.Build.props` |

## Conventions copied from the suite

- `.slnx` (not legacy `.sln`)
- `Directory.Build.props` sets nullable, implicit usings, latest C#, MIT `PackageLicenseExpression`, and links root LICENSE + README into every project
- MIT license, same copyright line as Converters / Logging / Themes
- GitHub Actions `windows-latest` + `dotnet-version: 10.0.x`

## Adding a new control later

1. New class library `src/Vestigium.Controls.{Name}` targeting `net10.0-windows` / WPF.
2. `_Documentation/Requirements_v1.0.md` and `DevelopersGuide_v1.0.md` **before** implementation.
3. Add the library (and tests if it needs its own fixtures) to `Vestigium.Controls.slnx`.
4. Register an `AddVestigium{Name}` extension. Wire it from `AddVestigiumControls` only if the base project references that library.
5. Do not add a sibling `*.Demo` project.

## Build mode

StatusBar implementation is intentionally thin. Treat the StatusBar SRS as the source of truth. Do not grow the control past Position + Message until that document is accepted.
