# Themes and tokens

## What a theme is

A theme is a `Tokens` record: 12 colours, a name, and two corner radii. Everything the library draws comes from these values, so changing a theme means changing 12 colours.

| Token | Used for |
|---|---|
| `Canvas` | Window background |
| `Surface` | Inputs, lists, cards, tab pages |
| `SurfaceAlt` | Hover and subtle fills, list headers |
| `Border` | Hairlines and dividers |
| `BorderStrong` | Outlines that need to be seen (buttons, check boxes), grips |
| `Text` | Body text |
| `TextMuted` | Captions, secondary text |
| `Accent` | The one highlight colour: selection, primary button, focus, links |
| `AccentHover` | Accent when hovered |
| `AccentPressed` | Accent when pressed |
| `OnAccent` | Text drawn on top of the accent |
| `Disabled` | Disabled text and glyphs |
| `Name` | Shown in the theme menu and saved as the user's choice |
| `Radius` | Corner radius of buttons and inputs |
| `CardRadius` | Corner radius of cards and group boxes |

## Ready-made themes

`ModernTheme.All` lists them in this order. The `Name` is what is saved.

| Field | Name | Style |
|---|---|---|
| `FluentLight` (alias `Light`) | Fluent Light | Windows 11 light |
| `FluentDark` (alias `Dark`) | Fluent Dark | Windows 11 dark |
| `MaterialLight` | Material Light | Small corners, large cards |
| `MaterialDark` | Material Dark | |
| `SolarizedLight` | Solarized Light | Tight corners |
| `SolarizedDark` | Solarized Dark | |
| `Nord` | Nord | Cool dark |
| `Dracula` | Dracula | Purple dark |
| `NeonSynthwave` | Neon Synthwave | Dark with a pink accent |
| `Bezier` | Bezier | Cool grey, tight corners, steel blue |

Screenshots are in [`screenshots/`](screenshots/).

## Make your own theme

`ModernTheme.T(...)` is private, so build the record directly. Copy an existing theme and change the colours:

```csharp
using System.Drawing;
using Fungusware.WinFormsThemes;

public static class MyThemes
{
    public static readonly Tokens Forest = new(
        Canvas:        Color.FromArgb(0x10, 0x18, 0x14),
        Surface:       Color.FromArgb(0x17, 0x22, 0x1C),
        SurfaceAlt:    Color.FromArgb(0x20, 0x2E, 0x26),
        Border:        Color.FromArgb(0x2C, 0x3D, 0x33),
        BorderStrong:  Color.FromArgb(0x5C, 0x7A, 0x69),
        Text:          Color.FromArgb(0xE8, 0xF1, 0xEB),
        TextMuted:     Color.FromArgb(0x9F, 0xB8, 0xA8),
        Accent:        Color.FromArgb(0x4C, 0xC3, 0x7F),
        AccentHover:   Color.FromArgb(0x6B, 0xD6, 0x99),
        AccentPressed: Color.FromArgb(0x38, 0xA8, 0x66),
        OnAccent:      Color.FromArgb(0x07, 0x14, 0x0D),
        Disabled:      Color.FromArgb(0x57, 0x6B, 0x5E),
        Name: "Forest", Radius: 6f, CardRadius: 10f);
}

ThemeApplier.SetTheme(MyThemes.Forest);
```

A custom theme is not in `ModernTheme.All`, so it does not appear in `AddThemeMenu` and `UseSavedTheme` will not find it by name. To list your own themes, add them to your own menu and call `SetTheme`.

Tips for choosing colours:

- Whether a theme is **dark** is decided from `Canvas` (`ModernTheme.IsDark`). That drives the title bar and scroll bars, so keep `Canvas` clearly dark or clearly light.
- `OnAccent` must be readable on `Accent`. It is used on primary buttons and selected check marks.
- `BorderStrong` should be visible against `Surface`. Light themes use a mid grey, dark themes a lighter grey.
- `Disabled` should be dimmer than `TextMuted` but still visible.

## Derived colours

Some colours are computed from the tokens so that a theme needs only 12:

| Helper | Meaning |
|---|---|
| `ModernTheme.Chrome(t)` | Menu bars, toolbars and the status bar: a step darker than the form so they read as chrome |
| `ModernTheme.Blend(a, b, amountOfA)` | Mix two colours (0 = all `b`, 1 = all `a`) |
| Selection colour | `Blend(Accent, Surface, 0.18)` focused, `0.10` unfocused |
| Hover and pressed (menus, toolbars) | `Blend(Accent, Surface, 0.16)` and `0.28` |
| Hint text | `Blend(Text, Canvas, 0.72)` |

## Status colours

`StatusColors` gives state colours that stay readable on light and dark themes. Use them instead of `Color.Red` and friends.

| Member | Use |
|---|---|
| `Success`, `Warning`, `Error`, `Fatal` | Text or icon colour for a state |
| `RunningRow` | Background for a row that is in progress |
| `Chip(kind)` | A `(Back, Fore)` pair for a small tinted label. `kind` is `"info"`, `"success"`, `"warning"`, `"error"`, anything else is neutral |
| `IsDark` | Whether the active theme is dark |

## Themed controls

| Control | Use |
|---|---|
| `CardPanel` | A rounded surface with a hairline border. Its `Tokens` property sets the look |
| `PillProgress` | A thin rounded progress bar. `Value` is 0 to 100 |
| `ModernTabControl` | A `TabControl` that paints its own tab strip so no native chrome shows |

`CardPanel` and `PillProgress` read their own `Tokens` property (default `FluentLight`). Set it to `ThemeApplier.Current` when you switch theme. `ModernTabControl` reads `ThemeApplier.Current` directly.
