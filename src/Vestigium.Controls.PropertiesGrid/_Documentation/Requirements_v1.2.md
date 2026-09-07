# Vestigium.Controls.PropertiesGrid — Software Requirements Specification

**Document ID:** VEST-CTL-PG-SRS-001  
**Version:** 1.2  
**Status:** Implemented  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM  
**Supersedes:** v1.1

This document is the source of truth for `Vestigium.Controls.PropertiesGrid` once accepted.

v1.2 adds what v1.1 parked: **multi-select**, **inline collection editor**, **reset to DefaultValue**, and **nested expand**. Host `EditorTemplates` by `Type` stays out.

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

### 2.1 Shape

Lookless `Control` named `VestigiumPropertiesGrid` (replace the current `UserControl` stub). Visual tree in `Themes/Generic.xaml`.

Template parts:

| Part | Role |
|---|---|
| `PART_Search` | Filter box |
| `PART_Toolbar` | Categorized / Alphabetical, Reset |
| `PART_List` | Virtualized property rows |
| `PART_Splitter` | Resize name column |
| `PART_Description` | Footer for the selected row |

`PropertyEngine` owns descriptor scan, multi-select merge, filter, sort, expand, collections, reset, commit, and INPC coalesce. The control wires parts. Demo ViewModels bind DPs.

### 2.2 Ways to feed rows

| Mode | When |
|---|---|
| **SelectedObject** | One instance. Reflect with `TypeDescriptor`. |
| **SelectedObjects** | One or more instances. Intersection merge. Wins over `SelectedObject` when non-empty. |
| **ItemsSource** | Host supplies `IList<VestigiumPropertyItem>`. No reflection. Wins over both selected modes. |

Priority: `ItemsSource` → `SelectedObjects` → `SelectedObject` → empty state.

`SelectedObject` is a convenience wrapper: get returns the only item when the set has one, else `null`. Set replaces `SelectedObjects` with a one-element list (or empty if null).

### 2.3 Theming is assigned by the host

This assembly SHALL NOT project-reference `Vestigium.Themes*`, call `ThemeManager`, expose a `Theme` property, or ship a palette.

It MAY project-reference `Vestigium.Controls.NumericUpDown` for numeric editors.

`Generic.xaml` fallbacks:

| Token | Fallback |
|---|---|
| Background | `#0F2744` |
| Row | `#132744` |
| Text | `#E2E8F0` |
| Muted | `#94A3B8` |
| Hairline | `#1E3A5F` |
| Accent / selected | `#4A90C8` |
| Category header | `#0B1524` |
| Mixed / indeterminate | `#64748B` |

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
public enum VestigiumPropertySort
{
    Categorized = 0,
    Alphabetical = 1
}

public enum VestigiumPropertyEditorKind
{
    Text = 0,
    Boolean = 1,
    Enum = 2,
    Numeric = 3,
    DateTime = 4,
    Color = 5,
    ReadOnly = 6,
    Expandable = 7,
    Collection = 8
}
```

`VestigiumPropertyItem` is the row VM the template binds:

| Field | Notes |
|---|---|
| `Name` | Display name |
| `Category` | Default `"Misc"` |
| `Description` | Footer text |
| `Kind` | Editor selector |
| `IsReadOnly` | |
| `IsMixed` | Multi-select values disagree |
| `CanReset` | Has `[DefaultValue]` and current ≠ default |
| `Value` | Boxed current value. `null` when mixed. |
| `Choices` | Enum names |
| `Path` | Dotted path (`Thresholds.Warning`, `Hops[2].Host`) |
| `Depth` | 0 = root |
| `Children` | Lazy |

---

## 4. Property matrix

| Property | Type | Default | Notes |
|---|---|---|---|
| `SelectedObject` | `object?` | `null` | Convenience for a single instance. |
| `SelectedObjects` | `IList` | empty | Multi-select source. |
| `ItemsSource` | `IEnumerable?` | `null` | Wins over reflection when set. |
| `Sort` | `VestigiumPropertySort` | **Categorized** | |
| `SearchText` | `string` | `""` | Case-insensitive contains on name, category, description. |
| `ShowSearch` | `bool` | `true` | |
| `ShowToolbar` | `bool` | `true` | |
| `ShowDescription` | `bool` | `true` | Footer. |
| `NameColumnWidth` | `double` | `160` | Splitter writes this. |
| `SelectedProperty` | `VestigiumPropertyItem?` | `null` | Two-way. Drives footer + Reset. |
| `IsReadOnly` | `bool` | `false` | Grid-wide lock. Disables Reset, Add, Remove. |
| `MaxExpandDepth` | `int` | **8** | Hard stop. `<= 0` coerces to 8. |

No `Theme`. No `EditorTemplates`. No `PropertyTabs`.

Empty sources render: "No object selected."

---

## 5. Discovery rules

Use `System.ComponentModel.TypeDescriptor`, not raw `GetProperties()`.

| Attribute / fact | Behavior |
|---|---|
| `[Browsable(false)]` | Hidden |
| Indexer named `Item` | Hidden as a property row. Collection items use `[n]` instead. |
| `[DisplayName]` | Row label |
| `[Category]` | Group. Missing → `Misc` |
| `[Description]` | Footer |
| `[ReadOnly(true)]` or no public setter | `ReadOnly` editor |
| `[DefaultValue]` | Enables Reset when current ≠ default |
| Standard `TypeConverter` | Parse / format |
| Custom WinForms `UITypeEditor` | **Ignored.** |

Public instance properties only. No fields. No statics.

Same type, new instance: keep expand / scroll if paths still match. Type set change: rebuild.

---

## 6. Multi-select (`SelectedObjects`)

Intersection, not union.

A property appears only when **every** selected object has a browsable property with the same **name** and **compatible type** (same `PropertyType`, or both numeric, or both enum of the same enum type).

| Situation | Editor |
|---|---|
| All values equal | Normal editor, that value |
| Values differ | `IsMixed = true`. Bool → indeterminate checkbox. Text / numeric / enum / color → empty + muted placeholder "Multiple values". |
| Commit on a mixed row | Writes the new value to **every** selected object. Clears mixed. |
| Escape | Leaves objects unchanged |

`PropertyValueChanged` fires once per object that actually changed.

Expandable / collection rows are available in multi-select only when every object has that property and the child merge still follows intersection. Mixed collection counts show "Multiple values" and do not expand until the host selects a single object. (Expanding two different lists in one tree is undefined. v1 refuses.)

Toolbar caption may show `{n} objects` when `n > 1`.

---

## 7. Nested expand

Not one level. Any property whose type is a non-primitive, non-string, non-enum, non-Color, non-DateTime **class or struct** with browsable children is `Expandable`.

Rules:

- Children load on first expand.
- `Depth` starts at 0. Expand is refused when `Depth >= MaxExpandDepth`.
- Cycle: if the instance is already an ancestor on `Path`, the row is ReadOnly text (`(circular)`) and has no chevron.
- Structs expand. Changing a child writes the mutated struct back onto the parent property.
- Null nested object: row shows `(none)`. Chevron disabled until the host assigns an instance. v1 does not construct nested objects for you.
- Session remembers expanded paths (`Thresholds.Warning`) across SelectedObject instance swaps of the same type.

---

## 8. Collection editor (inline, not a modal)

A property is `Collection` when the value implements `IList` (including `IList<T>` and `T[]`).

v1 does **not** edit `IDictionary` as a first-class grid. Dictionary properties are ReadOnly with `Count` text unless the host flattens them through `ItemsSource`.

### 8.1 Row

Collection row editor column shows `Count = N` and an inline button strip: **Add**, **Remove**, **Up**, **Down**.

- **Add** appends a new item. `T` with a public parameterless constructor → `Activator.CreateInstance`. String → `""`. Numeric → `0`. Enum → first member. No constructor → Add disables, no throw.
- **Remove** deletes the **selected child item** row. Disabled when none selected or list is fixed-size.
- **Up / Down** reorder the selected child. Disabled at ends or when the list is fixed-size.
- Arrays (`T[]`) are fixed-size: Add / Remove / Up / Down disabled. Items still expand and edit.
- `IList.IsReadOnly` or grid `IsReadOnly` → strip disabled.

### 8.2 Children

Expand the collection to see `[0]`, `[1]`, … each with the editor kind of `T`. If `T` is expandable, those rows expand under the same depth rules.

`INotifyCollectionChanged` on the list rebuilds children (coalesced). `INotifyPropertyChanged` on an item updates that child.

---

## 9. Reset

A row `CanReset` when the property has `[DefaultValue]` and the current value is not equal to that default (`Equals` / comparer for that type).

| Action | Behavior |
|---|---|
| Row context menu **Reset** | Writes the default onto every target object for that path. |
| Toolbar **Reset** | Same, for `SelectedProperty`. Disabled when `CanReset` is false. |
| Toolbar **Reset all** | Resets every root-level row that `CanReset`. Does not walk collapsed children until they have been realized. |

Reset fires `PropertyValueChanged` per object per property that actually changed. Mixed multi-select: Reset writes the default to all, which also clears mixed if they now agree.

No `[DefaultValue]` → no Reset. Missing default is not inferred from `default(T)` except for bool (`false`) — do **not** invent defaults.

---

## 10. Editors

`DataTemplateSelector` on `Kind`. Built-ins only in v1. No host `EditorTemplates` map.

| Kind | Control | Commit |
|---|---|---|
| Text | `TextBox` | Enter / LostFocus. Escape reverts. |
| Boolean | `CheckBox` | Immediate. Indeterminate when mixed. |
| Enum | `ComboBox` | Immediate on selection. |
| Numeric | `VestigiumNumericUpDown` | That control's Immediate + Auto. Integer vs decimal from the property type. Unsigned for `byte` / `uint` / `ulong`. |
| DateTime | `DatePicker` + optional time | LostFocus. |
| Color | Swatch + hex | LostFocus / Enter. Bad hex reverts. |
| ReadOnly | `TextBlock` | No edit. |
| Expandable | Chevron + nested rows | Child editors. |
| Collection | Count + Add/Remove/Up/Down | Children as above. |

Failed parse never throws. Revert to last good value.

`PropertyValueChanged` after a successful commit: `Path`, `OldValue`, `NewValue`, `Target` (the instance written).

---

## 11. Sort, search, keyboard

**Categorized (default):** category headers A–Z. Properties A–Z inside. Headers collapse. Remember collapse per category name for the session.

**Alphabetical:** flat list of root rows, no headers. Nested children still indent under their parent when expanded.

**Search:** live filter on name / category / description. Matching a parent keeps it. Matching only a child expands the ancestors and shows the hit. Category headers hide when no child remains.

**Keys:** Up/Down move rows. Left/Right collapse/expand. Enter begins edit (or toggles bool). Escape cancels edit. Tab next editor. F2 edit. Delete on a selected collection item = Remove.

---

## 12. Layout and look (Windows 10/11, not WinForms)

- Row height 32 dip. Hairline between rows. No 3D sunken cells.
- Selected row: 3 dip accent bar on the left + fill `#132744`.
- Category header: 28 dip, uppercase 11 px tracking, chevron, no gradient bar.
- Nested rows indent 16 dip per depth.
- Name column left, editor column fills. Splitter between them.
- Search placeholder "Filter properties".
- Description footer: 72 dip min, wraps, `DisplayName` + description + `Path`. Mixed rows add "Multiple values."
- Segoe UI. No Classic border. No yellow help pane. No toolbox bitmap strip. No modal collection window.

The control stretches to its parent. Virtualize the flattened visible row list.

---

## 13. Public surface

```xml
xmlns:pg="clr-namespace:Vestigium.Controls.PropertiesGrid;assembly=Vestigium.Controls.PropertiesGrid"

<pg:VestigiumPropertiesGrid SelectedObject="{Binding SelectedProbe}"
                            Sort="Categorized"
                            ShowDescription="True"/>

<pg:VestigiumPropertiesGrid SelectedObjects="{Binding SelectedProbes}"/>
```

```csharp
public class VestigiumPropertiesGrid : Control
{
    public object? SelectedObject { get; set; }
    public IList SelectedObjects { get; set; }
    public VestigiumPropertySort Sort { get; set; }
    public string SearchText { get; set; }
    public VestigiumPropertyItem? SelectedProperty { get; set; }
    public int MaxExpandDepth { get; set; }
    public event EventHandler<VestigiumPropertyValueChangedEventArgs>? PropertyValueChanged;
}
```

---

## 14. Demo

`Vestigium.Controls.PropertiesGrid.Demo` SHALL show:

1. **Probe sample** — categories `General`, `Timing`, `Display` (string, int, decimal, bool, enum, Color, DateTime).
2. **Categorized vs Alphabetical**.
3. **Search** live filter, including a hit on a nested child.
4. **Read-only** property and grid-wide `IsReadOnly`.
5. **Nested expand** — `Thresholds.Warning.Latency` at least three levels.
6. **Circular reference** row shows `(circular)` and will not expand forever.
7. **Collection** — `Hops` as `ObservableCollection<Hop>`: Add / Remove / reorder / edit `[n].Host`.
8. **Array** — fixed-size `int[]` Ports: items editable, Add disabled.
9. **Multi-select** — two probes, mixed DisplayName, one commit writes both. Side panel proves it.
10. **Reset** — `[DefaultValue]` on TTL; Reset and Reset all.
11. **Description footer** updates on row change; mixed caption when multi-select disagrees.
12. **Empty state** when nothing is selected.
13. **ItemsSource** tab with hand-built items.
14. Nested inside a default Vestigium form cell.

The demo does not initialize Vestigium.Themes.

---

## 15. Tests

| ID | Assertion |
|---|---|
| PG-T01 | `[Browsable(false)]` is omitted |
| PG-T02 | Missing category lands in `Misc` |
| PG-T03 | `[DisplayName]` is the row label |
| PG-T04 | No setter → ReadOnly kind |
| PG-T05 | Enum property → Enum kind |
| PG-T06 | `int` / `decimal` → Numeric kind |
| PG-T07 | Search "ttl" hides non-matches |
| PG-T08 | Alphabetical has no category headers |
| PG-T09 | Bad hex Color commit reverts |
| PG-T10 | INPC on the object updates the row value |
| PG-T11 | `ItemsSource` wins over `SelectedObject` |
| PG-T12 | Null selected object → empty state |
| PG-T13 | csproj has no `Vestigium.Themes*` and no `System.Windows.Forms` reference |
| PG-T14 | Two objects, different names → row `IsMixed` |
| PG-T15 | Commit on mixed writes both targets |
| PG-T16 | Intersection drops a property only one type has |
| PG-T17 | `IList<T>` → Collection kind |
| PG-T18 | Add on `List<T>` with ctor increases Count by 1 |
| PG-T19 | Add on `T[]` is a no-op |
| PG-T20 | Reset writes `[DefaultValue]` |
| PG-T21 | Reset disabled when value already equals default |
| PG-T22 | Expand depth 3 is reachable when `MaxExpandDepth` is 8 |
| PG-T23 | Circular instance → `(circular)`, no infinite children |
| PG-T24 | Multi-select collection does not expand |

---

## 16. Acceptance

1. Demo §14 items 1–14 work on Generic fallbacks.
2. Looks like a Windows 11 settings inspector, not a 1990s PropertyGrid.
3. Search does not hitch on a 50-property tree.
4. Holding a nested NumericUpDown does not lock the window.
5. Two selected probes: mixed row, one edit, both models change.
6. Hop collection Add / Remove / reorder works; array Ports refuses Add.
7. Reset TTL restores the `[DefaultValue]`.
8. Three-level expand works; circular stops.
9. Listed tests pass.
10. No Themes or WinForms project reference.

---

## 17. Later (not v1)

- Host `EditorTemplates` dictionary by `Type`
- `IDictionary` editor
- Flags enum checkboxes
- Password / masked text
- Property pages / tabs
- Construct null nested objects from a factory
- Drag-drop reorder of collection items (buttons ship first)
