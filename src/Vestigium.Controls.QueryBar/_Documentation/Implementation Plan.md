# Vestigium.Controls.QueryBar — Implementation Plan

**Document ID:** VEST-CTL-QRY-PLN-001
**Version:** 1.0
**Status:** Step 5 done
**Date:** 4 October 2026

## Step 1 — Project

Done. `src/Vestigium.Controls.QueryBar` is in the solution. The control is a shell. `AssemblyInfo` points the default style at this assembly. These three documents are the contract.

## Step 2 — Template

Done. Generic.xaml has the text box, the clear mark, and the chevron. Fallback brushes cover a host that has not loaded a palette. No popup yet.

## Step 3 — Text and clear

Done. Text binds both ways. Clear empties Text, raises Cleared, and leaves the caret at the end.

## Step 4 — Completion

Done. Complete runs after CompletionDelay. Tab accepts and leaves the caret at the end. Up and Down move the highlight. Escape closes. Enter does not accept. A trailing space is added unless the insert ends with `(`.

## Step 5 — Saved queries

Done. The chevron opens the host list under the bar and keeps it inside the window. A row writes the text, puts the caret at the end, and does not open completion. CloseOnApply closes the list. Pin raises PinRequested and leaves the list open. Limit 0 hides the chevron and does not remove rows.

## Step 6 — Tests

Template loads. Clear empties. Accept places the caret at the end. Saved apply does not open completion. Limit 0 hides the chevron and does not remove rows.

## Step 7 — RouteIQ

Replace the three bars with VestigiumQueryBar. Delete the copied popups. Keep the sessions, the store, and the grid filter pause.

## Step 8 — Package

Pack 1.0.0 only after RouteIQ runs on the control. Do not publish the shell.
