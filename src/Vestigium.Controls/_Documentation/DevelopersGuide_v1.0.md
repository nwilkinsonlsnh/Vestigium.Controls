# Vestigium.Controls (base) — Developers Guide

**Document ID:** VEST-CTL-BASE-DEV-001  
**Version:** 1.0  
**Date:** 6 September 2026

## Default window

`Shell/VestigiumDefaultWindow.xaml` is a normal `Window`. The umbrella demo constructs it in `App.OnStartup` after `AddVestigiumControls()` so the library window stays free of demo-only resources.

## DI

```csharp
services.AddVestigiumControls();
```

Currently registers `VestigiumDefaultWindowViewModel` as transient. Feature `Add*` methods will be chained here once those libraries are referenced from the base project.

## Do not

- Put StatusBar template parts in this assembly.
- Reference Vestigium.Logging from the base project in the skeleton.
