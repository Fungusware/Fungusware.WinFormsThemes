// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using System.Text;

namespace Fungusware.WinFormsThemes.Editor;

/// <summary>
/// A themeable code editor: syntax colouring, line numbers, breakpoint margin, minimap, brace matching, auto-close and auto-indent,
/// completion list, find highlighting, squiggles, undo/redo. No dependencies beyond WinForms.
/// <para>Documents are a list of lines. Lexing keeps only one int of state per line and runs on a worker thread, so large files
/// stay responsive; undo stores edits, not copies of the document.</para>
/// </summary>
public sealed partial class CodeEditor : Control
{
    // ---- document -------------------------------------------------------
    readonly List<string> _lines = new() { "" };
    string _eol = "\r\n";
    int _version;
    int _maxLen, _maxStale; bool _maxDirty = true;

    // ---- caret / selection ---------------------------------------------
    Pos _caret, _anchor;
    int _wantCol;

    // ---- undo -----------------------------------------------------------
    sealed class Edit
    {
        public Pos Start, End, CaretBefore, AnchorBefore, CaretAfter;
        public string Removed, Inserted, Kind; public long Tick;
    }
    readonly List<List<Edit>> _undo = new(), _redo = new();
    List<Edit> _group; int _groupDepth, _pendingMin = int.MaxValue;
    const int MaxUndo = 2000;

    // ---- public state ---------------------------------------------------
    public string FilePath { get; set; }
    public bool Modified { get; private set; }
    public bool ReadOnly { get; set; }
    public bool Overwrite { get; set; }
    public int IndentSize { get; set; } = 4;
    /// <summary>Raised after any text change (typing, paste, undo, programmatic edits). Not raised by <see cref="LoadText"/>.</summary>
    public event EventHandler Changed;
    public event EventHandler CaretMoved;
    public event EventHandler BreakpointsChanged;

    /// <summary>Lines (zero-based) with a breakpoint dot. Line numbers are not adjusted when text above them changes.</summary>
    public readonly HashSet<int> Breakpoints = new();
    /// <summary>Wavy underlines by line. Cleared automatically when the text changes, because their positions would be stale.</summary>
    public readonly Dictionary<int, Squiggle> Squiggles = new();

    public int LineCount => _lines.Count;
    public string GetLine(int index) => _lines[index];
    public int Version => _version;
    public Pos Caret => _caret;
    public Pos SelectionAnchor => _anchor;
    public bool HasSelection => _caret != _anchor;
    public string EolName => _eol == "\r\n" ? "CRLF" : "LF";
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    Pos SelStart => _caret < _anchor ? _caret : _anchor;
    Pos SelEnd => _caret < _anchor ? _anchor : _caret;
    public string SelectedText => HasSelection ? TextOf(SelStart, SelEnd).Replace("\n", _eol) : "";

    // ===================================================================
    //  Loading and reading
    // ===================================================================
    public string GetText() => string.Join(_eol, _lines);

    /// <summary>Replace the whole document and clear undo history.</summary>
    public void LoadText(string text)
    {
        _eol = text.Contains("\r\n") ? "\r\n" : text.Contains('\n') ? "\n" : "\r\n";
        _lines.Clear();
        foreach (var l in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) _lines.Add(l.Contains('\t') ? l.Replace("\t", new string(' ', IndentSize)) : l);
        if (_lines.Count == 0) _lines.Add("");
        _caret = _anchor = new Pos(0, 0); _wantCol = 0;
        _undo.Clear(); _redo.Clear(); _group = null; _groupDepth = 0;
        _maxDirty = true; _mapDirty = true; _version++;
        Modified = false; _popup = false;
        Squiggles.Clear();
        _vs.Value = 0; _hs.Value = 0;
        ResetLexing(0, immediate: true);
        LayoutScroll(); UpdateBrace(); Invalidate();
    }

    public void MarkSaved() { Modified = false; Changed?.Invoke(this, EventArgs.Empty); }

    string TextOf(Pos a, Pos b)
    {
        if (a.Line == b.Line) return _lines[a.Line].Substring(a.Column, b.Column - a.Column);
        var sb = new StringBuilder(_lines[a.Line].Substring(a.Column));
        for (int i = a.Line + 1; i < b.Line; i++) sb.Append('\n').Append(_lines[i]);
        sb.Append('\n').Append(_lines[b.Line].Substring(0, b.Column));
        return sb.ToString();
    }

    static Pos EndOf(Pos start, string text)
    {
        int nl = 0, last = -1;
        for (int i = 0; i < text.Length; i++) if (text[i] == '\n') { nl++; last = i; }
        return nl == 0 ? new Pos(start.Line, start.Column + text.Length) : new Pos(start.Line + nl, text.Length - last - 1);
    }

    // ===================================================================
    //  Editing core: every change is one ApplyEdit, recorded for undo
    // ===================================================================
    Pos ApplyEdit(Pos a, Pos b, string text)
    {
        string head = _lines[a.Line].Substring(0, a.Column), tail = _lines[b.Line].Substring(b.Column);
        for (int i = a.Line; i <= b.Line; i++) if (_lines[i].Length >= _maxLen) { _maxStale++; break; }   // _maxLen only ever grows between rescans, so deleting text never costs a pass over the document
        _lines.RemoveRange(a.Line, b.Line - a.Line + 1);
        Pos end;
        if (text.IndexOf('\n') < 0)
        {
            string l = head + text + tail; _lines.Insert(a.Line, l);
            end = new Pos(a.Line, head.Length + text.Length);
            if (l.Length > _maxLen) _maxLen = l.Length;
        }
        else
        {
            var parts = text.Split('\n');
            var ins = new List<string>(parts.Length) { head + parts[0] };
            for (int i = 1; i < parts.Length - 1; i++) ins.Add(parts[i]);
            ins.Add(parts[^1] + tail);
            _lines.InsertRange(a.Line, ins);
            foreach (var l in ins) if (l.Length > _maxLen) _maxLen = l.Length;
            end = new Pos(a.Line + parts.Length - 1, parts[^1].Length);
        }
        _version++;
        return end;
    }

    /// <summary>Replace the text between two positions. This is the one primitive all edits use; it records undo.</summary>
    public void Replace(Pos a, Pos b, string text, string kind = "edit")
    {
        if (ReadOnly) return;
        if (b < a) (a, b) = (b, a);
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var e = new Edit { Start = a, Removed = TextOf(a, b), Inserted = text, CaretBefore = _caret, AnchorBefore = _anchor, Kind = kind, Tick = Environment.TickCount64 };
        e.End = ApplyEdit(a, b, text);
        _caret = _anchor = e.End; _wantCol = e.End.Column; e.CaretAfter = e.End;
        Record(e);
        Touched(a.Line);
    }

    void Record(Edit e)
    {
        _redo.Clear();
        if (_group != null) { _group.Add(e); return; }
        if (e.Kind == "type" && _undo.Count > 0)
        {
            var last = _undo[^1];
            if (last.Count == 1 && last[0].Kind == "type" && e.Tick - last[0].Tick < 900 && last[0].Removed.Length == 0 && e.Removed.Length == 0
                && last[0].Inserted.IndexOf('\n') < 0 && e.Start == last[0].End)
            {
                last[0].Inserted += e.Inserted; last[0].End = e.End; last[0].CaretAfter = e.CaretAfter; last[0].Tick = e.Tick;
                return;
            }
        }
        _undo.Add(new List<Edit> { e });
        if (_undo.Count > MaxUndo) _undo.RemoveRange(0, 200);
    }

    /// <summary>Group several edits into one undo step and one repaint.</summary>
    public void BeginGroup() { if (_groupDepth++ == 0) { _group = new List<Edit>(); _pendingMin = int.MaxValue; } }
    public void EndGroup()
    {
        if (--_groupDepth > 0) return;
        var g = _group; _group = null;
        if (g.Count > 0) { _undo.Add(g); if (_undo.Count > MaxUndo) _undo.RemoveRange(0, 200); }
        if (_pendingMin != int.MaxValue) { int m = _pendingMin; _pendingMin = int.MaxValue; Touched(m); }
    }

    void Touched(int line)
    {
        if (_groupDepth > 0) { _pendingMin = Math.Min(_pendingMin, line); return; }
        ResetLexing(line, immediate: false);
        _mapDirty = true; Modified = true; _caretOn = true;
        Squiggles.Clear();
        _caret = ClampPos(_caret); _anchor = ClampPos(_anchor);
        LayoutScroll(); EnsureCaretVisible(); UpdateBrace();
        Changed?.Invoke(this, EventArgs.Empty); CaretMoved?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    Pos ClampPos(Pos p) { int l = Math.Clamp(p.Line, 0, _lines.Count - 1); return new Pos(l, Math.Clamp(p.Column, 0, _lines[l].Length)); }

    public void Undo()
    {
        if (_undo.Count == 0 || ReadOnly) return;
        var g = _undo[^1]; _undo.RemoveAt(_undo.Count - 1);
        int min = int.MaxValue;
        for (int i = g.Count - 1; i >= 0; i--) { var e = g[i]; ApplyEdit(e.Start, EndOf(e.Start, e.Inserted), e.Removed); min = Math.Min(min, e.Start.Line); }
        _caret = g[0].CaretBefore; _anchor = g[0].AnchorBefore; _wantCol = _caret.Column;
        _redo.Add(g); AfterHistory(min);
    }

    public void Redo()
    {
        if (_redo.Count == 0 || ReadOnly) return;
        var g = _redo[^1]; _redo.RemoveAt(_redo.Count - 1);
        int min = int.MaxValue;
        foreach (var e in g) { ApplyEdit(e.Start, EndOf(e.Start, e.Removed), e.Inserted); min = Math.Min(min, e.Start.Line); }
        _caret = _anchor = g[^1].CaretAfter; _wantCol = _caret.Column;
        _undo.Add(g); AfterHistory(min);
    }

    void AfterHistory(int minLine)
    {

        Touched(minLine);
    }

    // ===================================================================
    //  Editing commands
    // ===================================================================
    void DeleteSelection() { if (HasSelection) Replace(SelStart, SelEnd, "", "del"); }

    /// <summary>Insert at the caret, replacing any selection.</summary>
    public void InsertText(string text, string kind = "edit") { if (HasSelection) Replace(SelStart, SelEnd, text, kind); else Replace(_caret, _caret, text, kind); }

    public void SelectAll() { _anchor = new Pos(0, 0); _caret = new Pos(_lines.Count - 1, _lines[^1].Length); AfterMove(true); }

    public void Copy()
    {
        try { Clipboard.SetText(HasSelection ? SelectedText : _lines[_caret.Line] + "\r\n"); } catch { }
    }
    public void Cut()
    {
        if (ReadOnly) { Copy(); return; }
        Copy();
        if (HasSelection) DeleteSelection(); else DeleteLines(_caret.Line, _caret.Line);
    }
    public void Paste()
    {
        if (ReadOnly) return;
        try { if (Clipboard.ContainsText()) { HidePopup(); InsertText(Clipboard.GetText(), "paste"); } } catch { }
    }

    void DeleteLines(int l1, int l2)
    {
        if (l2 < _lines.Count - 1) Replace(new Pos(l1, 0), new Pos(l2 + 1, 0), "", "dline");
        else if (l1 > 0) Replace(new Pos(l1 - 1, _lines[l1 - 1].Length), new Pos(l2, _lines[l2].Length), "", "dline");
        else Replace(new Pos(0, 0), new Pos(l2, _lines[l2].Length), "", "dline");
    }

    public void DeleteLine() { DeleteLines(SelStart.Line, SelEnd.Line); }

    public void DuplicateLine()
    {
        var a = SelStart; var b = SelEnd;
        var block = string.Join("\n", _lines.GetRange(a.Line, b.Line - a.Line + 1));
        var (c, an) = (_caret, _anchor);
        int d = b.Line - a.Line + 1;
        Replace(new Pos(b.Line, _lines[b.Line].Length), new Pos(b.Line, _lines[b.Line].Length), "\n" + block, "dup");
        _caret = new Pos(c.Line + d, c.Column); _anchor = new Pos(an.Line + d, an.Column); Invalidate();
    }

    public void MoveLines(int dir)
    {
        var a = SelStart; var b = SelEnd;
        if (dir < 0 && a.Line == 0) return;
        if (dir > 0 && b.Line >= _lines.Count - 1) return;
        var (c, an) = (_caret, _anchor);
        var block = _lines.GetRange(a.Line, b.Line - a.Line + 1);
        int from = dir < 0 ? a.Line - 1 : a.Line, to = dir < 0 ? b.Line : b.Line + 1;
        string other = _lines[dir < 0 ? a.Line - 1 : b.Line + 1];
        string text = dir < 0 ? string.Join("\n", block) + "\n" + other : other + "\n" + string.Join("\n", block);
        Replace(new Pos(from, 0), new Pos(to, _lines[to].Length), text, "move");
        _caret = new Pos(c.Line + dir, c.Column); _anchor = new Pos(an.Line + dir, an.Column); Invalidate();
    }

    /// <summary>Comment or uncomment the selected lines using the language's line-comment marker.</summary>
    public void ToggleComment()
    {
        string tok = _language.LineComment; if (tok == null) return;
        var a = SelStart; var b = SelEnd;
        if (b.Column == 0 && b.Line > a.Line) b = new Pos(b.Line - 1, 0);
        bool all = true; int minIndent = int.MaxValue;
        for (int i = a.Line; i <= b.Line; i++)
        {
            string l = _lines[i]; if (l.Trim().Length == 0) continue;
            if (!l.TrimStart().StartsWith(tok)) all = false;
            minIndent = Math.Min(minIndent, l.Length - l.TrimStart().Length);
        }
        if (minIndent == int.MaxValue) minIndent = 0;
        var res = new List<string>();
        for (int i = a.Line; i <= b.Line; i++)
        {
            string l = _lines[i];
            if (l.Trim().Length == 0) { res.Add(l); continue; }
            if (all) { int p = l.IndexOf(tok, StringComparison.Ordinal); int len = tok.Length + (p + tok.Length < l.Length && l[p + tok.Length] == ' ' ? 1 : 0); res.Add(l.Remove(p, len)); }
            else res.Add(l.Insert(minIndent, tok + " "));
        }
        var (an, c) = (_anchor, _caret);
        Replace(new Pos(a.Line, 0), new Pos(b.Line, _lines[b.Line].Length), string.Join("\n", res), "comment");
        int d = all ? -(tok.Length + 1) : tok.Length + 1;
        _anchor = new Pos(an.Line, Math.Max(0, an.Column + d)); _caret = new Pos(c.Line, Math.Max(0, c.Column + d)); Invalidate();
    }

    public void IndentSelection(bool outdent)
    {
        var a = SelStart; var b = SelEnd;
        if (b.Column == 0 && b.Line > a.Line) b = new Pos(b.Line - 1, 0);
        var res = new List<string>();
        for (int i = a.Line; i <= b.Line; i++)
        {
            string l = _lines[i];
            if (outdent) { int r = 0; while (r < IndentSize && r < l.Length && l[r] == ' ') r++; res.Add(l.Substring(r)); }
            else res.Add(l.Length == 0 ? l : new string(' ', IndentSize) + l);
        }
        var (an, c) = (_anchor, _caret);
        Replace(new Pos(a.Line, 0), new Pos(b.Line, _lines[b.Line].Length), string.Join("\n", res), "indent");
        int d = outdent ? -IndentSize : IndentSize;
        _anchor = new Pos(an.Line, Math.Max(0, an.Column + d)); _caret = new Pos(c.Line, Math.Max(0, c.Column + d)); Invalidate();
    }

    public void TrimTrailingWhitespace()
    {
        BeginGroup();
        for (int i = 0; i < _lines.Count; i++)
        {
            string l = _lines[i], t = l.TrimEnd();
            if (t.Length != l.Length) Replace(new Pos(i, t.Length), new Pos(i, l.Length), "", "trim");
        }
        EndGroup();
    }

    // ===================================================================
    //  Find / replace
    // ===================================================================
    string _findTerm = ""; bool _findCase;
    /// <summary>Highlight every match of <paramref name="term"/> in the visible text (pass "" to clear).</summary>
    public void SetFindHighlight(string term, bool matchCase) { _findTerm = term ?? ""; _findCase = matchCase; Invalidate(); }

    public bool FindNext(string term, bool matchCase, bool forward)
    {
        if (string.IsNullOrEmpty(term)) return false;
        var cmp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int n = _lines.Count;
        var from = forward ? SelEnd : SelStart;
        if (forward)
        {
            for (int k = 0; k <= n; k++)
            {
                int l = (from.Line + k) % n;
                int start = k == 0 ? from.Column : 0;
                if (start > _lines[l].Length) continue;
                int idx = _lines[l].IndexOf(term, start, cmp);
                if (idx >= 0) { Select(new Pos(l, idx), new Pos(l, idx + term.Length)); return true; }
            }
        }
        else
        {
            for (int k = 0; k <= n; k++)
            {
                int l = ((from.Line - k) % n + n) % n;
                string line = _lines[l];
                int limit = k == 0 ? from.Column : line.Length;
                if (limit - term.Length < 0 || line.Length == 0) continue;
                int idx = line.LastIndexOf(term, Math.Min(line.Length - 1, limit - 1), cmp);
                if (idx >= 0 && idx + term.Length <= limit) { Select(new Pos(l, idx), new Pos(l, idx + term.Length)); return true; }
            }
        }
        return false;
    }

    public bool ReplaceCurrent(string term, string repl, bool matchCase)
    {
        var cmp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        if (HasSelection && string.Equals(SelectedText, term, cmp)) Replace(SelStart, SelEnd, repl, "replace");
        return FindNext(term, matchCase, true);
    }

    public int ReplaceAll(string term, string repl, bool matchCase)
    {
        if (string.IsNullOrEmpty(term) || ReadOnly) return 0;
        var cmp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int count = 0;
        BeginGroup();
        for (int i = 0; i < _lines.Count; i++)
        {
            string l = _lines[i]; int p = 0; var sb = (StringBuilder)null; int n0 = count;
            while (true)
            {
                int idx = l.IndexOf(term, p, cmp);
                if (idx < 0) { if (sb != null) sb.Append(l, p, l.Length - p); break; }
                sb ??= new StringBuilder();
                sb.Append(l, p, idx - p).Append(repl); p = idx + term.Length; count++;
            }
            if (count != n0) Replace(new Pos(i, 0), new Pos(i, l.Length), sb.ToString(), "replaceall");
        }
        EndGroup();
        return count;
    }

    public int CountMatches(string term, bool matchCase)
    {
        if (string.IsNullOrEmpty(term)) return 0;
        var cmp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int c = 0;
        foreach (var l in _lines) { int p = 0; while ((p = l.IndexOf(term, p, cmp)) >= 0) { c++; p += term.Length; } }
        return c;
    }

    public void Select(Pos a, Pos b) { _anchor = ClampPos(a); _caret = ClampPos(b); _wantCol = _caret.Column; AfterMove(true); }

    /// <summary>Move the caret to a zero-based line and column and scroll it into view.</summary>
    public void GoTo(int line, int col = 0)
    {
        line = Math.Clamp(line, 0, _lines.Count - 1); col = Math.Clamp(col, 0, _lines[line].Length);
        _caret = _anchor = new Pos(line, col); _wantCol = col;
        AfterMove(false);
        int vis = Math.Max(1, VisLines);
        if (line < _vs.Value || line >= _vs.Value + vis) SetFirstLine(line - vis / 3);
    }

    public string CurrentWord()
    {
        var l = _lines[_caret.Line]; int s = _caret.Column, e = _caret.Column;
        while (s > 0 && IsWord(l[s - 1])) s--;
        while (e < l.Length && IsWord(l[e])) e++;
        return l.Substring(s, e - s);
    }

    static bool IsWord(char c) => char.IsLetterOrDigit(c) || c == '_';
}
