// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using Krypton.Toolkit;
using KryptonModern.Editor;

namespace KryptonModern.VsClone;

/// <summary>The coloured status bar along the bottom (accent normally, orange while the app runs).</summary>
public sealed class StatusBarEx : Control
{
    public sealed class Item
    {
        public string Text = ""; public bool Right; public Action Click; public Rectangle Bounds; public string Tip;
    }
    public readonly List<Item> Items = new();
    public Color Back = Color.RoyalBlue, Fore = Color.White;
    Item _hot;

    public StatusBarEx() { Height = 24; Dock = DockStyle.Bottom; DoubleBuffered = true; Font = new Font("Segoe UI", 9f); }

    public Item Add(string text, bool right, Action click = null, string tip = null)
    {
        var i = new Item { Text = text, Right = right, Click = click, Tip = tip }; Items.Add(i); Invalidate(); return i;
    }
    public void Set(Item i, string text) { if (i.Text != text) { i.Text = text; Invalidate(); } }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.Clear(Back);
        int xl = 4, xr = Width - 4;
        foreach (var it in Items.Where(i => !i.Right))
        {
            int w = TextRenderer.MeasureText(it.Text, Font, Size.Empty, TextFormatFlags.NoPadding).Width + 20;
            it.Bounds = new Rectangle(xl, 0, w, Height); xl += w;
        }
        foreach (var it in Items.Where(i => i.Right).Reverse())
        {
            int w = TextRenderer.MeasureText(it.Text, Font, Size.Empty, TextFormatFlags.NoPadding).Width + 20;
            xr -= w; it.Bounds = new Rectangle(xr, 0, w, Height);
        }
        foreach (var it in Items)
        {
            if (it == _hot && it.Click != null) { using var b = new SolidBrush(Color.FromArgb(48, Fore)); g.FillRectangle(b, it.Bounds); }
            TextRenderer.DrawText(g, it.Text, Font, it.Bounds, Fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var h = Items.FirstOrDefault(i => i.Bounds.Contains(e.Location) && i.Click != null);
        if (h != _hot) { _hot = h; Cursor = h != null ? Cursors.Hand : Cursors.Default; Invalidate(); }
        base.OnMouseMove(e);
    }
    protected override void OnMouseLeave(EventArgs e) { _hot = null; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseClick(MouseEventArgs e) { base.OnMouseClick(e); Items.FirstOrDefault(i => i.Bounds.Contains(e.Location))?.Click?.Invoke(); }
}

/// <summary>Project / type / member drop-down strip above the editor.</summary>
public sealed class NavBar : Control
{
    public string Project = "", Type = "", Member = "";
    public NavBar() { Height = 26; Dock = DockStyle.Top; DoubleBuffered = true; Font = new Font("Segoe UI", 9f); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var t = ThemeApplier.Current; var g = e.Graphics;
        g.Clear(t.Surface);
        string[] parts = { Project, Type, Member };
        int pad = 6, gap = 6, w = (Width - pad * 2 - gap * 2) / 3;
        using var pen = new Pen(t.Border);
        for (int i = 0; i < 3; i++)
        {
            var r = new Rectangle(pad + i * (w + gap), 3, w, Height - 7);
            g.DrawRectangle(pen, r);
            TextRenderer.DrawText(g, string.IsNullOrEmpty(parts[i]) ? " " : parts[i], Font, new Rectangle(r.X + 8, r.Y, r.Width - 26, r.Height), t.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, "⌄", Font, new Rectangle(r.Right - 20, r.Y - 2, 16, r.Height), t.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
        }
        g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
    }
}

/// <summary>Quick Find / Replace panel that floats at the top of an editor, like Visual Studio's.</summary>
public sealed class FindBar : KryptonPanel
{
    readonly KryptonTextBox _find = new(), _repl = new();
    readonly KryptonLabel _count = new();
    readonly KryptonButton _prev = new(), _next = new(), _close = new(), _toggle = new(), _r1 = new(), _rAll = new();
    readonly KryptonCheckBox _case = new();
    bool _replace;
    public CodeEditor Editor;

    public FindBar()
    {
        Dock = DockStyle.Top; Height = 40; Visible = false; PanelBackStyle = PaletteBackStyle.PanelCustom1;
        Control Place(Control c, int x, int y, int w, int h) { c.SetBounds(x, y, w, h); Controls.Add(c); return c; }

        _toggle.ButtonStyle = ButtonStyle.Custom2; _toggle.Click += (_, _) => SetReplace(!_replace);
        Place(_toggle, 6, 7, 26, 26);
        _find.CueHint.CueHintText = "Find"; Place(_find, 38, 7, 260, 26);
        _find.TextChanged += (_, _) => { Editor?.SetFindHighlight(_find.Text, _case.Checked); UpdateCount(); if (Editor != null && _find.Text.Length > 0) { Editor.Select(Editor.Caret with { Column = Math.Max(0, Editor.Caret.Column - (Editor.HasSelection ? Editor.SelectedText.Length : 0)) }, Editor.Caret with { Column = Math.Max(0, Editor.Caret.Column - (Editor.HasSelection ? Editor.SelectedText.Length : 0)) }); Run(true); } };
        _find.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Run(!e.Shift); e.SuppressKeyPress = true; } else if (e.KeyCode == Keys.Escape) { CloseBar(); e.SuppressKeyPress = true; } };
        _count.LabelStyle = LabelStyle.Custom1; Place(_count, 304, 10, 90, 22);
        _prev.ButtonStyle = ButtonStyle.Custom2; _prev.Click += (_, _) => Run(false); Place(_prev, 396, 7, 26, 26);
        _next.ButtonStyle = ButtonStyle.Custom2; _next.Click += (_, _) => Run(true); Place(_next, 424, 7, 26, 26);
        _case.Text = "Match case"; _case.CheckedChanged += (_, _) => { Editor?.SetFindHighlight(_find.Text, _case.Checked); UpdateCount(); }; Place(_case, 458, 10, 100, 22);
        _close.ButtonStyle = ButtonStyle.Custom2; _close.Click += (_, _) => CloseBar(); Place(_close, 566, 7, 30, 26);

        _repl.CueHint.CueHintText = "Replace"; Place(_repl, 38, 38, 260, 26);
        _repl.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { DoReplace(); e.SuppressKeyPress = true; } else if (e.KeyCode == Keys.Escape) { CloseBar(); e.SuppressKeyPress = true; } };
        _r1.Text = "Replace"; _r1.ButtonStyle = ButtonStyle.Standalone; _r1.Click += (_, _) => DoReplace(); Place(_r1, 304, 38, 92, 26);
        _rAll.Text = "Replace all"; _rAll.ButtonStyle = ButtonStyle.Standalone; _rAll.Click += (_, _) => { if (Editor != null) { int n = Editor.ReplaceAll(_find.Text, _repl.Text, _case.Checked); _count.Values.Text = n + " replaced"; } }; Place(_rAll, 402, 38, 112, 26);
        SetReplace(false);
    }

    public void RefreshIcons(Tokens t)
    {
        _prev.Values.Image = Ico.Glyph(Ico.Up, t.Text, 14); _next.Values.Image = Ico.Glyph(Ico.Down, t.Text, 14);
        _close.Values.Image = Ico.Glyph(Ico.Close, t.Text, 14);
        _toggle.Values.Image = Ico.Glyph(_replace ? Ico.Down : Ico.Chevron, t.Text, 14);
    }

    void SetReplace(bool on)
    {
        _replace = on; Height = on ? 72 : 40;
        _repl.Visible = _r1.Visible = _rAll.Visible = on;
        RefreshIcons(ThemeApplier.Current);
    }

    public void Open(bool replace)
    {
        Visible = true; SetReplace(replace || _replace && replace);
        if (Editor != null)
        {
            string seed = Editor.HasSelection && !Editor.SelectedText.Contains('\n') ? Editor.SelectedText : Editor.CurrentWord();
            if (seed.Length > 0) _find.Text = seed;
            Editor.SetFindHighlight(_find.Text, _case.Checked);
        }
        UpdateCount(); _find.Focus(); _find.SelectAll();
    }

    void CloseBar() { Visible = false; Editor?.SetFindHighlight("", false); Editor?.Focus(); }

    void Run(bool fwd)
    {
        if (Editor == null || _find.Text.Length == 0) return;
        if (!Editor.FindNext(_find.Text, _case.Checked, fwd)) _count.Values.Text = "No results";
        else UpdateCount();
    }

    void DoReplace() { if (Editor != null) Editor.ReplaceCurrent(_find.Text, _repl.Text, _case.Checked); UpdateCount(); }

    void UpdateCount()
    {
        if (Editor == null) return;
        int n = Editor.CountMatches(_find.Text, _case.Checked);
        _count.Values.Text = _find.Text.Length == 0 ? "" : n == 0 ? "No results" : n + (n == 1 ? " result" : " results");
    }
}

/// <summary>One open document: navigation bar, quick-find bar and the editor.</summary>
public sealed class DocumentView : KryptonPanel
{
    public readonly CodeEditor Editor = new() { Dock = DockStyle.Fill };
    public readonly FindBar Find = new();
    public readonly NavBar Nav = new();
    public string Path { get; set; }

    public DocumentView(string path, string project)
    {
        Path = path; Editor.FilePath = path; Find.Editor = Editor; Nav.Project = project;
        Controls.Add(Editor); Controls.Add(Find); Controls.Add(Nav);
        Controls.SetChildIndex(Editor, 0); Controls.SetChildIndex(Find, 1); Controls.SetChildIndex(Nav, 2);
        Editor.CaretMoved += (_, _) => UpdateNav();
    }

    int _navLine = -1;
    void UpdateNav()
    {
        if (Editor.Caret.Line == _navLine) return;
        _navLine = Editor.Caret.Line;
        var (ns, type, member) = CodeStructure.Where(Editor);
        Nav.Type = type.Length == 0 ? "" : (ns.Length > 0 ? ns + "." : "") + type;
        Nav.Member = member;
        Nav.Invalidate();
    }
}
