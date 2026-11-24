# Migrating from KryptonModern

The project was KryptonModern, a set of files you copied, and `KryptonModern.Editor`, a package. It is now `Fungusware.WinFormsThemes`, split into a core and adapters. The GitHub repository was renamed to `Fungusware/Fungusware.WinFormsThemes`; the old address redirects.

## Why it changed

The first version put the Krypton palette at the centre and bolted the rest on. The theme should be the master: one set of colours and one engine, with Krypton, ObjectListView and the editor following it. See [how-it-works.md](how-it-works.md).

## Names

| Before | Now |
|---|---|
| Copy `ModernTheme.cs`, `ThemeApplier.cs`, `ModernControls.cs` into your project | `dotnet add package Fungusware.WinFormsThemes` |
| `namespace KryptonModern` | `namespace Fungusware.WinFormsThemes` |
| `ModernTheme.Light` / `ModernTheme.Dark` | `ModernTheme.FluentLight` / `FluentDark` (the old names still work) |
| `ModernTheme.Build(tokens)` | `KryptonTheme.Build(tokens)` (package `Fungusware.WinFormsThemes.Krypton`) |
| Create a `KryptonManager`, assign the palette, call `ThemeApplier.Apply(this, t)` in every form | `KryptonTheme.Register(); ThemeApplier.Install(theme);` once |
| `ThemeApplier.Apply(form, tokens)` | Still available (advanced use); normally not needed |
| NuGet `KryptonModern.Editor` | NuGet `Fungusware.WinFormsThemes.Editor` |
| `using KryptonModern.Editor;` | `using Fungusware.WinFormsThemes.Editor;` |
| `ModernTheme.Suite.cs` (Navigator, Ribbon, Docking entries) | Part of `Fungusware.WinFormsThemes.Krypton` |

## Behaviour that changed

- **Font:** the palette font is 9 pt Segoe UI (was 9.5 pt Segoe UI Variable). Set `KryptonTheme.FontSize = 9.5f` before `Install` for the old size.
- **ProgressBar and DateTimePicker:** plain ones are restyled in place instead of swapped for themed controls.
- **Theme names:** saved names are the theme's display name ("Fluent Light"), unchanged.

## From the Xackup-style `Xackup.Theme`

`Xackup.Theme` was an early copy of this code that grew a theme engine. That engine is now the core. Mapping:

| `Xackup.Theme` | Now |
|---|---|
| `Xackup.Theme.ThemeApplier`, `ModernTheme`, `Tokens`, `StatusColors`, `ModernRenderer` | The same names in `Fungusware.WinFormsThemes` |
| `ThemeApplier.Install()` (also created the Krypton manager) | `KryptonTheme.Register()` then `ThemeApplier.Install()` |
| `ThemeApplier.StyleList(olv)` and the built-in ObjectListView styling | `ObjectListViewTheme.Register()`, `ObjectListViewTheme.StyleList(olv)` |
| Built-in refresh of ObjectListView rows on a switch | Same, registered by `ObjectListViewTheme.Register()` |
