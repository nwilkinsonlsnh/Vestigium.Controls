# Vestigium.Controls.PropertiesGrid — Software Requirements Specification

**Document ID:** VEST-CTL-PG-SRS-001  
**Version:** 1.0  
**Status:** Placeholder — not scheduled  
**Date:** 6 September 2026

## 1. Objective

A WPF property inspector that reflects a selected object's public properties into a two-column grid (name / editor), grouped by category when `CategoryAttribute` is present.

## 2. Notes for the later planning pass

- MVVM: bind `SelectedObject`. Editors write back through the property descriptor.
- Read-only properties render as text, not editors.
- Must not take a dependency on WinForms `PropertyGrid`.
- Demo project already exists as a stub host.

Do not implement beyond the compileable stub until a v1.1 of this document is accepted.
