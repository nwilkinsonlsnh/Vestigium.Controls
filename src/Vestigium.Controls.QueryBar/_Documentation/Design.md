# Vestigium.Controls.QueryBar — Design

**Document ID:** VEST-CTL-QRY-DES-001
**Version:** 1.0
**Status:** Draft
**Date:** 4 October 2026

## 1. Place

`src/Vestigium.Controls.QueryBar` in Vestigium.Controls. Own package. `net10.0-windows`, `UseWPF`. Namespace `Vestigium.Controls.QueryBar`.

It references CommunityToolkit.Mvvm and Vestigium.Helpers.Kql. It does not reference Vestigium.Themes.

## 2. Type

`VestigiumQueryBar` is a lookless control. The template is in `Themes/Generic.xaml`.

The template holds the text box, the clear button, the chevron, the completion popup, and the saved-query popup. The popups are part of the control so a host does not re-anchor them.

## 3. Properties

| Property | Role |
|---|---|
| Text | The query. Two-way. |
| Session | The KqlSession used by Complete. |
| Queries | The saved rows for this bar. |
| Limit | Rolling size. 0 hides the chevron. |
| CloseOnApply | Close the saved list after a text selection. Default true. |
| CompletionDelay | Pause before the list opens. Default 140 ms. |

## 4. Events

| Event | When |
|---|---|
| Committed | Enter, or focus leaving the box. |
| Cleared | Clear button. |
| PinRequested | Pin clicked. Argument is the row. |
| RemoveRequested | Host may ignore. Not on the bar. |
| TextChanged | After the box text changes. The host may debounce its grid filter. |

## 5. Completion

The control calls `KqlHelper.Complete(text, caret, session, hints)`. Hints are the saved texts, used only to break equal-prefix ties, as KQL already does.

The list opens under the caret. It uses the window surface and the card highlight. It does not use the stock list chrome.

Accept replaces `ReplaceStart` for `ReplaceLength` with `Insert`. If the insert does not end with `(`, the control adds a trailing space. The caret is set after the binding writes the text back. A suppress flag stops the write-back from opening the list at position 0.

A saved-query apply uses the same caret placement and does not call Complete.

## 6. Saved-query popup

Placement target is the bar, not the chevron. A custom placement callback keeps the list inside the window. Width matches the bar. Text wraps.

The pin glyph is Segoe MDL2 `E718`. A sticky row uses `E841`.

## 7. What RouteIQ keeps

RouteIQ keeps the three sessions, the grid filters, the settings store, and the sticky flag. The control does not write ProgramData.

The grid filter stays on a short pause in the host. This control does not refresh a grid.

## 8. Logging

The control does not emit event ids. KQL already logs Complete and Compile. A missing session is a no-op, not a throw.
