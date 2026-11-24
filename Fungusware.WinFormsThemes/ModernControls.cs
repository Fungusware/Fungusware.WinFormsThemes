// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using System.Drawing.Drawing2D;

namespace Fungusware.WinFormsThemes;

/// <summary>Rounded surface with a hairline border, painted to match the active tokens.</summary>
public class CardPanel : Panel
{
    public Tokens Tokens { get; set; } = ModernTheme.Light;
    public CardPanel()
    {
        DoubleBuffered = true;
        Padding = new Padding(16);
        ResizeRedraw = true;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Tokens.Canvas);
        using var path = Round(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), Tokens.CardRadius);
        using var fill = new SolidBrush(Tokens.Surface);
        using var pen = new Pen(Tokens.Border);
        g.FillPath(fill, path);
        g.DrawPath(pen, path);
    }
    public static GraphicsPath Round(RectangleF r, float rad)
    {
        float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

/// <summary>Thin pill progress bar (Krypton has none).</summary>
public class PillProgress : Control
{
    int _value;
    public Tokens Tokens { get; set; } = ModernTheme.Light;
    public int Value { get => _value; set { _value = Math.Clamp(value, 0, 100); Invalidate(); } }
    public PillProgress() { DoubleBuffered = true; Height = 8; }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Tokens.Surface);
        var track = new RectangleF(0, 0, Width - 1, Height - 1);
        using (var tp = CardPanel.Round(track, Height / 2f))
        using (var b = new SolidBrush(Tokens.SurfaceAlt)) g.FillPath(b, tp);
        float w = Math.Max(Height, track.Width * _value / 100f);
        using var fp = CardPanel.Round(new RectangleF(0, 0, w, track.Height), Height / 2f);
        using var fb = new SolidBrush(Tokens.Accent);
        g.FillPath(fb, fp);
    }
}

/// <summary>TabControl that paints its own tab strip so no native light chrome shows.</summary>
public class ModernTabControl : TabControl
{
    public ModernTabControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        ItemSize = new Size(100, 32);
        SizeMode = TabSizeMode.Fixed;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var t = ThemeApplier.Current; var g = e.Graphics;
        g.Clear(t.Surface);
        int bottom = 0;
        for (int i = 0; i < TabCount; i++)
        {
            var r = GetTabRect(i); bottom = Math.Max(bottom, r.Bottom);
            bool sel = i == SelectedIndex;
            TextRenderer.DrawText(g, TabPages[i].Text, Font, r, sel ? t.Text : t.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (sel) { using var pen = new Pen(t.Accent, 3); g.DrawLine(pen, r.Left + 10, r.Bottom + 1, r.Right - 10, r.Bottom + 1); }
        }
        using var line = new Pen(t.Border);
        g.DrawLine(line, 0, bottom + 2, Width, bottom + 2);
    }
}
