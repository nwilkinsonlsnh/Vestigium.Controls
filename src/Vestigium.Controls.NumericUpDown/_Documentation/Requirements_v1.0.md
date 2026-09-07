# Vestigium.Controls.NumericUpDown — Software Requirements Specification

**Document ID:** VEST-CTL-NUD-SRS-001  
**Version:** 1.0  
**Status:** Placeholder — not scheduled  
**Date:** 6 September 2026

## 1. Objective

A WPF numeric spinner (decimal and integer) with MVVM bindings for `Value`, `Minimum`, `Maximum`, `Increment`, and `DecimalPlaces`.

## 2. Notes for the later planning pass

- Must work without mouse (Up/Down keys, page increment).
- Must coerce rather than throw when a binding pushes an out-of-range value.
- Theme tokens from Vestigium.Themes; fallback brushes in `Themes/Generic.xaml`.
- Demo project already exists as a stub host.

Do not implement beyond the compileable stub until a v1.1 of this document is accepted.
