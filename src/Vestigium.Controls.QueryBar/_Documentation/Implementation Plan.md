# Vestigium.Controls.QueryBar — Implementation Plan

**Document ID:** VEST-CTL-QRY-PLN-001
**Version:** 1.0
**Status:** Step 1 done
**Date:** 4 October 2026

## Step 1 — Project

Done. `src/Vestigium.Controls.QueryBar` is in the solution. The control is a shell. `AssemblyInfo` points the default style at this assembly. These three documents are the contract.

## Step 2 — Template

Build the bar in Generic.xaml. Text box, clear, chevron. Fallback brushes. No popup yet.

## Step 3 — Text and clear

Two-way Text. Clear raises Cleared and empties Text. Caret stays at the end after clear.

## Step 4 — Completion

Call KqlHelper.Complete after CompletionDelay. Tab, Up, Down, Escape. Accept writes Insert, adds a trailing space unless the insert ends with `(`, and sets the caret at the end after the binding write-back. Enter does not accept.

## Step 5 — Saved queries

Chevron opens the host list. Placement stays inside the window. Selecting a row writes the text, puts the caret at the end, and does not open completion. CloseOnApply unchecks the chevron. Pin raises PinRequested and leaves the list open. Limit 0 hides the chevron.

## Step 6 — Tests

Template loads. Clear empties. Accept places the caret at the end. Saved apply does not open completion. Limit 0 hides the chevron and does not remove rows.

## Step 7 — RouteIQ

Replace the three bars with VestigiumQueryBar. Delete the copied popups. Keep the sessions, the store, and the grid filter pause.

## Step 8 — Package

Pack 1.0.0 only after RouteIQ runs on the control. Do not publish the shell.
