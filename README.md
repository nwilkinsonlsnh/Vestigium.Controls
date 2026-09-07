# Vestigium.Controls

[![build](https://github.com/nwilkinsonlsnh/Vestigium.Controls/actions/workflows/build.yml/badge.svg)](https://github.com/nwilkinsonlsnh/Vestigium.Controls/actions/workflows/build.yml)

WPF control library for the Vestigium suite (PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost).

**Target:** .NET 10 LTS / WPF / Visual Studio 2026  
**Architecture:** MVVM + `Microsoft.Extensions.DependencyInjection`  
**Startup project:** `Vestigium.Controls.Demo` (default Vestigium shell form)

This repository is in **skeleton / requirements** phase. Controls compile as stubs. StatusBar is the first control with a written requirements document. Implementation of StatusBar waits for that document to be accepted.

Umbrella requirements: [`_Documentation/Requirements_v1.0.md`](_Documentation/Requirements_v1.0.md)  
Umbrella developer notes: [`_Documentation/DevelopersGuide_v1.0.md`](_Documentation/DevelopersGuide_v1.0.md)

## Libraries under this solution

| Project | Role | Docs |
|---|---|---|
| `Vestigium.Controls` | Shared primitives, DI host, **default Vestigium form** (menu + content + status bar) | [`src/Vestigium.Controls/_Documentation`](src/Vestigium.Controls/_Documentation) |
| `Vestigium.Controls.StatusBar` | Bindable status bar, dock **Top** or **Bottom** | [`src/Vestigium.Controls.StatusBar/_Documentation`](src/Vestigium.Controls.StatusBar/_Documentation) |
| `Vestigium.Controls.NumericUpDown` | Numeric spinner (stub) | [`src/Vestigium.Controls.NumericUpDown/_Documentation`](src/Vestigium.Controls.NumericUpDown/_Documentation) |
| `Vestigium.Controls.PropertiesGrid` | Property inspector (stub) | [`src/Vestigium.Controls.PropertiesGrid/_Documentation`](src/Vestigium.Controls.PropertiesGrid/_Documentation) |
| `Vestigium.Controls.UnderConstruction` | Placeholder surface used by unfinished chrome | [`src/Vestigium.Controls.UnderConstruction/_Documentation`](src/Vestigium.Controls.UnderConstruction/_Documentation) |

Each library has a matching `*.Demo` WPF host. `Vestigium.Controls.Demo` is the suite default form.

## Default form (this milestone)

`VestigiumDefaultWindow` is an empty host:

- Top-level horizontal `Menu` whose items currently resolve to **Under Construction**.
- Client area left empty for the consuming application.
- `VestigiumStatusBar` attached to the shell. Default dock is **Bottom**. Position is bindable to **Top** or **Bottom**.

## Open in Visual Studio

1. Clone this repository.
2. Open `Vestigium.Controls.slnx` in Visual Studio 2026.
3. Restore NuGet, set **Vestigium.Controls.Demo** as the startup project.
4. Run on Windows.

## Host in two calls

```csharp
using Microsoft.Extensions.DependencyInjection;
using Vestigium.Controls.DependencyInjection;

var services = new ServiceCollection();
services.AddVestigiumControls();
// later: services.AddVestigiumStatusBar();
var provider = services.BuildServiceProvider();
```

Do this during `Application.OnStartup` before the first window.

## Contracts that do not move

- MVVM on every project. Code-behind only calls `InitializeComponent` and assigns `DataContext`.
- Controls never throw on the dispatcher. Invalid input leaves the last good visual state.
- Design-time XAML preview works without a live `IServiceProvider`.
- Theme tokens come from `Vestigium.Themes` when the host has that package. Fallback brushes ship in each control's `Themes/Generic.xaml`.
- No project under this umbrella targets anything other than `net10.0-windows`.

## Suite neighbors

- [Vestigium.Themes](https://github.com/nwilkinsonlsnh/Vestigium.Themes) — palettes and stock control styles (including `StatusBar.Standard`).
- [Vestigium.Converters](https://github.com/nwilkinsonlsnh/Vestigium.Converters) — DI-resolved `IValueConverter` catalog.
- [Vestigium.Logging](https://github.com/nwilkinsonlsnh/Vestigium.Logging) — centralized Serilog module. StatusBar will subscribe later; not in v1 of the StatusBar requirements.
