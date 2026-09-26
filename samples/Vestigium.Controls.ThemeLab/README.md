# Controls Theme Lab

Small WPF host for Vestigium control chrome. Do not pack this project.

## Run

```powershell
dotnet run --project D:\Source\Clone\Vestigium.Controls\samples\Vestigium.Controls.ThemeLab\Vestigium.Controls.ThemeLab.csproj
```

Or open `Vestigium.Controls.slnx`, set **Vestigium.Controls.ThemeLab** as startup, F5.

## What to check

Walk every theme. The NumericUpDown spinner column must use `Control.Fill` / `Text.Primary`, not the stock Windows RepeatButton chrome. Add more specimens here before packing a control.
