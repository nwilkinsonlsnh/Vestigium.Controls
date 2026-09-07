# Vestigium.Controls.UnderConstruction — Software Requirements Specification

**Document ID:** VEST-CTL-UC-SRS-001  
**Version:** 1.0  
**Status:** Skeleton  
**Date:** 6 September 2026

## 1. Objective

A reusable "this surface is not built yet" view. The default Vestigium form uses it for unfinished menu destinations so hosts do not ship empty click targets.

## 2. v1 surface

`UnderConstructionView` is a `UserControl` with:

- A heading (`Title`, default `"Under Construction"`).
- Optional `Detail` text.
- No commands.

The default form may either disable unfinished menu items **or** navigate the content host to this view. Skeleton uses disabled menu items plus this view as the empty client placeholder.

## 3. Non-goals

- Animated machinery illustrations
- Linking to issue trackers
