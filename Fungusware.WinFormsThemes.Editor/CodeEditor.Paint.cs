// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using System.Drawing.Drawing2D;

namespace Fungusware.WinFormsThemes.Editor;

public sealed partial class CodeEditor
{
    Bitmap _map; bool _mapDirty = true; int _mapFirst;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; var T = _theme;
        g.Clear(T.Background);
        int gw = GutterW, textRight = TextRight;
        int first = First, lastVis = Math.Min(_lines.Count - 1, first + VisLines + 1);
        var sel0 = SelStart; var sel1 = SelEnd;
        using var guidePen = new Pen(T.IndentGuide);
        using var curBrush = new SolidBrush(T.CurrentLine);
        using var selBrush = new SolidBrush(Focused ? T.Selection : T.SelectionInactive);
        using var findBrush = new SolidBrush(T.FindMatch);
        var textClip = new Rectangle(gw, 0, Math.Max(1, textRight - gw), ViewH);
        var cmp = _findCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        for (int i = first; i <= lastVis; i++)
        {
            int y = YOf(i);
            if (y > ViewH) break;
            string line = _lines[i];

            if (_showLineNumbers)
                TextRenderer.DrawText(g, (i + 1).ToString(), _font, new Rectangle(18, y, gw - 18 - 8, _lh), i == _caret.Line ? T.GutterActiveForeground : T.GutterForeground,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            if (Breakpoints.Contains(i))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var bb = new SolidBrush(T.Error);
                g.FillEllipse(bb, 3, y + (_lh - 11) / 2, 11, 11);
                g.SmoothingMode = SmoothingMode.None;
            }

            g.SetClip(textClip);
            if (i == _caret.Line && !HasSelection) g.FillRectangle(curBrush, textClip.Left, y, textClip.Width, _lh);
            if (_findTerm.Length > 0)
            {
                int p = 0;
                while ((p = line.IndexOf(_findTerm, p, cmp)) >= 0) { g.FillRectangle(findBrush, XOf(p), y, (int)(_findTerm.Length * _cw), _lh); p += _findTerm.Length; }
            }
            if (HasSelection && i >= sel0.Line && i <= sel1.Line)
            {
                int c1 = i == sel0.Line ? sel0.Column : 0, c2 = i == sel1.Line ? sel1.Column : line.Length + 1;
                g.FillRectangle(selBrush, XOf(c1), y, Math.Max(2, XOf(c2) - XOf(c1)), _lh);
            }
            foreach (var bp in new[] { _braceA, _braceB })
                if (bp is Pos q && q.Line == i)
                {
                    using var mb = new SolidBrush(T.BraceMatch); using var mp = new Pen(T.BraceMatchBorder);
                    g.FillRectangle(mb, XOf(q.Column), y, (int)_cw, _lh); g.DrawRectangle(mp, XOf(q.Column), y, (int)_cw - 1, _lh - 1);
                }
            if (_showGuides)
            {
                int indent = EffIndent(i);
                for (int k = 0; k < indent; k += IndentSize) g.DrawLine(guidePen, XOf(k) + 1, y, XOf(k) + 1, y + _lh);
            }

            DrawText(g, i, y);

            if (Squiggles.TryGetValue(i, out var sq))
            {
                int x1 = XOf(line.Length - line.TrimStart().Length), x2 = XOf(line.TrimEnd().Length);
                using var wp = new Pen(sq.Severity == Severity.Error ? T.Error : sq.Severity == Severity.Warning ? T.Warning : T.Info);
                int yy = y + _lh - 3; var pts = new List<Point>();
                for (int x = x1; x <= x2; x += 3) pts.Add(new Point(x, yy + (((x - x1) / 3) % 2 == 0 ? 0 : 2)));
                if (pts.Count > 1) g.DrawLines(wp, pts.ToArray());
            }
            if (Focused && _caretOn && i == _caret.Line)
            {
                using var cp = new Pen(T.Caret, Overwrite ? 1 : 2);
                int cx = XOf(_caret.Column);
                if (Overwrite) g.DrawRectangle(cp, cx, y + 1, (int)_cw, _lh - 3); else g.DrawLine(cp, cx, y + 1, cx, y + _lh - 2);
            }
            g.ResetClip();
        }

        if (MapW > 0) PaintMap(g);
        if (_popup && _items.Count > 0) PaintPopup(g);
        using var sep = new Pen(EditorTheme.Mix(T.Foreground, T.Background, 0.06f));
        g.DrawLine(sep, gw - 1, 0, gw - 1, ViewH);
    }

    int EffIndent(int i)
    {
        string s = _lines[i];
        if (s.Trim().Length > 0) return s.Length - s.TrimStart().Length;
        int up = 0, down = 0;
        for (int j = i - 1; j >= Math.Max(0, i - 40); j--) if (_lines[j].Trim().Length > 0) { up = _lines[j].Length - _lines[j].TrimStart().Length; if (_lines[j].TrimEnd().EndsWith('{')) up += IndentSize; break; }
        for (int j = i + 1; j < Math.Min(_lines.Count, i + 40); j++) if (_lines[j].Trim().Length > 0) { down = _lines[j].Length - _lines[j].TrimStart().Length; break; }
        return Math.Min(up, down);
    }

    /// <summary>Draw one line as the fewest possible text runs: adjacent pieces of the same colour are merged into a single draw call.</summary>
    void DrawText(Graphics g, int i, int y)
    {
        string line = _lines[i];
        int colStart = Math.Max(0, (int)(ScrollX / _cw) - 1);
        int colEnd = Math.Min(line.Length, colStart + (int)(ViewW / _cw) + 4);
        if (colEnd <= colStart) return;
        var plain = _theme.Tokens[0];
        var toks = TokensOf(i, _tokBuf);
        int runStart = -1, runEnd = 0; Color runColor = default; int pos = 0;

        void Flush()
        {
            if (runStart < 0) return;
            var span = line.AsSpan(runStart, runEnd - runStart);
            if (span.Trim().Length > 0) TextRenderer.DrawText(g, line.Substring(runStart, runEnd - runStart), _font, new Point(XOf(runStart), y + 1), runColor, Tff);
            runStart = -1;
        }
        void Add(int s, int len, Color c)
        {
            int a = Math.Max(s, colStart), b = Math.Min(s + len, colEnd);
            if (b <= a) return;
            if (runStart >= 0 && c == runColor && a == runEnd) { runEnd = b; return; }
            Flush(); runStart = a; runEnd = b; runColor = c;
        }
        foreach (var t in toks)
        {
            if (t.Start > pos) Add(pos, t.Start - pos, plain);
            Add(t.Start, t.Length, _theme.Tokens[(int)t.Kind]);
            pos = t.Start + t.Length;
        }
        if (pos < line.Length) Add(pos, line.Length - pos, plain);
        Flush();
    }

    void PaintPopup(Graphics g)
    {
        var T = _theme; var r = PopupRect();
        using (var b = new SolidBrush(T.PopupBackground)) g.FillRectangle(b, r);
        using (var p = new Pen(T.PopupBorder)) g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);
        int rows = Math.Min(8, _items.Count);
        for (int k = 0; k < rows; k++)
        {
            int idx = _itemTop + k; if (idx >= _items.Count) break;
            var rr = new Rectangle(r.X + 2, r.Y + 2 + k * _lh, r.Width - 4, _lh);
            if (idx == _sel) { using var b = new SolidBrush(T.PopupSelection); g.FillRectangle(b, rr); }
            var item = _items[idx];
            using (var ib = new SolidBrush(T.Tokens[(int)item.Kind])) g.FillRectangle(ib, rr.X + 6, rr.Y + _lh / 2 - 4, 8, 8);
            TextRenderer.DrawText(g, item.Text, _font, new Point(rr.X + 22, rr.Y + 1), T.PopupText, Tff);
            TextRenderer.DrawText(g, item.Text.Substring(0, Math.Min(_prefix.Length, item.Text.Length)), _font, new Point(rr.X + 22, rr.Y + 1), T.PopupMatch, Tff);
        }
    }

    // ---- minimap --------------------------------------------------------
    // One 1px-high coloured bar per token, 2px per line, cached in a bitmap that is rebuilt only when text, theme, size or scroll position change.
    void PaintMap(Graphics g)
    {
        var T = _theme;
        int mw = MapW, mh = ViewH, x0 = TextRight;
        int total = _lines.Count, rows = mh / 2;
        int first = total <= rows ? 0 : (int)((long)(total - rows) * First / Math.Max(1, _lines.Count - VisLines));
        first = Math.Clamp(first, 0, Math.Max(0, total - rows));
        if (_map == null || _map.Width != mw || _map.Height != mh || _mapDirty || first != _mapFirst)
        {
            _mapFirst = first; _mapDirty = false;
            _map?.Dispose(); _map = new Bitmap(mw, mh);
            using var mg = Graphics.FromImage(_map);
            mg.Clear(EditorTheme.Mix(T.Foreground, T.Background, 0.035f));
            int last = Math.Min(total - 1, first + rows);
            var brushes = new SolidBrush[T.Tokens.Length];
            for (int k = 0; k < brushes.Length; k++) brushes[k] = new SolidBrush(Color.FromArgb(150, T.Tokens[k]));
            var toks = new List<Token>();
            for (int i = first; i <= last; i++)
            {
                int y = (i - first) * 2; string line = _lines[i]; int pos = 0;
                void Bar(int s, int len, int kind)
                {
                    int a = s, end = Math.Min(s + len, 120);
                    while (a < end)
                    {
                        while (a < end && line[a] == ' ') a++;
                        int b = a; while (b < end && line[b] != ' ') b++;
                        if (b > a) mg.FillRectangle(brushes[kind], 4 + a, y, b - a, 1);
                        a = b;
                    }
                }
                TokensOf(i, toks);
                foreach (var t in toks) { if (t.Start > pos) Bar(pos, t.Start - pos, 0); Bar(t.Start, t.Length, (int)t.Kind); pos = t.Start + t.Length; }
                if (pos < line.Length) Bar(pos, line.Length - pos, 0);
            }
            foreach (var b in brushes) b.Dispose();
            using var errB = new SolidBrush(T.Error); using var wB = new SolidBrush(T.Warning); using var iB = new SolidBrush(T.Info);
            foreach (var kv in Squiggles) if (kv.Key >= first && kv.Key <= last) mg.FillRectangle(kv.Value.Severity == Severity.Error ? errB : kv.Value.Severity == Severity.Warning ? wB : iB, mw - 5, (kv.Key - first) * 2, 4, 2);
            foreach (var bp in Breakpoints) if (bp >= first && bp <= last) mg.FillEllipse(errB, 0, (bp - first) * 2 - 1, 4, 4);
        }
        g.DrawImageUnscaled(_map, x0, 0);
        int vy = (First - _mapFirst) * 2, vh = Math.Min(VisLines * 2, mh);
        using var vb = new SolidBrush(Color.FromArgb(34, T.Foreground)); using var vp = new Pen(Color.FromArgb(60, T.Foreground));
        g.FillRectangle(vb, x0, vy, mw, vh);
        g.DrawLine(vp, x0, 0, x0, mh);
    }
}
