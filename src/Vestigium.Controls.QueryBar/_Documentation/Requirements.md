# Vestigium.Controls.QueryBar — Requirements

**Document ID:** VEST-CTL-QRY-SRS-001
**Version:** 1.0
**Status:** Draft
**Date:** 4 October 2026
**Package:** Vestigium.Controls.QueryBar 1.0.0
**Depends on:** Vestigium.Helpers.Kql 1.0.2

## 1. Objective

One WPF bar for a KQL session. RouteIQ, and later the other IQ tools, stop copying the text box, the clear mark, the saved-query list, and the completion list.

The language stays in Vestigium.Helpers.Kql. This control is the chrome.

## 2. In scope

- A text box, a clear button, and a saved-query chevron.
- A completion list under the caret.
- A saved-query list under the bar.
- Tab accepts the highlighted row and leaves the caret at the end of the inserted text.
- Up and Down move the highlight. Escape closes the list. Enter commits the text and does not accept a suggestion.
- A saved query writes the text, puts the caret at the end, and does not open completion.
- Clear empties the text and raises Cleared.
- Typing is immediate. The completion list waits a short pause.
- The host supplies the session, the saved rows, the limit, and whether a selection closes the list.
- Size 0 hides the chevron and does not delete saved rows.
- Pin is a click. The host stores sticky.

## 3. Out of scope

- Parsing, ranking, and field catalogs. Those stay in Vestigium.Helpers.Kql.
- The grid filter. The host compiles and refreshes its own grid.
- settings.json, the Settings page, and the per-tab store.
- A private palette. The control uses Vestigium brush tokens and ships fallback hex in Generic.xaml.
- A reference to Vestigium.Themes or Vestigium.Themes.Controls.

## 4. Host contract

The control does not construct a KqlSession. The host passes one.

The control does not persist queries. The host passes the rows for the current tab and handles Committed, PinRequested, and RemoveRequested.

RouteIQ remains the first consumer. The three copied bars are replaced by this control when the implementation plan is done.

## 5. Keys

| Key | Completion list open | Completion list closed |
|---|---|---|
| Tab | Accept highlighted row. Caret at end. | Insert a tab, or do nothing if the host marks Tab handled. |
| Up / Down | Move highlight. | Move the caret. |
| Escape | Close the list. | Do nothing. |
| Enter | Commit the text. Do not accept the row. | Commit the text. |

## 6. Saved queries

A row shows the text and a pin. Selecting the text applies it. Selecting the pin raises PinRequested and does not apply the text and does not close the list.

CloseOnApply defaults to true. The host can set it false.

Limit 0 hides the chevron. The control does not remove rows.

## 7. Theme

Surface, text, and stroke come from Vestigium.Brushes.Surface.Window, Vestigium.Brushes.Surface.Card, Vestigium.Brushes.Text.Primary, and Vestigium.Brushes.Stroke.Subtle. The clear mark stays red. Generic.xaml has a dark fallback so the bar is readable before a host theme loads.

## 8. Non-goals

- Machine learning for suggestions.
- Completing inside an unclosed constructor.
- Sharing one saved-query list across tabs. The host filters.
