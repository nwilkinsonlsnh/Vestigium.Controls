# Vestigium.Controls.PropertiesGrid — Developers Guide

**Document ID:** VEST-CTL-PG-DEV-001  
**Version:** 1.4  
**Status:** Implemented  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.4.md`](Requirements_v1.4.md)

v1.3 editor alignment and category images still apply. This page covers grouping headers.

## Grouping headers

Separate from editors. Bold and left by default.

```xml
<pg:VestigiumPropertiesGrid SelectedObject="{Binding Selected, Mode=OneWay}"
                            EditorTextAlignment="Left"
                            CategoryTextAlignment="Left"
                            IsCategoryBold="True"
                            CategoryOrientation="Horizontal"
                            CategoryIcons="{Binding CategoryIcons}"/>
```

Center the TIMING / GENERAL labels without moving TTL:

```xml
CategoryTextAlignment="Center"
EditorTextAlignment="Left"
```

Unbold:

```xml
IsCategoryBold="False"
```

Stack chevron, icon, and name:

```xml
CategoryOrientation="Vertical"
```

`CategoryOrientation` is `System.Windows.Controls.Orientation` (Horizontal / Vertical), not a text rotation.

## Do not

- Bind grouping alignment to `EditorTextAlignment`. They are two DPs on purpose.
- Expect grouping chrome in Alphabetical sort. There are no headers.
