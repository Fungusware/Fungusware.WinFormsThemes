# Getting started

Pick the case that matches your app. Each one is complete.

Requirements: Windows, .NET 8 (`net8.0-windows`) or newer. Read [how-it-works.md](how-it-works.md) for the ideas behind the calls below.

## 1. Plain WinForms app (no Krypton)

```
dotnet add package Fungusware.WinFormsThemes
```

```csharp
using Fungusware.WinFormsThemes;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        ThemeApplier.Install(ModernTheme.FluentDark);   // once, before the first form
        Application.Run(new MainForm());                // forms are themed as they open
    }
}
```

That is all. Buttons, lists, tabs, menus, toolbars, group boxes and the title bar now follow the theme.

## 2. App built on Krypton Toolkit

```
dotnet add package Fungusware.WinFormsThemes
dotnet add package Fungusware.WinFormsThemes.Krypton
```

```csharp
using Fungusware.WinFormsThemes;

static void Main()
{
    ApplicationConfiguration.Initialize();
    KryptonTheme.Register();                         // Krypton follows the theme
    ThemeApplier.Install(ModernTheme.NeonSynthwave); // builds the palette and starts theming
    Application.Run(new MainForm());                 // MainForm : KryptonForm
}
```

Do not create a `KryptonManager` yourself and do not assign a palette: the adapter does both. Use `KryptonForm` for the window, otherwise you keep the standard Windows title bar.

Optional Krypton packages (Navigator, Ribbon, Docking, Workspace) are brought in by the Krypton package. Their palette entries are included.

## 3. App that also uses ObjectListView

```
dotnet add package Fungusware.WinFormsThemes.ObjectListView
```

```csharp
KryptonTheme.Register();
ObjectListViewTheme.Register();                  // before Install
ThemeApplier.Install(ModernTheme.FluentLight);
```

Every `ObjectListView` is now styled: flat bold header, accent-tinted selection, themed empty-list message, and the last column fills the free space. For a list that is built before the theme is installed, call `ObjectListViewTheme.StyleList(olv)`.

If your list colours rows (a `RowFormatter` or `FormatRow` handler), read colours from `StatusColors` or `ThemeApplier.Current`. The package rebuilds the rows after a theme switch, so they pick up the new colours.

## 4. Let the user pick a theme

**A menu:** `ThemeApplier.AddThemeMenu(menuItem)` adds a "Theme" submenu with one checked entry per theme.

**In code:**

```csharp
ThemeApplier.SetTheme(ModernTheme.Dracula);          // switches every open form and every adapter
```

**Remember the choice.** The library does not choose where to store it. Plug your storage into two hooks, then apply the saved theme once settings can be read:

```csharp
ThemeApplier.ReadSavedThemeName  = () => Settings.Default.Theme;
ThemeApplier.WriteSavedThemeName = name => { Settings.Default.Theme = name; Settings.Default.Save(); };

ThemeApplier.Install();           // default theme first
ThemeApplier.UseSavedTheme();     // then the saved one, if there is one
```

`SetTheme` calls `WriteSavedThemeName` for you. `UseSavedTheme` does not write it back.

**React to a switch** (to repaint something of your own):

```csharp
ThemeApplier.ThemeChanged += (_, _) => myControl.Invalidate();
```

## 5. Use the colours in your own drawing

Never hard-code colours in custom painting. Read them from the active theme:

```csharp
var t = ThemeApplier.Current;
g.Clear(t.Surface);
TextRenderer.DrawText(g, text, font, bounds, t.Text);
```

For states (success, warning, error) use `StatusColors`, which has a light and a dark variant:

```csharp
label.ForeColor = StatusColors.Success;
var (back, fore) = StatusColors.Chip("warning");   // "info", "success", "warning", "error"
```

## 6. Style a custom control

```csharp
ThemeApplier.RegisterStyler<MyGauge>((gauge, t) =>
{
    gauge.BackColor = t.Surface;
    gauge.NeedleColor = t.Accent;
});
```

It runs when a `MyGauge` is first seen, and again on every theme change. To rebuild data-driven colours after a switch, add `ThemeApplier.RegisterRefresh<MyGauge>(g => g.Rebuild());`.

For a whole control family (a third-party suite), write an adapter: [adapters.md](adapters.md).

## 7. Manage the Krypton palette yourself (advanced)

If you must own the `KryptonManager` (for example a tool that switches theme per document), skip `Install` and do it by hand:

```csharp
var palette = KryptonTheme.Build(tokens);
manager.GlobalPaletteMode = PaletteMode.Custom;
manager.GlobalCustomPalette = palette;
ThemeApplier.Apply(form, tokens);      // themes the plain controls on this form
```

Call `KryptonTheme.Register()` once so Krypton controls are recognised. The demo app's `DemoForm` does exactly this. Call `ThemeApplier.Apply(form, tokens)` after the form is shown (the `Shown` event), because a `KryptonForm`'s child controls are not reachable in the constructor.

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| A form is partly light | The adapter for that control family is not registered. Register it before `Install`. |
| Title bar is not themed | Only Windows 11 lets an app colour the title bar. On older Windows the standard caption stays (a `KryptonForm` draws its own). |
| Scroll bars or context menus stay light | They use undocumented Windows calls (Windows 10 1903+). On older Windows they stay light. |
| Row colours in a list do not change on a switch | Colours are chosen when rows are built. Register a refresher (`RegisterRefresh`) or use the ObjectListView package. |
| A custom control ignores the theme | Register a styler for it, or read `ThemeApplier.Current` when you paint. |
| `SetTheme` seems to do nothing | The theme you passed is already active, or you passed `null`. |
| Controls look wrong when added at run time | They are picked up automatically through `ControlAdded`. If they are created off-screen first, call `ThemeApplier.Apply(control)`. |
