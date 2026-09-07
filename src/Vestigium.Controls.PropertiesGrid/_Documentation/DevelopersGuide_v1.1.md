# Vestigium.Controls.PropertiesGrid — Developers Guide

**Document ID:** VEST-CTL-PG-DEV-001  
**Version:** 1.1  
**Status:** Planning  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.1.md`](Requirements_v1.1.md)

## Project

| Item | Value |
|---|---|
| Assembly | `Vestigium.Controls.PropertiesGrid` |
| Type | `VestigiumPropertiesGrid` (lookless `Control`; the v1.0 `UserControl` stub is replaced at build) |
| Engine | `PropertyEngine` |
| Demo | `src/Vestigium.Controls.PropertiesGrid.Demo` |

## Host

```xml
xmlns:pg="clr-namespace:Vestigium.Controls.PropertiesGrid;assembly=Vestigium.Controls.PropertiesGrid"

<pg:VestigiumPropertiesGrid SelectedObject="{Binding SelectedProbe}"/>
```

Annotate the model with `System.ComponentModel` attributes. That is the WinForms muscle memory we keep.

```csharp
public sealed class ProbeSettings : ObservableObject
{
    [Category("General"), DisplayName("Display name"), Description("Label shown on the tile.")]
    public string DisplayName { get => field; set => SetProperty(ref field, value); }

    [Category("Timing"), DisplayName("TTL"), Description("Hop limit 1–255.")]
    public int Ttl { get => field; set => SetProperty(ref field, value); }

    [Browsable(false)]
    public Guid Id { get; init; }
}
```

## Do not

- Reference `System.Windows.Forms` or host a WinForms `PropertyGrid`.
- Reference `Vestigium.Themes*`.
- Expect `UITypeEditor` modal dialogs. Supply a `VestigiumPropertyItem` or wait for host editor templates in a later version.
- Rebuild the row list yourself on every `PropertyChanged`. The engine coalesces value updates.

## Dynamic rows

When there is no CLR shape, bind `ItemsSource` to `IList<VestigiumPropertyItem>` and leave `SelectedObject` null.

## Editors

Numeric properties use `VestigiumNumericUpDown`. That control owns hold / snap / sign. Do not wrap it in another debounce.
