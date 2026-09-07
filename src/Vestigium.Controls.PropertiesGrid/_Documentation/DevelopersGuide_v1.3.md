# Vestigium.Controls.PropertiesGrid — Developers Guide

**Document ID:** VEST-CTL-PG-DEV-001  
**Version:** 1.3  
**Status:** Superseded by v1.4  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.3.md`](Requirements_v1.3.md)

v1.2 host / multi-select / collections / nested / reset still apply. This page covers alignment and category images.

## Align editors

Default is **Left**. That is what you want for TTL and Timeout in a settings inspector.

```xml
<pg:VestigiumPropertiesGrid SelectedObject="{Binding Selected, Mode=OneWay}"
                            EditorTextAlignment="Left"/>
```

```xml
EditorTextAlignment="{Binding EditorTextAlignment}"
```

`EditorTextAlignment` is OneWay from the host. Left / Center / Right.

One field that should stay right-aligned (a money column, a delta):

```csharp
[Category("Timing"), VestigiumTextAlignment(TextAlignment.Right)]
public decimal Timeout { get; set; }
```

`SelectedObject` / `SelectedObjects` / `ItemsSource` stay OneWay. The grid does not write the inspected object back.

## Category images

Nothing appears until you assign `CategoryIcons`.

Optional built-in pack:

```csharp
CategoryIcons = VestigiumCategoryGlyphs.CreateStandard();
```

PNG:

```csharp
CategoryIcons.Add(new VestigiumCategoryIcon
{
    Category = "Timing",
    ImageUri = new Uri("pack://application:,,,/Assets/clock.png")
});
```

SVG file or inline path:

```csharp
CategoryIcons.Add(new VestigiumCategoryIcon
{
    Category = "Network",
    IconData = "M1.4,6.4 H4.6 V9.6 H1.4 Z M11.4,2.2 H14.6 V5.4 H11.4 Z"
});

CategoryIcons.Add(new VestigiumCategoryIcon
{
    Category = "Display",
    Svg = File.ReadAllText("monitor.svg")
});
```

Replace a standard glyph by category name. First match wins.

WPF does not host SVG as `ImageSource`. Raster goes through `Image` / `ImageUri`. Vector goes through `IconData`, `IconGeometry`, or the first path in `Svg`. A `.svg` URI is read and parsed the same way. Complex multi-shape SVG: convert to `DrawingImage` and set `Image`.

## Do not

- Expect numbers to stay right unless you set `EditorTextAlignment="Right"` or the alignment attribute.
- Reference `Vestigium.Themes*` for these glyphs. They are geometry, not a theme.
- Put a 256 px marketing PNG in a 14×14 slot. 16×16 or 32×32 source, stretched uniformly.
