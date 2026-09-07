# Vestigium.Controls.UnderConstruction — Developers Guide

**Document ID:** VEST-CTL-UC-DEV-001  
**Version:** 1.4  
**Status:** Current  
**Date:** 7 September 2026  
**Contract:** [`Requirements_v1.4.md`](Requirements_v1.4.md)

## Project

| Item | Value |
|---|---|
| Assembly | `Vestigium.Controls.UnderConstruction` |
| Type | `VestigiumUnderConstruction` |
| Rules | `UnderConstructionRules` |
| Demo | `src/Vestigium.Controls.UnderConstruction.Demo` |

`Header` and `Message` from v1.1 are gone. Use `Title`, `Subject`, and `Description`.

v1.4 is the Description memo pass. Title and Subject stay one line. Description keeps Enter as a new paragraph.

## Host a page

```xml
<uc:VestigiumUnderConstruction
    Title="Vestigium"
    Subject="Default form client area"
    Description="Feature views land here after their requirements are accepted.&#10;&#10;Use this paragraph for schedule, owner, or a short note to the user."/>
```

The content column grows with the window up to 720 dip. Do not wrap the control in a 420-wide panel unless you want a narrow column.

In C# prefer `\n` in the string. In XAML, `&#10;` is a newline.

## Host an overlay

```xml
<Grid>
  <local:LiveTile/>
  <uc:VestigiumUnderConstruction
      Title="Reports"
      Subject="Module in development"
      Description="Scheduled for Q3.&#10;&#10;Notify to get a mail when this cell ships."
      IsActive="{Binding ReportsUnreleased}"
      Command="{Binding NotifyMeCommand}"/>
</Grid>
```

## Image

| What you have | Property |
|---|---|
| Built-in cone + barrier | Leave `ImageSource` and `IconData` null |
| PNG / JPEG | `ImageSource` or `ImageUri` (file, pack, or http) |
| Path geometry | `IconData` (`VestigiumUnderConstructionGlyphs.CreateCone()` / `CreateBarrier()`) |
| SVG file | Prefer rasterize to PNG for WPF. `ImageUri` to an `.svg` is accepted; BitmapImage may not decode it. |

Image wins over geometry. Geometry wins over built-in art.

## Description editor (host)

The control displays newlines. It is not the editor. Bind a real memo box:

```xml
<TextBox Text="{Binding Description, UpdateSourceTrigger=PropertyChanged}"
         AcceptsReturn="True"
         AcceptsTab="False"
         TextWrapping="Wrap"
         VerticalScrollBarVisibility="Auto"
         HorizontalScrollBarVisibility="Disabled"
         VerticalContentAlignment="Top"
         MinHeight="200"
         Height="220"
         MaxHeight="400"
         Padding="10,8"
         SpellCheck.IsEnabled="True"
         MaxLength="{Binding DescriptionMaxLength}"/>
```

Do **not** `Trim()` Description on every keystroke. A trailing Enter is how the user starts the next paragraph. Use:

```csharp
partial void OnDescriptionChanged(string value)
{
    var limited = UnderConstructionRules.Limit(value, DescriptionMaxLength, multiline: true);
    if (limited != value)
        Description = limited;
}
```

`TextBox.MaxLength = 0` is unlimited, same as the control.

Title and Subject stay single-line (`UnderConstructionRules.Limit` without `multiline: true`). A pasted newline becomes a space.

## Limits

Defaults: Title **75**, Subject **125**, Description **unlimited**.

```xml
TitleMaxLength="40"
SubjectMaxLength="80"
DescriptionMaxLength="400"
```

`0` turns the cap off. Negative values snap back to the default (75 / 125 / 0). Description max counts `\n`.

## Do not

- Call `ThemeManager` from this assembly.
- Put the words “UNDER CONSTRUCTION” in the bitmap — Title already says it.
- Ship a checkerboard background in a PNG. Export transparency on a real alpha channel.
- Rely on the control coerce alone when the user types into a ViewModel-bound TextBox.
- `Trim()` Description while the caret is live — that deletes Enter at the end of the memo.
