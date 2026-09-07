# Vestigium.Controls.PropertiesGrid — Software Requirements Specification

**Document ID:** VEST-CTL-PG-SRS-001  
**Version:** 1.5  
**Status:** Implemented  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM  
**Supersedes:** v1.4  
**Source of truth** for `Vestigium.Controls.PropertiesGrid`.

v1.5 consolidates v1.2–v1.4 and adds the **host binding contract** that prevents TwoWay writes into get-only ViewModel properties.

| Version | What it added |
|---|---|
| 1.2 | Multi-select, inline collections, reset, nested expand |
| 1.3 | `EditorTextAlignment`, `[VestigiumTextAlignment]`, `CategoryIcons` |
| 1.4 | Grouping header orientation, alignment, bold |
| 1.5 | OneWay source bindings; grouping and editor alignment stay independent |

---

## 1. Product

A lookless WPF property inspector. Bind one object or many. The control lists browsable properties in a two-column name / editor grid, grouped by category, with search, a description footer, reset, expandable nested objects, and inline collection editing.

It is the WinForms `PropertyGrid` job with a Windows 10/11 surface. It is not a port of `System.Windows.Forms.PropertyGrid` and SHALL NOT reference WinForms.

### 1.1 What v1 is for

ProbeHost / PingIQ settings panes: inspect a probe, compare two probes, edit a hop list, reset a field, drill into nested thresholds. Hosts bind `SelectedObject` or `SelectedObjects`. They do not build rows by hand unless they opt into `ItemsSource`.

### 1.2 What v1 is not

Not a DataGrid. Not a theme engine. Not a WinForms `UITypeEditor` host. Not Visual Studio's Property Window (no property tabs). Not a host-extensible editor catalog (no `EditorTemplates` dictionary in v1).

---

## 2. Architecture

Lookless `Control` named `VestigiumPropertiesGrid`. Visual tree in `Themes/Generic.xaml`.

| Part | Role |
|---|---|
| `PART_Search` | Filter box |
| `PART_Toolbar` | Categorized / Alphabetical, Reset |
| `PART_List` | Virtualized property rows |
| `PART_Splitter` | Resize name column |
| `PART_Description` | Footer for the selected row |

`PropertyEngine` owns descriptor scan, multi-select merge, filter, sort, expand, collections, reset, commit, alignment, category icons, and INPC coalesce. The control wires parts. Demo ViewModels bind DPs.

### 2.1 Ways to feed rows

| Mode | When |
|---|---|
| **SelectedObject** | One instance. Reflect with `TypeDescriptor`. |
| **SelectedObjects** | One or more instances. Intersection merge. Wins over `SelectedObject` when non-empty. |
| **ItemsSource** | Host supplies `IList<VestigiumPropertyItem>`. No reflection. Wins over both selected modes. |

Priority: `ItemsSource` → `SelectedObjects` → `SelectedObject` → empty state.

### 2.2 Host binding contract (v1.5)

`SelectedObject`, `SelectedObjects`, `ItemsSource`, and `CategoryIcons` SHALL register **without** `BindsTwoWayByDefault`. Default binding mode is OneWay.

The grid inspects the bound object. It SHALL NOT write the inspected object back onto the host.

| Binding | Required mode | Why |
|---|---|---|
| `SelectedObject` | **OneWay** | Host owns the instance. |
| `SelectedObjects` | **OneWay** | Host owns the set. |
| `ItemsSource` | **OneWay** | Host owns the row list. |
| `CategoryIcons` | **OneWay** | Host owns the glyph map. |
| `SelectedProperty` | TwoWay allowed | Footer + Reset selection. |
| `SearchText` | TwoWay allowed | Search box. |

Hosts that expose a get-only computed property (for example `object? GridTarget => Selected`) MUST bind with `Mode=OneWay`. A TwoWay binding against a get-only CLR property throws:

`InvalidOperationException: A TwoWay or OneWayToSource binding cannot work on the read-only property …`

Recommended multi-select XAML:

```xml
<pg:VestigiumPropertiesGrid SelectedObject="{Binding Selected, Mode=OneWay}"
                            SelectedObjects="{Binding GridTargets, Mode=OneWay}"/>
```

When `SelectedObjects` is null or empty, the engine falls through to `SelectedObject`.

### 2.3 Theming is assigned by the host

This assembly SHALL NOT project-reference `Vestigium.Themes*`, call `ThemeManager`, expose a `Theme` property, or ship a palette. Category glyphs are geometry / images the host assigns, not a theme.

It MAY project-reference `Vestigium.Controls.NumericUpDown` for numeric editors.

`Generic.xaml` fallbacks: Background `#0F2744`, Row `#132744`, Text `#E2E8F0`, Muted `#94A3B8`, Hairline `#1E3A5F`, Accent `#4A90C8`, Category header `#0B1524`, Mixed `#64748B`.

### 2.4 Encapsulated work

Hosts do not write debounce timers.

- Rebuild the descriptor list when selected identity / type set changes.
- `INotifyPropertyChanged` on any selected object updates **values**, not the row list. Storms coalesce on a 50–250 ms AIMD drain.
- Filter keystrokes filter a cached tree. No re-reflect per key.
- Children of an expandable / collection row are discovered **on first expand**, then cached.
- Commit is STA. No `Task.Run` for setters.
- Unloaded: stop the drain timer.

A ViewModel that starts a timer to "protect" this control means the pipeline failed.

---

## 3. Types

```csharp
public enum VestigiumPropertySort { Categorized = 0, Alphabetical = 1 }

public enum VestigiumPropertyEditorKind
{
    Text = 0, Boolean = 1, Enum = 2, Numeric = 3,
    DateTime = 4, Color = 5, ReadOnly = 6, Expandable = 7, Collection = 8
}
```

`VestigiumPropertyItem` is the row VM the template binds: `Name`, `Category` (default `Misc`), `Description`, `Kind`, `IsReadOnly`, `IsMixed`, `CanReset`, `Value`, `Choices`, `Path`, `Depth`, `Children`, `TextAlignment` (nullable override), `EffectiveTextAlignment`, `CategoryImage`, `CategoryGeometry`, `HasCategoryGlyph`.

---

## 4. Property matrix

| Property | Type | Default | Notes |
|---|---|---|---|
| `SelectedObject` | `object?` | `null` | OneWay. Single instance. |
| `SelectedObjects` | `IList` | empty | OneWay. Wins when `Count > 0`. |
| `ItemsSource` | `IEnumerable?` | `null` | OneWay. Wins over reflection. |
| `Sort` | `VestigiumPropertySort` | **Categorized** | |
| `SearchText` | `string` | `""` | TwoWay. Case-insensitive contains. |
| `ShowSearch` | `bool` | `true` | |
| `ShowToolbar` | `bool` | `true` | |
| `ShowDescription` | `bool` | `true` | Footer. |
| `NameColumnWidth` | `double` | `160` | Splitter writes this. |
| `SelectedProperty` | `VestigiumPropertyItem?` | `null` | TwoWay. Footer + Reset. |
| `IsReadOnly` | `bool` | `false` | Grid-wide lock. |
| `MaxExpandDepth` | `int` | **8** | `<= 0` coerces to 8. |
| `EditorTextAlignment` | `TextAlignment` | **Left** | Editors only. Justify → Left. |
| `CategoryIcons` | `IList` | empty | OneWay. Nothing shown until assigned. |
| `CategoryTextAlignment` | `TextAlignment` | **Left** | Grouping headers only. Justify → Left. |
| `IsCategoryBold` | `bool` | **true** | Grouping header type. |
| `CategoryOrientation` | `Orientation` | **Horizontal** | Horizontal row or stacked. |

No `Theme`. No `EditorTemplates`. No `PropertyTabs`.

Empty sources render: "No object selected."

---

## 5. Editor text alignment (v1.3)

TTL / Timeout sitting on the far right was a NumericUpDown standalone default (`TextAlignment.Right`), not a grid rule. The grid owns alignment for every editor.

Applies to Text, Mixed, Numeric, Enum, DateTime, Color hex, ReadOnly value text.

Supported: **Left**, **Center**, **Right**. `Justify` coerces to Left.

Numeric editors bind `VestigiumNumericUpDown.TextAlignment`. That control keeps **Right** as its own standalone default. Inside this grid, `EffectiveTextAlignment` overwrites it.

Per-property override wins over the grid:

```csharp
[Category("Timing"), VestigiumTextAlignment(TextAlignment.Right)]
public decimal Timeout { get; set; }
```

`VestigiumPropertyItem.TextAlignment` (nullable) → `EffectiveTextAlignment` (what the template binds).

Checkboxes do not shift; the box stays at the start of the value column.

Changing `EditorTextAlignment` SHALL NOT move grouping headers.

---

## 6. Grouping headers (v1.4)

Category rows are independent of editor cells.

| Property | Default | Behavior |
|---|---|---|
| `CategoryTextAlignment` | Left | Aligns the header cluster (chevron + icon + name) across the **full row**. |
| `IsCategoryBold` | **true** | Host may unbold. |
| `CategoryOrientation` | Horizontal | Horizontal = one row. Vertical = stacked; row grows (`MinHeight` 32). |

`CategoryOrientation` is `System.Windows.Controls.Orientation`. It is not text rotation.

Live: templates bind these DPs from the grid (`AncestorType`). No engine rebuild required.

Alphabetical sort has no headers, so these DPs have no visible effect until Categorized is on.

---

## 7. Category images (v1.3)

Category headers MAY show a 14×14 glyph to the right of the chevron. Nothing appears until the host assigns `CategoryIcons` (`IList` / `VestigiumCategoryIconCollection`).

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
| `IconData` | Path mini-language |
| `Svg` | Inline SVG. First `d="..."` path is used. |

Resolve order: **Image → ImageUri raster → IconGeometry → IconData → Svg / .svg URI**. Bad data is ignored. No throw.

`VestigiumCategoryGlyphs.CreateStandard()` is an optional vector pack for General, Timing, Display, Network, Advanced, Misc. Hosts copy and replace any entry.

Alphabetical sort has no category headers, so no icons.

---

## 8. Discovery rules

Use `System.ComponentModel.TypeDescriptor`, not raw `GetProperties()`.

| Attribute / fact | Behavior |
|---|---|
| `[Browsable(false)]` | Hidden |
| Indexer named `Item` | Hidden as a property row. Collection items use `[n]`. |
| `[DisplayName]` | Row label |
| `[Category]` | Group. Missing → `Misc` |
| `[Description]` | Footer |
| `[ReadOnly(true)]` or no public setter | `ReadOnly` editor |
| `[DefaultValue]` | Enables Reset when current ≠ default |
| `[VestigiumTextAlignment]` | Per-property editor alignment |
| Standard `TypeConverter` | Parse / format |
| Custom WinForms `UITypeEditor` | **Ignored.** |

Public instance properties only. No fields. No statics.

Same type, new instance: keep expand / scroll if paths still match. Type set change: rebuild.

---

## 9. Multi-select (`SelectedObjects`)

Intersection, not union. A property appears only when **every** selected object has a browsable property with the same **name** and **compatible type**.

| Situation | Editor |
|---|---|
| All values equal | Normal editor |
| Values differ | `IsMixed = true`. Bool → indeterminate. Others → empty + "Multiple values". |
| Commit on a mixed row | Writes the new value to **every** selected object. |
| Escape | Leaves objects unchanged |

`PropertyValueChanged` fires once per object that actually changed.

Collection rows do not expand while more than one object is selected.

Toolbar caption may show `{n} objects` when `n > 1`.

---

## 10. Nested expand

Any property whose type is a non-primitive, non-string, non-enum, non-Color, non-DateTime **class or struct** with browsable children is `Expandable`.

- Children load on first expand.
- Expand is refused when `Depth >= MaxExpandDepth` (default 8).
- Cycle: instance already an ancestor on `Path` → ReadOnly `(circular)`, no chevron.
- Structs expand. Changing a child writes the mutated struct back onto the parent.
- Null nested object: `(none)`. Chevron disabled. v1 does not construct nested objects.
- Session remembers expanded paths across SelectedObject instance swaps of the same type.

---

## 11. Collection editor (inline, not a modal)

A property is `Collection` when the value implements `IList` (including `IList<T>` and `T[]`).

v1 does **not** edit `IDictionary` as a first-class grid.

Collection row shows `Count = N` plus **Add**, **Remove**, **Up**, **Down**.

- **Add** appends `T` via parameterless ctor, `""`, `0`, or first enum. No constructor → Add disables, no throw.
- **Remove** deletes the selected child. Disabled when none selected or list is fixed-size.
- **Up / Down** reorder. Disabled at ends or when fixed-size.
- Arrays are fixed-size: Add / Remove / Up / Down disabled. Items still expand and edit.
- `IList.IsReadOnly` or grid `IsReadOnly` → strip disabled.

Expand to see `[0]`, `[1]`, … with the editor kind of `T`. `INotifyCollectionChanged` rebuilds children (coalesced).

---

## 12. Reset

A row `CanReset` when the property has `[DefaultValue]` and the current value is not equal to that default.

| Action | Behavior |
|---|---|
| Row context menu **Reset** | Writes the default onto every target for that path. |
| Toolbar **Reset** | Same, for `SelectedProperty`. |
| Toolbar **Reset all** | Resets every realized root-level row that `CanReset`. |

No `[DefaultValue]` → no Reset. Do **not** invent `default(T)` except bool (`false`).

---

## 13. Editors

`DataTemplateSelector` on `Kind`. Built-ins only in v1.

| Kind | Control | Commit |
|---|---|---|
| Text | `TextBox` | Enter / LostFocus. Escape reverts. |
| Boolean | `CheckBox` | Immediate. Indeterminate when mixed. |
| Enum | `ComboBox` | Immediate on selection. |
| Numeric | `VestigiumNumericUpDown` | Immediate + Auto. Integer vs decimal from the property type. Unsigned for `byte` / `uint` / `ulong`. |
| DateTime | `DatePicker` + optional time | LostFocus. |
| Color | Swatch + hex | LostFocus / Enter. Bad hex reverts. |
| ReadOnly | `TextBlock` | No edit. |
| Expandable | Chevron + nested rows | Child editors. |
| Collection | Count + Add/Remove/Up/Down | Children as above. |

Failed parse never throws. Revert to last good value.

---

## 14. Sort, search, keyboard

**Categorized (default):** category headers A–Z. Properties A–Z inside. Headers collapse. Remember collapse per category name for the session.

**Alphabetical:** flat list of root rows, no headers. Nested children still indent under their parent when expanded.

**Search:** live filter on name / category / description. Matching a parent keeps it. Matching only a child expands the ancestors and shows the hit. Category headers hide when no child remains.

**Keys:** Up/Down move rows. Left/Right collapse/expand. Enter begins edit (or toggles bool). Escape cancels edit. Tab next editor. F2 edit. Delete on a selected collection item = Remove.

---

## 15. Layout

- Property row min-height 32 dip. Hairline between rows. No 3D sunken cells.
- Selected row: 3 dip accent bar on the left + fill `#132744`.
- Category header: min-height 32 dip, uppercase 11 px tracking, chevron, optional glyph, full-row alignment cluster.
- Nested rows indent 16 dip per depth.
- Name column left, editor column fills. Splitter between them.
- Search placeholder "Filter properties".
- Description footer: 72 dip min, wraps, `DisplayName` + description + `Path`. Mixed rows add "Multiple values."
- Segoe UI. No Classic border. No yellow help pane. No toolbox bitmap strip. No modal collection window.

The control stretches to its parent. Virtualize the flattened visible row list.

---

## 16. Demo

`Vestigium.Controls.PropertiesGrid.Demo` SHALL show:

1. Probe sample — categories `General`, `Timing`, `Display` (string, int, decimal, bool, enum, Color, DateTime).
2. Categorized vs Alphabetical.
3. Search live filter, including a hit on a nested child.
4. Read-only property and grid-wide `IsReadOnly`.
5. Nested expand — `Thresholds.Warning.Latency` at least three levels.
6. Circular reference row shows `(circular)`.
7. Collection — `Hops`: Add / Remove / reorder / edit `[n].Host`.
8. Array — fixed-size Ports: items editable, Add disabled.
9. Multi-select — two probes, mixed DisplayName, one commit writes both. Bind `Selected` + `GridTargets` OneWay. No get-only TwoWay crash.
10. Reset — `[DefaultValue]` on TTL; Reset and Reset all.
11. Description footer updates on row change.
12. Empty state when nothing is selected.
13. ItemsSource tab with hand-built items.
14. Nested inside a default Vestigium form cell.
15. Editor alignment Left / Center / Right (default Left).
16. Grouping alignment Left / Center / Right, Horizontal / Vertical, Bold checkbox (on by default).
17. Category headers show `VestigiumCategoryGlyphs.CreateStandard()`.

The demo does not initialize Vestigium.Themes.

---

## 17. Tests

| ID | Assertion |
|---|---|
| PG-T01–T24 | v1.2 discovery, multi-select, collections, reset, nested, circular |
| PG-T25 | Engine default alignment is Left |
| PG-T26 | Changing `EditorTextAlignment` updates `EffectiveTextAlignment` |
| PG-T27 | `[VestigiumTextAlignment]` overrides the grid |
| PG-T28 | Standard pack attaches geometry to Timing |
| PG-T29 | A category with no map entry has no glyph |
| PG-T30 | Inline SVG `d=` parses to geometry |
| PG-T31 | Bad `IconData` does not throw |
| PG-T32 | Defaults: category Left, Bold true, Horizontal |
| PG-T33 | Category Center + Vertical + unbold does not change `EditorTextAlignment` |
| PG-T34 | `SelectedObject` metadata does not include `BindsTwoWayByDefault` |

---

## 18. Acceptance

1. Demo §16 items 1–17 work on Generic fallbacks.
2. Looks like a Windows 11 settings inspector, not a 1990s PropertyGrid.
3. Search does not hitch on a 50-property tree.
4. Holding a nested NumericUpDown does not lock the window.
5. Two selected probes: mixed row, one edit, both models change.
6. Hop collection Add / Remove / reorder works; array Ports refuses Add.
7. Reset TTL restores the `[DefaultValue]`.
8. Three-level expand works; circular stops.
9. Editor Left keeps TTL with other editors. Grouping Center does not move TTL.
10. Bold grouping is on by default and can be turned off.
11. Multi-select tab does not throw TwoWay on a get-only VM property.
12. Listed tests pass.
13. No Themes or WinForms project reference.

---

## 19. Later (not v1)

- Host `EditorTemplates` dictionary by `Type`
- `IDictionary` editor
- Flags enum checkboxes
- Password / masked text
- Property pages / tabs
- Construct null nested objects from a factory
- Drag-drop reorder of collection items (buttons ship first)
