# Vestigium.Controls.PropertiesGrid — Software Requirements Specification

**Document ID:** VEST-CTL-PG-SRS-001  
**Version:** 1.1  
**Status:** Planning — accepted for review, not yet implemented  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM  
**Supersedes:** v1.0 (stub note)

This document is the source of truth for `Vestigium.Controls.PropertiesGrid` once accepted.

---

## 1. Product

A lookless WPF property inspector. Bind `SelectedObject`. The control lists browsable properties in a two-column name / editor grid, grouped by category, with a search box and a description footer.

It is the WinForms `PropertyGrid` job — inspect one object, edit values in place — with a Windows 10/11 surface. It is not a port of `System.Windows.Forms.PropertyGrid` and SHALL NOT reference WinForms.

### 1.1 What v1 is for

ProbeHost / PingIQ style settings panes: one selected model, categories, typed editors, a sentence of help under the current row. Hosts bind `SelectedObject`. They do not build rows by hand unless they opt into `ItemsSource`.

### 1.2 What v1 is not

Not a DataGrid. Not a theme engine. Not a WinForms `UITypeEditor` host. Not a multi-object merge grid. Not a collection editor. Not Visual Studio's Property Window (no property tabs, no document outline).

---

## 2. Architecture

### 2.1 Shape

Lookless `Control` named `VestigiumPropertiesGrid` (replace the current `UserControl` stub). Visual tree in `Themes/Generic.xaml`.

Template parts:

| Part | Role |
|---|---|
| `PART_Search` | Filter box |
| `PART_Toolbar` | Categorized / Alphabetical toggle |
| `PART_List` | Virtualized property rows |
| `PART_Splitter` | Resize name column |
| `PART_Description` | Footer for the selected row |

`PropertyEngine` owns descriptor scan, filter, sort, commit, and INPC coalesce. The control wires parts. Demo ViewModels bind DPs.

### 2.2 Two ways to feed rows

| Mode | When |
|---|---|
| **SelectedObject** (default) | Reflect with `TypeDescriptor` so `Category`, `DisplayName`, `Description`, `Browsable`, `ReadOnly`, `DefaultValue`, and `TypeConverter` work. |
| **ItemsSource** | Host supplies `IList<VestigiumPropertyItem>`. No reflection. Use this for dynamic / dictionary settings. |

If both are set, `ItemsSource` wins. Clearing `ItemsSource` falls back to `SelectedObject`.

### 2.3 Theming is assigned by the host

This assembly SHALL NOT project-reference `Vestigium.Themes*`, call `ThemeManager`, expose a `Theme` property, or ship a palette.

It MAY project-reference `Vestigium.Controls.NumericUpDown` for numeric editors.

`Generic.xaml` fallbacks (same family as the rest of the suite):

| Token | Fallback |
|---|---|
| Background | `#0F2744` |
| Row | `#132744` |
| Text | `#E2E8F0` |
| Muted | `#94A3B8` |
| Hairline | `#1E3A5F` |
| Accent / selected | `#4A90C8` |
| Category header | `#0B1524` |

### 2.4 Encapsulated work

Hosts do not write debounce timers.

- Rebuild the descriptor list when `SelectedObject` / `ItemsSource` identity changes.
- `INotifyPropertyChanged` on the selected object updates **values**, not the row list. Storms coalesce on a 50–250 ms AIMD drain (same idea as StatusBar / NumericUpDown).
- Filter keystrokes filter a cached list. No re-reflect per key.
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
    Expandable = 7
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
| `Value` | Boxed current value |
| `Choices` | Enum names / flags later |
| `Path` | Dotted path for expandable children |

---

## 4. Property matrix

| Property | Type | Default | Notes |
|---|---|---|---|
| `SelectedObject` | `object?` | `null` | Inspect this instance. |
| `ItemsSource` | `IEnumerable?` | `null` | Wins over reflection when set. |
| `Sort` | `VestigiumPropertySort` | **Categorized** | |
| `SearchText` | `string` | `""` | Case-insensitive contains on name, category, description. |
| `ShowSearch` | `bool` | `true` | |
| `ShowToolbar` | `bool` | `true` | |
| `ShowDescription` | `bool` | `true` | Footer. |
| `NameColumnWidth` | `double` | `160` | Splitter writes this. |
| `SelectedProperty` | `VestigiumPropertyItem?` | `null` | Two-way. Drives the footer. |
| `IsReadOnly` | `bool` | `false` | Grid-wide lock. |

No `Theme`. No `SelectedObjects` in v1. No `PropertyTabs`.

Empty `SelectedObject` and empty `ItemsSource` render an empty state: "No object selected."

---

## 5. Discovery rules (SelectedObject mode)

Use `System.ComponentModel.TypeDescriptor`, not raw `GetProperties()`.

| Attribute / fact | Behavior |
|---|---|
| `[Browsable(false)]` | Hidden |
| Indexer / `Item[]` | Hidden |
| `[DisplayName]` | Row label |
| `[Category]` | Group. Missing → `Misc` |
| `[Description]` | Footer |
| `[ReadOnly(true)]` or no public setter | `ReadOnly` editor |
| `[DefaultValue]` | Reserved for later Reset. v1 may show it in the footer only. |
| `TypeConverter` that is standard (enum, color, culture numbers) | Used to parse / format |
| Custom `UITypeEditor` (WinForms) | **Ignored.** Host supplies a DataTemplate or a `VestigiumPropertyItem`. |

Public instance properties only. No fields. No statics.

Changing `SelectedObject` to a different type rebuilds rows. Same type, different instance: rebuild values, keep expand / scroll if names still match.

---

## 6. Editors (v1)

`DataTemplateSelector` on `Kind`. Hosts MAY add templates keyed by type in the control's `EditorTemplates` dictionary later; v1 ships the built-ins.

| Kind | Control | Commit |
|---|---|---|
| Text | `TextBox` | Enter / LostFocus. Escape reverts. |
| Boolean | `CheckBox` | Immediate. |
| Enum | `ComboBox` | Immediate on selection. |
| Numeric | `VestigiumNumericUpDown` | That control's Immediate + Auto defaults. Infer integer vs decimal from the property type. Unsigned for `byte` / `uint` / `ulong`. |
| DateTime | `DatePicker` + optional time `TextBox` | LostFocus. |
| Color | Swatch + hex `TextBox` | LostFocus / Enter. Bad hex reverts. |
| ReadOnly | `TextBlock` | No edit. |
| Expandable | Chevron + nested rows | Child editors as above. **One level** in v1. |

Failed parse never throws. Revert to last good value.

`PropertyValueChanged` routed event fires after a successful commit (`string PropertyName`, `object? OldValue`, `object? NewValue`).

---

## 7. Sort, search, keyboard

**Categorized (default):** category headers in ordinal / alphabetical order. Properties A–Z inside a category. Headers collapse. Remember collapse per category name for the session.

**Alphabetical:** flat list, no headers.

**Search:** live filter. Hides non-matching rows. Category headers hide when no child remains. Clear the box to restore.

**Keys:** Up/Down move rows. Enter begins edit (or toggles bool). Escape cancels edit. Tab moves to the next editor. F2 edit.

---

## 8. Layout and look (Windows 10/11, not WinForms)

- Row height 32 dip. Hairline between rows. No 3D sunken cells.
- Selected row: 3 dip accent bar on the left + fill `#132744`.
- Category header: 28 dip, uppercase 11 px tracking, chevron, no gradient bar.
- Name column left, editor column fills. Splitter between them.
- Search is a single line under the toolbar, placeholder "Filter properties".
- Description footer: 72 dip min, wraps, shows `DisplayName` + description. Empty description still shows the name.
- Segoe UI. No Classic / 3D border. No yellow help pane. No toolbox bitmap strip.

The control stretches to its parent. Virtualize the row list (`VirtualizingStackPanel`).

---

## 9. Public surface

```xml
xmlns:pg="clr-namespace:Vestigium.Controls.PropertiesGrid;assembly=Vestigium.Controls.PropertiesGrid"

<pg:VestigiumPropertiesGrid SelectedObject="{Binding SelectedProbe}"
                            Sort="Categorized"
                            ShowDescription="True"/>
```

```csharp
public class VestigiumPropertiesGrid : Control
{
    public object? SelectedObject { get; set; }
    public VestigiumPropertySort Sort { get; set; }
    public string SearchText { get; set; }
    public VestigiumPropertyItem? SelectedProperty { get; set; }
    public event EventHandler<VestigiumPropertyValueChangedEventArgs>? PropertyValueChanged;
}
```

---

## 10. Demo

`Vestigium.Controls.PropertiesGrid.Demo` SHALL show:

1. **Probe sample** — a fake PingIQ settings object with categories `General`, `Timing`, `Display` (string, int, decimal, bool, enum, Color, DateTime).
2. **Categorized vs Alphabetical** toggle.
3. **Search** that hides rows live.
4. **Read-only** property and grid-wide `IsReadOnly`.
5. **Expandable** nested `Thresholds` object one level deep.
6. **Description footer** updates on row change.
7. **Live object** — a side panel bound to the same instance so edits are visible outside the grid.
8. **Empty state** when `SelectedObject` is null.
9. **ItemsSource** tab with hand-built `VestigiumPropertyItem`s (no reflection).
10. Nested inside a default Vestigium form cell (not only a full window).

The demo does not initialize Vestigium.Themes.

---

## 11. Tests

| ID | Assertion |
|---|---|
| PG-T01 | `[Browsable(false)]` is omitted |
| PG-T02 | Missing category lands in `Misc` |
| PG-T03 | `[DisplayName]` is the row label |
| PG-T04 | No setter → ReadOnly kind |
| PG-T05 | Enum property → Enum kind with names |
| PG-T06 | `int` / `decimal` → Numeric kind |
| PG-T07 | Search "ttl" hides non-matches |
| PG-T08 | Alphabetical has no category headers |
| PG-T09 | Bad hex Color commit reverts |
| PG-T10 | INPC on the object updates the row value |
| PG-T11 | `ItemsSource` wins over `SelectedObject` |
| PG-T12 | Null selected object → empty state |
| PG-T13 | csproj has no `Vestigium.Themes*` and no `System.Windows.Forms` reference |

---

## 12. Acceptance

1. Demo §10 items 1–10 work on Generic fallbacks.
2. Looks like a Windows 11 settings inspector, not a 1990s PropertyGrid.
3. Typing in Search does not hitch on a 50-property object.
4. Holding a nested NumericUpDown does not lock the window (NUD contract).
5. Listed tests pass.
6. No Themes or WinForms project reference.

---

## 13. Later (not v1)

- `SelectedObjects` multi-select with mixed-value display
- Collection / list editor
- Reset to `[DefaultValue]`
- Host `EditorTemplates` dictionary by `Type`
- More than one expand level
- Flags enum checkboxes
- Password / masked text
- Property pages / tabs
- Drag-drop property order
