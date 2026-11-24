# Adapters

An adapter carries the active theme to one family of controls. The core engine drives it; the adapter never decides the theme.

Read [how-it-works.md](how-it-works.md) first.

## Which mechanism do I use?

| You want to... | Use |
|---|---|
| Style one control type | `ThemeApplier.RegisterStyler<T>` |
| Rebuild data-driven colours after a switch | `ThemeApplier.RegisterRefresh<T>` |
| Support a whole library (its own palette, manager, many control types) | A `ThemeAdapter`, registered with `ThemeApplier.RegisterAdapter` |

## The adapters we ship

### Krypton: `Fungusware.WinFormsThemes.Krypton`

```csharp
KryptonTheme.Register();                       // once, before ThemeApplier.Install
```

| Piece | What it does |
|---|---|
| `KryptonTheme.Register()` | Registers the adapter with the engine |
| `KryptonTheme.Build(tokens)` | Turns a theme into a `KryptonCustomPaletteBase` (usable on its own) |
| `KryptonTheme.FontSize` | Base font size for the palette, 9 pt Segoe UI by default. Set before `Install` |
| Adapter `Install` | Creates the `KryptonManager`, builds the palette, sets it as the global palette |
| Adapter `ThemeChanged` | Rebuilds the palette for the new theme |
| Adapter `Owns` | Any control in the `Krypton.Toolkit` namespace |
| Adapter `Style` | Inputs stop being permanently "active", bottom bars get the chrome tone and a top hairline, link labels get the accent |
| Adapter `WalkInto` | Yes for `KryptonPanel`, `KryptonForm`, `KryptonSplitContainer`, `KryptonSplitterPanel`, `KryptonGroupBox`, `KryptonGroup`; no for everything else |

Palette roles (which Krypton style is used for what, for example `ButtonStyle.Custom1` is the primary button) are listed in the main [README](../README.md#control-styles).

Source files: `KryptonTheme.cs` (adapter), `KryptonTheme.Palette.cs` (palette builder), `KryptonTheme.Suite.cs` (Navigator, Workspace, Docking, Ribbon and tool strip entries).

### ObjectListView: `Fungusware.WinFormsThemes.ObjectListView`

```csharp
ObjectListViewTheme.Register();                // once, before ThemeApplier.Install
```

This one uses the lightweight route, not a full adapter: it registers a styler and a refresher for `ObjectListView`.

| Piece | What it does |
|---|---|
| Styler | Bold flat header with themed frame and hover colours, selection colours (focused and unfocused), alternating row colour, no grid lines, themed empty-list message, dark scroll bars |
| Column sizing | The last column fills the free space, so the header bar runs the full width. Header text is never squeezed to a stub ("A...") on an empty list. A list built inside a hidden tab keeps its designed column widths |
| Refresher | After a theme switch, calls `RefreshObjects` so row colours chosen in a formatter are rebuilt |
| `ObjectListViewTheme.StyleList(olv)` | Styles one list on demand, with the current theme |

If an ObjectListView header looks wrong, this is the file to look at: `Fungusware.WinFormsThemes.ObjectListView/ObjectListViewTheme.cs`.

### Editor: `Fungusware.WinFormsThemes.Editor`

The code editor control has its own `EditorTheme` and does not reference the core, so it stays usable on its own. The demo (`VsClone/EditorThemes.cs`) shows how to build an `EditorTheme` from a `Tokens`. Making that a registered adapter is planned.

## Writing your own adapter

Derive from `ThemeAdapter` and override only what you need. Every method has a safe default.

```csharp
using Fungusware.WinFormsThemes;

public sealed class AcmeGridAdapter : ThemeAdapter
{
    AcmeSkin _skin;

    // Called once when the engine installs (or immediately, if it is already installed).
    public override void Install(Tokens t) { _skin = new AcmeSkin(); Apply(t); }

    // Called on every run-time theme switch, before open forms are restyled.
    public override void ThemeChanged(Tokens t) => Apply(t);

    // Which controls are yours? The engine skips its own styling for these.
    public override bool Owns(Control c) => c is AcmeGrid or AcmeToolbar;

    // Style one owned control.
    public override void Style(Control c, Tokens t)
    {
        if (c is AcmeGrid g) { g.HeaderBack = t.SurfaceAlt; g.HeaderFore = t.Text; }
    }

    // Look inside your controls? Say no if their parts belong to your skin.
    public override bool WalkInto(Control c) => c is AcmePanel;

    void Apply(Tokens t) { _skin.Back = t.Surface; _skin.Accent = t.Accent; }
}

public static class AcmeTheme
{
    public static void Register() => ThemeApplier.RegisterAdapter(new AcmeGridAdapter());
}
```

Then, in your app:

```csharp
AcmeTheme.Register();
ThemeApplier.Install(ModernTheme.FluentDark);
```

### Rules for adapters

- **Follow, never lead.** Do not store or choose a theme. Take it from the tokens you are given.
- **Be idempotent.** `Style` runs again on every theme switch for every control you own. Do not hook an event twice: use `ThemeApplier.Tagged(control)` (returns true if the control was already marked, and marks it if not) to attach handlers once.
- **Do not replace controls.** Restyle in place so designer fields and event handlers keep working.
- **Read the tokens at paint time** when you owner-draw: use `ThemeApplier.Current`, so a switch only needs `Invalidate()`.
- **Do not take a dependency in the core.** A new library means a new package `Fungusware.WinFormsThemes.<Library>` that references the core.

### Helpers the engine exposes to adapters

| Helper | Use |
|---|---|
| `ThemeApplier.ScrollBarTheme(control, tokens, darkClass)` | Ask Windows for dark or light native scroll bars on a control |
| `ThemeApplier.Tagged(control)` | Do something once per control |
| `ThemeApplier.PadToolbarButtons(toolStrip)` | The same button padding the engine uses |
| `ThemeApplier.Breathe(control)` | Give a fill-docked input in a table row a little margin |
| `ThemeApplier.RoundedRect(rect, radius)` | A rounded rectangle path |
| `ModernTheme.Blend(a, b, amountOfA)` | Mix two colours |
| `ModernTheme.Chrome(tokens)` | The tone for menu bars, toolbars and the status bar |
| `ModernTheme.IsDark(tokens)` | True for dark themes |
