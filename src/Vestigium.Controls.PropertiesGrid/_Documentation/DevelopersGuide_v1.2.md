# Vestigium.Controls.PropertiesGrid — Developers Guide

**Document ID:** VEST-CTL-PG-DEV-001  
**Version:** 1.2  
**Status:** Implemented  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.2.md`](Requirements_v1.2.md)

## Project

| Item | Value |
|---|---|
| Assembly | `Vestigium.Controls.PropertiesGrid` |
| Type | `VestigiumPropertiesGrid` (lookless `Control`; the v1.0 `UserControl` stub is replaced at build) |
| Engine | `PropertyEngine` |
| Demo | `src/Vestigium.Controls.PropertiesGrid.Demo` |

## Host one object

```xml
<pg:VestigiumPropertiesGrid SelectedObject="{Binding SelectedProbe}"/>
```

Annotate the model with `System.ComponentModel` attributes.

```csharp
[Category("Timing"), DisplayName("TTL"), Description("Hop limit 1–255."), DefaultValue(64)]
public int Ttl { get => field; set => SetProperty(ref field, value); }
```

## Host many objects

```xml
<pg:VestigiumPropertiesGrid SelectedObjects="{Binding SelectedProbes}"/>
```

Only properties that exist on **every** selected instance appear. Different values show as mixed. One commit writes all targets.

Collection rows do not expand while more than one object is selected.

## Collections

Expose `ObservableCollection<T>` (or any `IList`). The row shows `Count`, Add / Remove / Up / Down. Children are `[0]`, `[1]`, …

`T` needs a public parameterless constructor for Add. Arrays are fixed-size: items edit, Add is off.

`IDictionary` is not an editor in v1 — flatten through `ItemsSource` if you need it.

## Nested objects

A class/struct property expands. Children load on first open. Cycles render `(circular)`. Cap is `MaxExpandDepth` (default 8). Null nested objects show `(none)` and do not auto-construct.

## Reset

`[DefaultValue]` enables Reset on that row and Reset all on the toolbar. No attribute means no reset — the grid will not invent `default(T)`.

## Dynamic rows

Bind `ItemsSource` to `IList<VestigiumPropertyItem>` and leave selected objects empty.

## Do not

- Reference `System.Windows.Forms` or host a WinForms `PropertyGrid`.
- Reference `Vestigium.Themes*`.
- Expect `UITypeEditor` dialogs or a host `EditorTemplates` map. That is later.
- Expand two collections at once in multi-select. The control refuses.
- Rebuild the row list yourself on every `PropertyChanged`. The engine coalesces value updates.
