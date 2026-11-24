// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using System.Drawing;
using System.Windows.Forms;
using Krypton.Toolkit;

namespace Fungusware.WinFormsThemes;

public static partial class KryptonTheme
{
    static bool _registered;

    /// <summary>
    /// Plugs Krypton into the theme engine. Call once at start-up, before <see cref="ThemeApplier.Install"/>: the engine then
    /// builds the Krypton palette for the active theme, rebuilds it on every theme switch, and styles Krypton controls.
    /// Apps that manage the palette themselves can skip <c>Install</c> and use <see cref="Build"/> directly.
    /// </summary>
    public static void Register()
    {
        if (_registered) return;
        _registered = true;
        ThemeApplier.RegisterAdapter(new Adapter());
    }

    sealed class Adapter : ThemeAdapter
    {
        KryptonManager _manager;

        public override void Install(Tokens t)
        {
            _manager = new KryptonManager { GlobalPaletteMode = PaletteMode.Custom, GlobalApplyToolstrips = false };
            _manager.GlobalCustomPalette = Build(t);
        }

        public override void ThemeChanged(Tokens t) { if (_manager != null) _manager.GlobalCustomPalette = Build(t); }

        public override bool Owns(Control c) => c.GetType().Namespace == "Krypton.Toolkit";

        public override void Style(Control c, Tokens t) => StyleKrypton(c, t);

        // Do not walk inside Krypton input controls; their parts belong to the palette.
        public override bool WalkInto(Control c) =>
            c is KryptonPanel or KryptonForm or KryptonSplitContainer or KryptonSplitterPanel or KryptonGroupBox or KryptonGroup;
    }

    internal static void StyleKrypton(Control c, Tokens t)
    {
        // Krypton draws inputs in their "active" (accent) state all the time unless told otherwise.
        switch (c)
        {
            case KryptonTextBox x: x.AlwaysActive = false; ThemeApplier.Breathe(x); break;
            case KryptonComboBox x: x.AlwaysActive = false; ThemeApplier.Breathe(x); break;
            case KryptonNumericUpDown x: x.AlwaysActive = false; ThemeApplier.Breathe(x); break;
            case KryptonMaskedTextBox x: x.AlwaysActive = false; ThemeApplier.Breathe(x); break;
            case KryptonRichTextBox x: x.AlwaysActive = false; break;
            case KryptonDateTimePicker x: x.AlwaysActive = false; ThemeApplier.Breathe(x); break;
        }

        switch (c)
        {
            case KryptonPanel bar when bar.Dock == DockStyle.Bottom && bar.Height <= 56 && bar.Parent is Form:
                // A bottom bar (Xackup's status bar). Give it the same tone as the toolbars and a hairline on top.
                bar.PanelBackStyle = PaletteBackStyle.PanelCustom3;
                if (!ThemeApplier.Tagged(bar))
                    bar.Paint += (_, e) =>
                    {
                        using var pen = new Pen(ModernTheme.Blend(ThemeApplier.Current.BorderStrong, ModernTheme.Chrome(ThemeApplier.Current), 0.45f));
                        e.Graphics.DrawLine(pen, 0, 0, bar.Width, 0);
                    };
                break;
            case ToolStrip ts: ThemeApplier.PadToolbarButtons(ts); break; // KryptonToolStrip
            case KryptonLinkLabel link:
                foreach (var o in new[] { link.OverrideNotVisited, link.OverrideVisited, link.OverrideFocus, link.OverridePressed })
                    o.ShortText.Color1 = t.Accent;
                break;
        }
    }
}
