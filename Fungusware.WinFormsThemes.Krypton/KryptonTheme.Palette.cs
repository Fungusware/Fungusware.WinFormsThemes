// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using System.Drawing;
using System.Windows.Forms;
using Krypton.Toolkit;
using static Fungusware.WinFormsThemes.ModernTheme;

namespace Fungusware.WinFormsThemes;

/// <summary>Builds a Krypton palette from <see cref="Tokens"/> and plugs Krypton into the theme engine (see <see cref="Register"/>).</summary>
public static partial class KryptonTheme
{
    /// <summary>Base font size (pt, Segoe UI) used by the palette. Set before the theme is installed.</summary>
    public static float FontSize { get; set; } = 9f;

    public static KryptonCustomPaletteBase Build(Tokens t)
    {
        var p = new KryptonCustomPaletteBase();
        p.BasePalette = KryptonManager.GetPaletteForMode(PaletteMode.Microsoft365Silver);

        // 9pt Segoe UI matches the metrics the designer layouts were drawn with, so labels don't outgrow their slots.
        var font = new Font("Segoe UI", FontSize);
        var semi = new Font(font.FontFamily, FontSize, FontStyle.Bold);

        // ---- window + panels ------------------------------------------
        SolidBack(p.FormStyles.FormMain.StateCommon.Back, t.Canvas);
        p.FormStyles.FormMain.StateCommon.Border.Color1 = t.Border;
        p.FormStyles.FormMain.StateCommon.Border.Color2 = t.Border;

        // title bar: flat, canvas coloured, theme text
        foreach (dynamic fs in new dynamic[] { p.HeaderStyles.HeaderForm.StateCommon, p.HeaderStyles.HeaderForm.StateNormal })
        {
            fs.Back.ColorStyle = PaletteColorStyle.Solid;
            fs.Back.Color1 = t.Canvas; fs.Back.Color2 = t.Canvas;
            fs.Content.ShortText.Color1 = t.Text; fs.Content.ShortText.Color2 = t.Text;
        }

        Solid(p.PanelStyles.PanelClient.StateCommon, t.Canvas);
        Solid(p.PanelStyles.PanelAlternate.StateCommon, t.Canvas);
        // Custom1 = "card": surface fill (rounded outline drawn by CardPanel)
        Solid(p.PanelStyles.PanelCustom1.StateCommon, t.Surface);
        // Custom2 = side nav
        Solid(p.PanelStyles.PanelCustom2.StateCommon, t.Canvas);
        // Custom3 = bar tone (status bar and other bottom bars)
        Solid(p.PanelStyles.PanelCustom3.StateCommon, Chrome(t));

        // ---- labels ----------------------------------------------------
        p.LabelStyles.LabelNormalControl.StateCommon.ShortText.Color1 = t.Text;
        p.LabelStyles.LabelNormalControl.StateCommon.ShortText.Font = font;
        foreach (var l in new[] { p.LabelStyles.LabelNormalPanel, p.LabelStyles.LabelBoldPanel, p.LabelStyles.LabelItalicPanel, p.LabelStyles.LabelTitlePanel })
        { l.StateCommon.ShortText.Color1 = t.Text; l.StateNormal.ShortText.Color1 = t.Text; l.StateCommon.ShortText.Font = l == p.LabelStyles.LabelBoldPanel ? semi : font; }
        p.LabelStyles.LabelTitlePanel.StateCommon.ShortText.Font = new Font(font.FontFamily, 20f, FontStyle.Bold);
        p.LabelStyles.LabelNormalControl.StateNormal.ShortText.Color1 = t.Text;
        p.LabelStyles.LabelBoldControl.StateNormal.ShortText.Color1 = t.Text;
        p.LabelStyles.LabelTitleControl.StateNormal.ShortText.Color1 = t.Text;
        p.LabelStyles.LabelCustom1.StateNormal.ShortText.Color1 = t.TextMuted;
        p.LabelStyles.LabelNormalControl.StateDisabled.ShortText.Color1 = t.Disabled;
        p.LabelStyles.LabelBoldControl.StateCommon.ShortText.Color1 = t.Text;
        p.LabelStyles.LabelBoldControl.StateCommon.ShortText.Font = semi;
        p.LabelStyles.LabelTitleControl.StateCommon.ShortText.Color1 = t.Text;
        p.LabelStyles.LabelTitleControl.StateCommon.ShortText.Font = new Font(font.FontFamily, 20f, FontStyle.Bold);
        // hint / explanation text: a step softer than body text, but still readable on every theme
        var hint = Blend(t.Text, t.Canvas, 0.72f);
        foreach (var l in new[] { p.LabelStyles.LabelItalicControl, p.LabelStyles.LabelItalicPanel, p.LabelStyles.LabelKeyTip, p.LabelStyles.LabelSuperTip })
        {
            l.StateCommon.ShortText.Color1 = hint; l.StateNormal.ShortText.Color1 = hint;
            l.StateDisabled.ShortText.Color1 = t.Disabled;
        }
        // Custom1 = muted caption
        p.LabelStyles.LabelCustom1.StateCommon.ShortText.Color1 = t.TextMuted;
        p.LabelStyles.LabelCustom1.StateCommon.ShortText.Font = new Font(font.FontFamily, 9f);

        // ---- buttons ---------------------------------------------------
        // Standalone = secondary
        Button(p.ButtonStyles.ButtonStandalone, t.Surface, t.SurfaceAlt, t.Border, t.BorderStrong, t.Text, t, font);
        // Custom1 = primary (accent fill)
        Button(p.ButtonStyles.ButtonCustom1, t.Accent, t.AccentHover, t.AccentPressed, t.Accent, t.OnAccent, t, font);
        // Custom2 = subtle (transparent until hovered)
        Button(p.ButtonStyles.ButtonCustom2, t.Canvas, t.SurfaceAlt, t.Border, t.Canvas, t.Text, t, font);

        // drop-down / spinner buttons inside inputs: flat, same fill as the input
        Button(p.ButtonStyles.ButtonInputControl, t.Surface, t.SurfaceAlt, t.Border, t.Surface, t.Text, t, font);

        // ---- text inputs / combo / numeric / date ---------------------
        foreach (var s in new[] { p.InputControlStyles.InputControlStandalone, p.InputControlStyles.InputControlRibbon, p.InputControlStyles.InputControlCustom1, p.InputControlStyles.InputControlCustom2, p.InputControlStyles.InputControlCustom3 })
        {
        var c = s.StateCommon;
        c.Back.Color1 = t.Surface; c.Back.Color2 = t.Surface; c.Back.ColorStyle = PaletteColorStyle.Solid;
        c.Border.Color1 = t.Border; c.Border.Color2 = t.Border;
        c.Border.Width = 1; c.Border.Rounding = t.Radius;
        c.Border.DrawBorders = PaletteDrawBorders.All;
        c.Border.GraphicsHint = PaletteGraphicsHint.AntiAlias;
        c.Content.ShortText.Color1 = t.Text; c.Content.ShortText.Font = font;
        c.Content.Padding = new Padding(4, 3, 4, 3);
        s.StateNormal.Border.Color1 = t.Border; s.StateNormal.Border.Color2 = t.Border;
        s.StateActive.Border.Color1 = t.Accent; s.StateActive.Border.Color2 = t.Accent;
        s.StateDisabled.Back.Color1 = t.SurfaceAlt; s.StateDisabled.Back.Color2 = t.SurfaceAlt;
        s.StateDisabled.Content.ShortText.Color1 = t.Disabled;
        }

        // ---- grid ------------------------------------------------------
        var g = p.GridStyles.GridCommon.StateCommon;
        g.Background.Color1 = t.Surface;
        g.HeaderColumn.Back.Color1 = t.Surface; g.HeaderColumn.Back.Color2 = t.Surface;
        g.HeaderColumn.Content.Color1 = t.TextMuted;
        g.HeaderColumn.Content.Font = semi;
        g.HeaderColumn.Border.Color1 = t.Border; g.HeaderColumn.Border.Color2 = t.Border;
        g.DataCell.Back.Color1 = t.Surface; g.DataCell.Back.Color2 = t.Surface;
        g.DataCell.Content.Color1 = t.Text; g.DataCell.Content.Font = font;
        g.DataCell.Border.Color1 = t.Border; g.DataCell.Border.Color2 = t.Border;
        var sel = p.GridStyles.GridCommon.StateSelected.DataCell;
        sel.Back.Color1 = Blend(t.Accent, t.Surface, 0.16f);
        sel.Back.Color2 = sel.Back.Color1;
        sel.Content.Color1 = t.Text;

        // ---- header (flat, hairline underline) --------------------------
        foreach (var h in new[] { p.HeaderStyles.HeaderPrimary, p.HeaderStyles.HeaderSecondary, p.HeaderStyles.HeaderDockActive })
        {
            h.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;
            h.StateCommon.Back.Color1 = t.Surface; h.StateCommon.Back.Color2 = t.Surface;
            h.StateCommon.Border.Color1 = t.Border; h.StateCommon.Border.Color2 = t.Border;
            h.StateCommon.Border.DrawBorders = PaletteDrawBorders.Bottom;
            h.StateCommon.Border.Rounding = 0;
            h.StateCommon.Content.ShortText.Color1 = t.Text; h.StateCommon.Content.ShortText.Font = semi;
            h.StateCommon.Content.LongText.Color1 = t.TextMuted; h.StateCommon.Content.LongText.Font = font;
        }

        // ---- list box items (selection = accent tint, not yellow) -------
        var li = p.ButtonStyles.ButtonListItem;
        var selBack = Blend(t.Accent, t.Surface, 0.22f);
        li.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;
        li.StateCommon.Border.DrawBorders = PaletteDrawBorders.None;
        li.StateCommon.Content.ShortText.Color1 = t.Text; li.StateCommon.Content.ShortText.Font = font;
        li.StateCommon.Content.Padding = new Padding(6, 3, 6, 3);
        Fill(li.StateNormal, t.Surface, t.Surface);
        Fill(li.StateTracking, t.SurfaceAlt, t.SurfaceAlt);
        Fill(li.StatePressed, t.SurfaceAlt, t.SurfaceAlt);
        Fill(li.StateCheckedNormal, selBack, selBack);
        Fill(li.StateCheckedTracking, selBack, selBack);
        Fill(li.StateCheckedPressed, selBack, selBack);
        li.StateNormal.Content.ShortText.Color1 = t.Text;
        li.StateTracking.Content.ShortText.Color1 = t.Text;
        li.StateCheckedNormal.Content.ShortText.Color1 = t.Text;
        li.StateCheckedTracking.Content.ShortText.Color1 = t.Text;
        li.StatePressed.Content.ShortText.Color1 = t.Text;
        li.StateCheckedPressed.Content.ShortText.Color1 = t.Text;
        li.StateDisabled.Content.ShortText.Color1 = t.Disabled;

        // ---- splitters --------------------------------------------------
        foreach (var sep in new[] { p.SeparatorStyles.SeparatorHighProfile, p.SeparatorStyles.SeparatorHighInternalProfile, p.SeparatorStyles.SeparatorLowProfile })
        {
            sep.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;
            sep.StateCommon.Back.Color1 = t.Border; sep.StateCommon.Back.Color2 = t.Border;
            sep.StateTracking.Back.Color1 = t.Accent; sep.StateTracking.Back.Color2 = t.Accent;
            sep.StatePressed.Back.Color1 = t.AccentPressed; sep.StatePressed.Back.Color2 = t.AccentPressed;
        }

        // ---- check box + radio glyphs (drawn, so they match the tokens) --
        var cb = p.Images.CheckBox;
        cb.UncheckedNormal = Box(t, false, t.BorderStrong, t.Surface);
        cb.UncheckedTracking = Box(t, false, t.Accent, t.Surface);
        cb.UncheckedPressed = Box(t, false, t.AccentPressed, t.SurfaceAlt);
        cb.UncheckedDisabled = Box(t, false, t.Disabled, t.SurfaceAlt);
        cb.CheckedNormal = Box(t, true, t.Accent, t.Accent);
        cb.CheckedTracking = Box(t, true, t.AccentHover, t.AccentHover);
        cb.CheckedPressed = Box(t, true, t.AccentPressed, t.AccentPressed);
        cb.CheckedDisabled = Box(t, true, t.Disabled, t.Disabled);
        var rb = p.Images.RadioButton;
        rb.UncheckedNormal = Dot(t, false, t.BorderStrong);
        rb.UncheckedTracking = Dot(t, false, t.Accent);
        rb.UncheckedPressed = Dot(t, false, t.AccentPressed);
        rb.UncheckedDisabled = Dot(t, false, t.Disabled);
        rb.CheckedNormal = Dot(t, true, t.Accent);
        rb.CheckedTracking = Dot(t, true, t.AccentHover);
        rb.CheckedPressed = Dot(t, true, t.AccentPressed);
        rb.CheckedDisabled = Dot(t, true, t.Disabled);

        BuildSuite(p, t, font, semi);

        return p;
    }

    const int G = 20;

    static Bitmap Box(Tokens t, bool on, Color edge, Color fill)
    {
        var bmp = new Bitmap(G, G);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = ThemeApplier.RoundedRect(new RectangleF(2, 2, G - 5, G - 5), Math.Min(4, t.Radius));
        using (var b = new SolidBrush(fill)) g.FillPath(b, path);
        using (var pen = new Pen(edge, 1.5f)) g.DrawPath(pen, path);
        if (on)
        {
            using var tick = new Pen(t.OnAccent, 2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round, LineJoin = System.Drawing.Drawing2D.LineJoin.Round };
            g.DrawLines(tick, new[] { new PointF(5.5f, 10.5f), new PointF(8.5f, 13.5f), new PointF(14.5f, 6.5f) });
        }
        return bmp;
    }

    static Bitmap Dot(Tokens t, bool on, Color edge)
    {
        var bmp = new Bitmap(G, G);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (var b = new SolidBrush(on ? edge : t.Surface)) g.FillEllipse(b, 2, 2, G - 5, G - 5);
        using (var pen = new Pen(edge, 1.5f)) g.DrawEllipse(pen, 2, 2, G - 5, G - 5);
        if (on) { using var w = new SolidBrush(t.OnAccent); g.FillEllipse(w, 7, 7, 6, 6); }
        return bmp;
    }


    static void SolidBack(PaletteBack b, Color c) { b.Color1 = c; b.Color2 = c; b.ColorStyle = PaletteColorStyle.Solid; }
    static void Solid(dynamic b, Color c) { b.Color1 = c; b.Color2 = c; }

    static void Button(dynamic style, Color normal, Color hover, Color pressed,
                       Color border, Color text, Tokens t, Font font)
    {
        var c = style.StateCommon;
        c.Back.ColorStyle = PaletteColorStyle.Solid;
        c.Border.Width = 1; c.Border.Rounding = t.Radius;
        c.Border.DrawBorders = PaletteDrawBorders.All;
        c.Border.GraphicsHint = PaletteGraphicsHint.AntiAlias;
        c.Content.ShortText.Font = font;
        c.Content.ShortText.Color1 = text;
        c.Content.Padding = new Padding(6, 4, 6, 4);

        Fill(style.StateNormal, normal, border);
        Fill(style.StateTracking, hover, border);
        Fill(style.StatePressed, pressed, border);
        Fill(style.StateCheckedNormal, pressed, border);
        Fill(style.StateDisabled, t.SurfaceAlt, t.Border);
        style.StateDisabled.Content.ShortText.Color1 = t.Disabled;
    }

    static void Fill(PaletteTriple s, Color back, Color border)
    {
        s.Back.Color1 = back; s.Back.Color2 = back;
        s.Border.Color1 = border; s.Border.Color2 = border;
    }
}
