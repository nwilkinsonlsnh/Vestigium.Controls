# Vestigium.Controls.PropertiesGrid — Developers Guide

**Document ID:** VEST-CTL-PG-DEV-001  
**Version:** 1.5  
**Status:** Implemented  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.5.md`](Requirements_v1.5.md)

## Project

| Item | Value |
|---|---|
| Assembly | `Vestigium.Controls.PropertiesGrid` |
| Type | `VestigiumPropertiesGrid` (lookless `Control`) |
| Engine | `PropertyEngine` |
| Demo | `src/Vestigium.Controls.PropertiesGrid.Demo` |

## Bind the inspected object OneWay

The grid reads the instance. It does not write it back. Default binding mode on `SelectedObject` / `SelectedObjects` / `ItemsSource` / `CategoryIcons` is OneWay.

```xml
xmlns:pg="clr-namespace:Vestigium.Controls.PropertiesGrid;assembly=Vestigium.Controls.PropertiesGrid"

<pg:VestigiumPropertiesGrid SelectedObject="{Binding Selected, Mode=OneWay}"
                            Sort="Categorized"
                            EditorTextAlignment="Left"
                            CategoryTextAlignment="Left"
                            IsCategoryBold="True"
                            CategoryOrientation="Horizontal"
                            CategoryIcons="{Binding CategoryIcons, Mode=OneWay}"/>
```

Multi-select: bind the writable selected instance **and** the set. When the set is empty, the engine uses `SelectedObject`.

```xml
<pg:VestigiumPropertiesGrid SelectedObject="{Binding Selected, Mode=OneWay}"
                            SelectedObjects="{Binding GridTargets, Mode=OneWay}"/>
```

Do not bind a get-only computed property TwoWay:

```csharp
// This throws if the binding is TwoWay.
public object? GridTarget => Selected;
```

```
InvalidOperationException:
A TwoWay or OneWayToSource binding cannot work on the read-only property 'GridTarget'.
```

If you keep a computed property, give it a setter or mark the binding `Mode=OneWay`. Rebuild **library and demo** after pulling — a demo-only rebuild keeps the old DLL.

## Annotate the model

```csharp
[Category("Timing"), DisplayName("TTL"), Description("Hop limit 1–255."), DefaultValue(64)]
public int Ttl { get => field; set => SetProperty(ref field, value); }
```

`[Browsable(false)]` hides a property. No public setter → ReadOnly editor. Missing `[Category]` lands in `Misc`.

## Editor alignment

Default is **Left**. That is what you want for TTL and Timeout in a settings inspector.

```xml
EditorTextAlignment="{Binding EditorTextAlignment}"
```

One field that should stay right-aligned:

```csharp
[Category("Timing"), VestigiumTextAlignment(TextAlignment.Right)]
public decimal Timeout { get; set; }
```

`VestigiumNumericUpDown` standalone still defaults to Right. Inside this grid the row's `EffectiveTextAlignment` wins.

## Grouping headers

Separate DPs from editors. Bold and left by default.

```xml
CategoryTextAlignment="Center"   <!-- TIMING / GENERAL move; TTL does not -->
IsCategoryBold="False"           <!-- unbold -->
CategoryOrientation="Vertical"   <!-- stack chevron, icon, name -->
```

`CategoryOrientation` is `System.Windows.Controls.Orientation` (Horizontal / Vertical), not a text rotation.

Alphabetical sort has no headers, so grouping chrome is invisible until Categorized is on.

## Category images

Nothing appears until you assign `CategoryIcons`.

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

Vector path or inline SVG:

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

WPF does not host SVG as `ImageSource`. Raster goes through `Image` / `ImageUri`. Vector goes through `IconData`, `IconGeometry`, or the first path in `Svg`. A `.svg` URI is read and parsed the same way. Complex multi-shape SVG: convert to `DrawingImage` and set `Image`. Use 16×16 or 32×32 source in a 14×14 slot.

## Host many objects

```xml
<pg:VestigiumPropertiesGrid SelectedObjects="{Binding SelectedProbes, Mode=OneWay}"/>
```

Only properties that exist on **every** selected instance appear. Different values show as mixed. One commit writes all targets. Collection rows do not expand while more than one object is selected.

## Collections

Expose `ObservableCollection<T>` (or any `IList`). The row shows `Count`, Add / Remove / Up / Down. Children are `[0]`, `[1]`, …

`T` needs a public parameterless constructor for Add. Arrays are fixed-size: items edit, Add is off.

`IDictionary` is not an editor in v1 — flatten through `ItemsSource` if you need it.

## Nested objects

A class/struct property expands. Children load on first open. Cycles render `(circular)`. Cap is `MaxExpandDepth` (default 8). Null nested objects show `(none)` and do not auto-construct.

## Reset

`[DefaultValue]` enables Reset on that row and Reset all on the toolbar. No attribute means no reset — the grid will not invent `default(T)`.

## Dynamic rows

Bind `ItemsSource` to `IList<VestigiumPropertyItem>` with `Mode=OneWay` and leave selected objects empty.

## Do not

- Bind `SelectedObject` / `SelectedObjects` / `ItemsSource` / `CategoryIcons` TwoWay onto a get-only CLR property.
- Bind grouping alignment to `EditorTextAlignment`. They are two DPs on purpose.
- Expect grouping chrome in Alphabetical sort.
- Expect numbers to stay right unless you set `EditorTextAlignment="Right"` or `[VestigiumTextAlignment]`.
- Reference `System.Windows.Forms` or host a WinForms `PropertyGrid`.
- Reference `Vestigium.Themes*` for these glyphs. They are geometry, not a theme.
- Expect `UITypeEditor` dialogs or a host `EditorTemplates` map. That is later.
- Expand two collections at once in multi-select. The control refuses.
- Rebuild the row list yourself on every `PropertyChanged`. The engine coalesces value updates.
