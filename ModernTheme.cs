// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using Krypton.Toolkit;

namespace KryptonModern;

/// <summary>Colour tokens for one appearance (light or dark).</summary>
public sealed record Tokens(
    Color Canvas,       // window background
    Color Surface,      // cards, inputs
    Color SurfaceAlt,   // hover / subtle fills
    Color Border,
    Color BorderStrong,
    Color Text,
    Color TextMuted,
    Color Accent,
    Color AccentHover,
    Color AccentPressed,
    Color OnAccent,
    Color Disabled,
    string Name = "Theme",
    float Radius = 6f,
    float CardRadius = 10f);

/// <summary>
/// A Krypton palette that imitates Windows 11 / modern web controls:
/// flat fills, 1px hairlines, rounded corners, one accent colour.
/// </summary>
public static partial class ModernTheme
{
    public static readonly Tokens Light = new(
        Canvas: Color.FromArgb(0xF3, 0xF4, 0xF7),
        Surface: Color.White,
        SurfaceAlt: Color.FromArgb(0xEB, 0xED, 0xF2),
        Border: Color.FromArgb(0xDD, 0xE0, 0xE7),
        BorderStrong: Color.FromArgb(0x9A, 0xA1, 0xB0),
        Text: Color.FromArgb(0x1C, 0x21, 0x2B),
        TextMuted: Color.FromArgb(0x5B, 0x63, 0x73),
        Accent: Color.FromArgb(0x25, 0x63, 0xEB),
        AccentHover: Color.FromArgb(0x1D, 0x4E, 0xC7),
        AccentPressed: Color.FromArgb(0x17, 0x3F, 0xA3),
        OnAccent: Color.White,
        Disabled: Color.FromArgb(0xA8, 0xAE, 0xBA), Name: "Fluent Light");

    public static readonly Tokens Dark = new(
        Canvas: Color.FromArgb(0x14, 0x17, 0x1D),
        Surface: Color.FromArgb(0x1E, 0x22, 0x2B),
        SurfaceAlt: Color.FromArgb(0x2A, 0x2F, 0x3A),
        Border: Color.FromArgb(0x32, 0x38, 0x45),
        BorderStrong: Color.FromArgb(0x6B, 0x74, 0x86),
        Text: Color.FromArgb(0xEE, 0xF0, 0xF4),
        TextMuted: Color.FromArgb(0x9A, 0xA3, 0xB5),
        Accent: Color.FromArgb(0x5B, 0x8D, 0xFF),
        AccentHover: Color.FromArgb(0x7A, 0xA3, 0xFF),
        AccentPressed: Color.FromArgb(0x45, 0x78, 0xEB),
        OnAccent: Color.FromArgb(0x0B, 0x13, 0x28),
        Disabled: Color.FromArgb(0x5A, 0x62, 0x73), Name: "Fluent Dark");



    static Color C(int rgb) => Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);

    static Tokens T(string name, float radius, float card, int canvas, int surface, int alt, int border, int strong,
                    int text, int muted, int accent, int hover, int pressed, int onAccent, int disabled) =>
        new(C(canvas), C(surface), C(alt), C(border), C(strong), C(text), C(muted),
            C(accent), C(hover), C(pressed), C(onAccent), C(disabled), name, radius, card);

    public static readonly Tokens MaterialLight = T("Material Light", 4, 12,
        0xF7F2FA, 0xFFFFFF, 0xECE6F0, 0xCAC4D0, 0x79747E, 0x1D1B20, 0x49454F,
        0x6750A4, 0x5B4698, 0x4F3D8A, 0xFFFFFF, 0xB0AAB5);

    public static readonly Tokens MaterialDark = T("Material Dark", 4, 12,
        0x141218, 0x211F26, 0x2B2930, 0x49454F, 0x938F99, 0xE6E0E9, 0xCAC4D0,
        0xD0BCFF, 0xE0D0FF, 0xB9A2F2, 0x381E72, 0x6E6A75);

    public static readonly Tokens SolarizedLight = T("Solarized Light", 2, 4,
        0xEEE8D5, 0xFDF6E3, 0xE6DFC8, 0xD6CFB6, 0x93A1A1, 0x586E75, 0x657B83,
        0x268BD2, 0x2E9BE5, 0x1F78B4, 0x002B36, 0xA3ABA6);

    public static readonly Tokens SolarizedDark = T("Solarized Dark", 2, 4,
        0x002B36, 0x073642, 0x0E4452, 0x1C5565, 0x586E75, 0x93A1A1, 0x7F9296,
        0x268BD2, 0x4AA3E8, 0x1F78B4, 0x002B36, 0x586E75);

    public static readonly Tokens Nord = T("Nord", 4, 8,
        0x2E3440, 0x3B4252, 0x434C5E, 0x4C566A, 0x7B88A1, 0xECEFF4, 0xAEB8CB,
        0x88C0D0, 0x9FD0DE, 0x81A1C1, 0x2E3440, 0x6B7790);

    public static readonly Tokens Dracula = T("Dracula", 6, 10,
        0x21222C, 0x282A36, 0x343746, 0x44475A, 0x6272A4, 0xF8F8F2, 0xB0B6D0,
        0xBD93F9, 0xCDA9FF, 0xA47BE8, 0x282A36, 0x6272A4);

    public static readonly Tokens NeonSynthwave = T("Neon Synthwave", 8, 14,
        0x0B0618, 0x150C2E, 0x20143F, 0x35235F, 0x7A5FBF, 0xF4ECFF, 0xB5A3DD,
        0xFF3CAC, 0xFF6AC1, 0xE0208F, 0x1A0524, 0x6A5A8E);

        // Approximation of DevExpress "Bezier": cool grey canvas, white surfaces, tight corners, steel-blue accent.
    public static readonly Tokens Bezier = T("Bezier", 2, 3,
        0xEDEFF2, 0xFFFFFF, 0xE1E5EA, 0xCDD3DA, 0x8A94A0, 0x2B3036, 0x5A636D,
        0x3872B8, 0x2F63A3, 0x265389, 0xFFFFFF, 0xAAB1B9);

    public static readonly Tokens[] All = { Light, Dark, MaterialLight, MaterialDark, SolarizedLight, SolarizedDark, Nord, Dracula, NeonSynthwave, Bezier };
    public static KryptonCustomPaletteBase Build(Tokens t)
    {
        var p = new KryptonCustomPaletteBase();
        p.BasePalette = KryptonManager.GetPaletteForMode(PaletteMode.Microsoft365Silver);

        var font = new Font("Segoe UI Variable Text", 9.5f);
        if (font.Name != "Segoe UI Variable Text") font = new Font("Segoe UI", 9.5f);
        var semi = new Font(font.FontFamily, 9.5f, FontStyle.Bold);

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
        cb.UncheckedDisabled = Box(t, false, t.Border, t.SurfaceAlt);
        cb.CheckedNormal = Box(t, true, t.Accent, t.Accent);
        cb.CheckedTracking = Box(t, true, t.AccentHover, t.AccentHover);
        cb.CheckedPressed = Box(t, true, t.AccentPressed, t.AccentPressed);
        cb.CheckedDisabled = Box(t, true, t.Disabled, t.Disabled);
        var rb = p.Images.RadioButton;
        rb.UncheckedNormal = Dot(t, false, t.BorderStrong);
        rb.UncheckedTracking = Dot(t, false, t.Accent);
        rb.UncheckedPressed = Dot(t, false, t.AccentPressed);
        rb.UncheckedDisabled = Dot(t, false, t.Border);
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
        using var path = CardPanel.Round(new RectangleF(2, 2, G - 5, G - 5), Math.Min(4, t.Radius));
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

    public static Color Blend(Color a, Color b, float amountOfA) =>
        Color.FromArgb(
            (int)(a.R * amountOfA + b.R * (1 - amountOfA)),
            (int)(a.G * amountOfA + b.G * (1 - amountOfA)),
            (int)(a.B * amountOfA + b.B * (1 - amountOfA)));
}
