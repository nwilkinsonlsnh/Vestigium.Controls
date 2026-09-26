# Vestigium.Controls — Umbrella Requirements

**Document ID:** VEST-CTL-SRS-000  
**Version:** 1.2  
**Status:** Active  
**Date:** 25 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM

## 1. Objective

Provide a single Visual Studio solution that hosts every reusable WPF control the Vestigium suite needs, plus the **default Vestigium form** that every IQ application (PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost) can start from.

This document is the umbrella. Each control library owns a separate requirements file:

| Library | Document |
|---|---|
| `Vestigium.Controls` (base + default form) | `src/Vestigium.Controls/_Documentation/Requirements_v1.0.md` |
| `Vestigium.Controls.StatusBar` | `src/Vestigium.Controls.StatusBar/_Documentation/Requirements_v1.0.md` |
| `Vestigium.Controls.NumericUpDown` | `src/Vestigium.Controls.NumericUpDown/_Documentation/Requirements_v1.0.md` |
| `Vestigium.Controls.PropertiesGrid` | `src/Vestigium.Controls.PropertiesGrid/_Documentation/Requirements_v1.0.md` |
| `Vestigium.Controls.UnderConstruction` | `src/Vestigium.Controls.UnderConstruction/_Documentation/Requirements_v1.0.md` |

## 2. Architectural constraints (binding)

### 2.1 One solution, many libraries

Shared types, the default window, and DI registration live in `Vestigium.Controls`. Feature controls live in sibling assemblies so a host can take StatusBar without taking PropertiesGrid.

### 2.2 MVVM is required

Every project under this umbrella uses CommunityToolkit.Mvvm. ViewModels own state. Views bind. Code-behind does not contain business logic.

### 2.3 Dependency injection

Registration follows the suite pattern already used by `Vestigium.Converters`:

```csharp
services.AddVestigiumControls();
```

Feature libraries add their own extension (`AddVestigiumStatusBar`, and so on). The base extension may call feature extensions when those projects are referenced.

### 2.4 Theming is a host concern

No project under `Vestigium.Controls.slnx` SHALL reference `Vestigium.Themes`, `Vestigium.Themes.Controls`, or a palette assembly.

Controls paint with `{DynamicResource Vestigium.Brushes.*}` and ship fallback hex in their own `Themes/Generic.xaml` (WPF default-style dictionary). A consuming application that wants Light Blue, Dracula, or any other palette registers those packages and calls `ThemeManager.Initialize` in **its** `OnStartup`, per the Vestigium.Themes Developers Guide. `SwitchTheme` on that host repaints every Vestigium control already in the tree.

`Vestigium.Themes.Controls` styles stock WPF types (`StatusBar.Standard`, `Button.Primary`, …). That catalog is not how custom Vestigium controls get a theme. Custom controls consume tokens; they do not take catalog style keys as a required `Style=`.

### 2.5 Target framework

`net10.0-windows` + `UseWPF=true`. Visual Studio 2026. C# latest / C# 14 as provided by the SDK.

### 2.6 Documentation and packaging layout

Every library folder contains `_Documentation` with at least:

- `Requirements_vN.md`
- `DevelopersGuide_vN.md`

There is no sibling `*.Demo` WPF executable. Visual verification lives in consuming hosts, not in this repository.

Root `LICENSE` (MIT) and `README.md` are the only license and package-readme files. `Directory.Build.props` links both into every project and sets `PackageLicenseExpression` to MIT. Do not duplicate those files under `src/`.

### 2.7 License

MIT. Copyright line matches `LICENSE`: Vestigium / Wilkinson Business, 2026.

## 3. Solution shape

```
Vestigium.Controls.slnx
Directory.Build.props                    MIT + README stamp for every project
LICENSE                                  MIT
README.md
_Documentation/                          umbrella docs
src/Vestigium.Controls/                  base + default form
src/Vestigium.Controls.StatusBar/
src/Vestigium.Controls.NumericUpDown/
src/Vestigium.Controls.PropertiesGrid/
src/Vestigium.Controls.UnderConstruction/
src/Vestigium.Controls.Tests/
```

## 4. Delivery phases

| Phase | Scope | Status |
|---|---|---|
| 0 | Solution skeleton, default form chrome, stub controls, docs | Shipped |
| 1 | Accept and implement StatusBar per its SRS | Shipped |
| 2 | UnderConstruction visual language used by unfinished menus | Shipped — SRS v1.4 |
| 3 | NumericUpDown requirements + build | Shipped — SRS v1.2 |
| 4 | PropertiesGrid requirements + build | Shipped |
| 5 | Remove in-repo demo hosts; libraries + tests only | This revision |

## 5. Non-goals (umbrella)

- WinUI / MAUI / Avalonia ports
- Designer-only packages in v1
- Shipping NuGet to nuget.org as a required step of this revision
- Coupling any control to PingIQ / DnsIQ / TraceIQ / HttpIQ assemblies
- Shipping or initializing Vestigium.Themes from this solution
- In-repo `*.Demo` WPF hosts

## 6. Document control

| Version | Change | Source |
|---|---|---|
| 1.0 | Initial umbrella SRS and solution skeleton | Grok, 6 Sep 2026 |
| 1.1 | Theming assigned by consuming hosts, not by this solution | Stakeholder, 6 Sep 2026 |
| 1.2 | Demo hosts removed. MIT LICENSE + README stamped via Directory.Build.props | Stakeholder, 25 Sep 2026 |
