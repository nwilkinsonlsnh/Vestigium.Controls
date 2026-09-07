# Vestigium.Controls.PropertiesGrid — Software Requirements Specification

**Document ID:** VEST-CTL-PG-SRS-001  
**Version:** 1.4  
**Status:** Implemented  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS, Visual Studio 2026, WPF MVVM  
**Supersedes:** v1.3

v1.4 adds **grouping header presentation**: orientation, alignment, and bold. Editor alignment from v1.3 is unchanged.

---

## Grouping headers

Category rows are independent of editor cells. Changing `EditorTextAlignment` SHALL NOT move TIMING / GENERAL labels.

| Property | Type | Default | Notes |
|---|---|---|---|
| `CategoryTextAlignment` | `TextAlignment` | **Left** | Left / Center / Right of the header cluster (chevron + icon + name). Spans the full row. `Justify` coerces to Left. |
| `IsCategoryBold` | `bool` | **true** | Bold on. Host may unbold. |
| `CategoryOrientation` | `Orientation` | **Horizontal** | Horizontal = chevron, icon, name in one row. Vertical = stacked, row grows (`MinHeight` 32). |

Alphabetical sort has no headers, so these DPs have no visible effect until Categorized is on.

Live: DPs bind in the template from the grid (AncestorType). No rebuild required.

---

## v1.3 still applies

`EditorTextAlignment` (default Left), `[VestigiumTextAlignment]`, `CategoryIcons`, NumericUpDown standalone Right.

---

## Tests added in v1.4

| ID | Assertion |
|---|---|
| PG-T32 | Defaults: category Left, Bold true, Horizontal |
| PG-T33 | Category Center + Vertical + unbold does not change EditorTextAlignment |

---

## Demo

Probe tab: Grouping Left / Center / Right, Horizontal / Vertical, Bold grouping text checkbox (on).
