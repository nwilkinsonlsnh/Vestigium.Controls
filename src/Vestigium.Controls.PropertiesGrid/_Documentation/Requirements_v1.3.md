# Vestigium.Controls.PropertiesGrid — Software Requirements Specification

**Document ID:** VEST-CTL-PG-SRS-001  
**Version:** 1.3  
**Status:** Implemented  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM  
**Supersedes:** v1.2

v1.3 adds **editor text alignment** and **category header images**. All v1.2 behavior remains.

---

## 1. Editor text alignment

TTL / Timeout sitting on the far right is a NumericUpDown default (`TextAlignment.Right`), not a grid rule.

The grid owns alignment for every editor.

| Property | Type | Default | Notes |
|---|---|---|---|
| `EditorTextAlignment` | `System.Windows.TextAlignment` | **Left** | Applies to Text, Mixed, Numeric, Enum, DateTime, Color hex, ReadOnly value text. |

Supported values in v1: **Left**, **Center**, **Right**. `Justify` coerces to Left.

Numeric editors bind `VestigiumNumericUpDown.TextAlignment` (that control keeps **Right** as its own standalone default).

Per-property override:

```csharp
[Category("Timing"), VestigiumTextAlignment(TextAlignment.Right)]
public decimal Timeout { get; set; }
```

`VestigiumPropertyItem.TextAlignment` (nullable) wins over the grid. `EffectiveTextAlignment` is what the template binds.

Checkboxes do not shift; the box stays at the start of the value column.

---

## 2. Category images

Category headers MAY show a 14×14 glyph to the right of the chevron.

Host assigns `CategoryIcons` (`IList` / `VestigiumCategoryIconCollection`). Nothing is shown until the host sets it. Not a theme.

```xml
<pg:VestigiumPropertiesGrid SelectedObject="{Binding Selected}"
                            CategoryIcons="{Binding CategoryIcons}"/>
```

```csharp
CategoryIcons = VestigiumCategoryGlyphs.CreateStandard();
```

`VestigiumCategoryIcon` fields (first match by category name, case-insensitive):

| Field | Use |
|---|---|
| `Category` | Header name (`Timing`, `General`, …) |
| `Image` | `ImageSource` (PNG, ICO, BMP, `DrawingImage`) |
| `ImageUri` | Pack or file URI for raster. `.svg` is parsed as vector. |
| `IconGeometry` | WPF `Geometry` |
| `IconData` | Path mini-language (`M2,2 H14 V14 H2 Z`) |
| `Svg` | Inline SVG. First `d="..."` path is used. |

Resolve order: **Image → ImageUri raster → IconGeometry → IconData → Svg / .svg URI**. Bad data is ignored. No throw.

`VestigiumCategoryGlyphs.CreateStandard()` is an optional vector pack for General, Timing, Display, Network, Advanced, Misc. Hosts copy and replace any entry.

Alphabetical sort has no category headers, so no icons.

---

## 3. NumericUpDown

`VestigiumNumericUpDown.TextAlignment` (default **Right**) is the spinner’s own setting. The properties grid always overwrites it from `EffectiveTextAlignment`.

---

## 4. Tests added in v1.3

| ID | Assertion |
|---|---|
| PG-T25 | Engine default alignment is Left |
| PG-T26 | Changing `EditorTextAlignment` updates `EffectiveTextAlignment` |
| PG-T27 | `[VestigiumTextAlignment]` overrides the grid |
| PG-T28 | Standard pack attaches geometry to Timing |
| PG-T29 | A category with no map entry has no glyph |
| PG-T30 | Inline SVG `d=` parses to geometry |
| PG-T31 | Bad `IconData` does not throw |

v1.2 tests PG-T01–T24 still apply.

---

## 5. Demo

Probe tab: Left / Center / Right buttons. Default Left so TTL and Timeout sit with the other editors.

Category headers show the standard vector pack.

Host may still assign PNG via `Image` / `ImageUri`.
