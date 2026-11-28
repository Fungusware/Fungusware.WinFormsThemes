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
/// <summary>A thin rounded progress bar painted from <see cref="Tokens"/>. <see cref="Value"/> is 0 to 100.</summary>
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

/// <summary>TabControl that paints its own tab strip so no native light chrome shows, with a light border round the page.</summary>
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
        // A light border round the page shows that these are tabs and what each one contains. It goes in the few pixels the native
        // control leaves round its pages, and is drawn first so the selected tab's underline sits on top of it.
        int bottom = 0;
        for (int i = 0; i < TabCount; i++) bottom = Math.Max(bottom, GetTabRect(i).Bottom);
        using (var edge = new Pen(ModernTheme.Blend(t.BorderStrong, t.Border, 0.45f)))
            g.DrawRectangle(edge, 0, bottom + 1, Width - 1, Math.Max(0, Height - bottom - 2));
        for (int i = 0; i < TabCount; i++)
        {
            var r = GetTabRect(i);
            bool sel = i == SelectedIndex;
            DrawCaption(g, TabPages[i], r, sel ? t.Text : t.TextMuted);
            if (sel) { using var pen = new Pen(t.Accent, 3); g.DrawLine(pen, r.Left + 10, r.Bottom + 1, r.Right - 10, r.Bottom + 1); }
        }
    }

    // The page's icon from the control's ImageList (by ImageIndex or ImageKey), then its text, centred in the tab together. An icon
    // too tall for the tab is scaled down. In a tab too narrow for both, the text is the part that is shortened.
    void DrawCaption(Graphics g, TabPage page, Rectangle r, Color colour)
    {
        const TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
        int index = ImageList == null ? -1 : page.ImageIndex >= 0 ? page.ImageIndex : ImageList.Images.IndexOfKey(page.ImageKey);
        if (index < 0 || index >= ImageList.Images.Count)
        {
            TextRenderer.DrawText(g, page.Text, Font, r, colour, flags | TextFormatFlags.HorizontalCenter);
            return;
        }

        const int gap = 4, margin = 2;
        var size = ImageList.ImageSize;
        int height = Math.Min(size.Height, r.Height - 4);
        int width = size.Width * height / size.Height;
        int text = TextRenderer.MeasureText(g, page.Text, Font, r.Size, flags).Width;
        int room = Math.Max(0, r.Width - 2 * margin - width - gap);
        text = Math.Min(text, room);
        int x = r.Left + Math.Max(margin, (r.Width - width - gap - text) / 2);
        var box = new Rectangle(x, r.Top + (r.Height - height) / 2, width, height);

        if (box.Size == size) ImageList.Draw(g, box.Location, index);
        else
        {
            using var image = ImageList.Images[index];
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(image, box);
        }
        TextRenderer.DrawText(g, page.Text, Font, new Rectangle(box.Right + gap, r.Top, text, r.Height), colour, flags);
    }
}
