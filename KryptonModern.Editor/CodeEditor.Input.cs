// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
namespace KryptonModern.Editor;

public sealed partial class CodeEditor
{
    // ---- completion state -----------------------------------------------
    readonly List<CompletionItem> _items = new(); int _sel, _itemTop; bool _popup; string _prefix = "";

    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); StartLexing(); }

    // ===================================================================
    //  Caret movement
    // ===================================================================
    void AfterMove(bool keepWant)
    {
        if (!keepWant) _wantCol = _caret.Column;
        _caretOn = true; _blink.Stop(); if (Focused) _blink.Start();
        EnsureCaretVisible(); UpdateBrace();
        CaretMoved?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    void MoveTo(Pos p, bool extend, bool keepWant = false)
    {
        _caret = p; if (!extend) _anchor = p;
        if (!keepWant) _wantCol = p.Column;
        AfterMove(true);
        HidePopup();
    }

    Pos WordLeft(Pos p)
    {
        if (p.Column == 0) return p.Line > 0 ? new Pos(p.Line - 1, _lines[p.Line - 1].Length) : p;
        var s = _lines[p.Line]; int c = p.Column;
        while (c > 0 && char.IsWhiteSpace(s[c - 1])) c--;
        if (c > 0 && IsWord(s[c - 1])) { while (c > 0 && IsWord(s[c - 1])) c--; } else if (c > 0) c--;
        return new Pos(p.Line, c);
    }

    Pos WordRight(Pos p)
    {
        var s = _lines[p.Line];
        if (p.Column >= s.Length) return p.Line < _lines.Count - 1 ? new Pos(p.Line + 1, 0) : p;
        int c = p.Column;
        if (IsWord(s[c])) { while (c < s.Length && IsWord(s[c])) c++; } else c++;
        while (c < s.Length && char.IsWhiteSpace(s[c])) c++;
        return new Pos(p.Line, c);
    }

    // ===================================================================
    //  Keyboard
    // ===================================================================
    protected override bool IsInputKey(Keys k)
    {
        var code = k & Keys.KeyCode;
        if (code == Keys.Tab && (k & Keys.Control) != 0) return false;
        switch (code)
        {
            case Keys.Tab: case Keys.Enter: case Keys.Up: case Keys.Down: case Keys.Left: case Keys.Right:
            case Keys.Home: case Keys.End: case Keys.PageUp: case Keys.PageDown: return true;
            case Keys.Escape: return _popup;
        }
        return base.IsInputKey(k);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        bool ctrl = e.Control, shift = e.Shift, alt = e.Alt;
        var k = e.KeyCode;

        if (_popup)
        {
            if (k == Keys.Down) { _sel = Math.Min(_sel + 1, _items.Count - 1); KeepSelVisible(); Invalidate(); e.Handled = e.SuppressKeyPress = true; return; }
            if (k == Keys.Up) { _sel = Math.Max(_sel - 1, 0); KeepSelVisible(); Invalidate(); e.Handled = e.SuppressKeyPress = true; return; }
            if (k is Keys.Enter or Keys.Tab) { AcceptCompletion(); e.Handled = e.SuppressKeyPress = true; return; }
            if (k == Keys.Escape) { HidePopup(); e.Handled = e.SuppressKeyPress = true; return; }
        }

        e.Handled = true;
        int L = _caret.Line, C = _caret.Column;
        switch (k)
        {
            case Keys.Left:
                if (!shift && HasSelection && !ctrl) MoveTo(SelStart, false);
                else if (ctrl) MoveTo(WordLeft(_caret), shift);
                else MoveTo(C > 0 ? new Pos(L, C - 1) : L > 0 ? new Pos(L - 1, _lines[L - 1].Length) : _caret, shift);
                break;
            case Keys.Right:
                if (!shift && HasSelection && !ctrl) MoveTo(SelEnd, false);
                else if (ctrl) MoveTo(WordRight(_caret), shift);
                else MoveTo(C < _lines[L].Length ? new Pos(L, C + 1) : L < _lines.Count - 1 ? new Pos(L + 1, 0) : _caret, shift);
                break;
            case Keys.Up:
                if (alt && !ctrl) MoveLines(-1);
                else if (ctrl && !shift) SetFirstLine(First - 1);
                else if (L > 0) MoveTo(ClampPos(new Pos(L - 1, _wantCol)), shift, true); else MoveTo(new Pos(0, 0), shift);
                break;
            case Keys.Down:
                if (alt && !ctrl) MoveLines(1);
                else if (ctrl && !shift) SetFirstLine(First + 1);
                else if (L < _lines.Count - 1) MoveTo(ClampPos(new Pos(L + 1, _wantCol)), shift, true); else MoveTo(new Pos(L, _lines[L].Length), shift);
                break;
            case Keys.PageUp: { int v = VisLines - 1; SetFirstLine(First - v); MoveTo(ClampPos(new Pos(L - v, _wantCol)), shift, true); break; }
            case Keys.PageDown: { int v = VisLines - 1; SetFirstLine(First + v); MoveTo(ClampPos(new Pos(L + v, _wantCol)), shift, true); break; }
            case Keys.Home:
                if (ctrl) MoveTo(new Pos(0, 0), shift);
                else { var s = _lines[L]; int fn = s.Length - s.TrimStart().Length; MoveTo(new Pos(L, C == fn ? 0 : fn), shift); }
                break;
            case Keys.End:
                if (ctrl) MoveTo(new Pos(_lines.Count - 1, _lines[^1].Length), shift);
                else MoveTo(new Pos(L, _lines[L].Length), shift);
                break;
            case Keys.Back:
                if (HasSelection) DeleteSelection();
                else if (ctrl) { var w = WordLeft(_caret); if (w != _caret) Replace(w, _caret, "", "del"); }
                else if (C > 0)
                {
                    var s = _lines[L]; char prev = s[C - 1]; char next = C < s.Length ? s[C] : '\0';
                    bool pair = _language.AutoCloseBrackets && ((prev == '(' && next == ')') || (prev == '[' && next == ']') || (prev == '{' && next == '}') || (prev == '"' && next == '"') || (prev == '\'' && next == '\''));
                    Replace(new Pos(L, C - 1), new Pos(L, C + (pair ? 1 : 0)), "", "back");
                }
                else if (L > 0) Replace(new Pos(L - 1, _lines[L - 1].Length), _caret, "", "back");
                HidePopup();
                break;
            case Keys.Delete:
                if (HasSelection) DeleteSelection();
                else if (ctrl) { var w = WordRight(_caret); if (w != _caret) Replace(_caret, w, "", "del"); }
                else if (C < _lines[L].Length) Replace(_caret, new Pos(L, C + 1), "", "del");
                else if (L < _lines.Count - 1) Replace(_caret, new Pos(L + 1, 0), "", "del");
                break;
            case Keys.Enter: NewLine(); break;
            case Keys.Tab:
                if (HasSelection && SelStart.Line != SelEnd.Line) IndentSelection(shift);
                else if (shift) IndentSelection(true);
                else InsertText(new string(' ', IndentSize - (C % IndentSize)), "tab");
                break;
            case Keys.Escape: if (HasSelection) MoveTo(_caret, false); else e.Handled = false; break;
            case Keys.A when ctrl: SelectAll(); break;
            case Keys.C when ctrl: Copy(); break;
            case Keys.X when ctrl: Cut(); break;
            case Keys.V when ctrl: Paste(); break;
            case Keys.Z when ctrl: Undo(); break;
            case Keys.Y when ctrl: Redo(); break;
            case Keys.D when ctrl && !shift: DuplicateLine(); break;
            case Keys.L when ctrl && shift: DeleteLine(); break;
            case Keys.OemQuestion when ctrl: ToggleComment(); break;
            case Keys.Space when ctrl: ShowCompletion(true); break;
            case Keys.Insert when !ctrl && !shift: Overwrite = !Overwrite; CaretMoved?.Invoke(this, EventArgs.Empty); break;
            case Keys.Insert when shift: Paste(); break;
            default: e.Handled = false; break;
        }
        if (e.Handled) e.SuppressKeyPress = true;
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        char c = e.KeyChar;
        if (c < 32 || c == 127) return;
        e.Handled = true;
        if (!ReadOnly) TypeChar(c);
    }

    void TypeChar(char c)
    {
        var s = _lines[_caret.Line];
        char next = _caret.Column < s.Length ? s[_caret.Column] : '\0';
        char prev = _caret.Column > 0 ? s[_caret.Column - 1] : '\0';
        bool auto = _language.AutoCloseBrackets;

        if (!HasSelection && auto && c is ')' or ']' or '}' or '"' or '\'' && next == c) { MoveTo(new Pos(_caret.Line, _caret.Column + 1), false); return; }
        if (auto && !HasSelection && c is '(' or '[' or '{' or '"' or '\'')
        {
            string closer = c switch { '(' => ")", '[' => "]", '{' => "}", _ => c.ToString() };
            bool ok = next == '\0' || char.IsWhiteSpace(next) || next is ')' or ']' or '}' or ';' or ',';
            if ((c is '"' or '\'') && (IsWord(prev) || prev == c)) ok = false;
            if (ok)
            {
                Replace(_caret, _caret, c + closer, "type");
                _caret = _anchor = new Pos(_caret.Line, _caret.Column - 1); _wantCol = _caret.Column; AfterMove(true); return;
            }
        }
        if (c == '}' && auto && !HasSelection && _caret.Column >= IndentSize && s.AsSpan(0, _caret.Column).Trim(' ').Length == 0)
        {
            Replace(new Pos(_caret.Line, _caret.Column - IndentSize), _caret, "}", "type"); return;
        }
        if (Overwrite && !HasSelection && _caret.Column < s.Length) Replace(_caret, new Pos(_caret.Line, _caret.Column + 1), c.ToString(), "type");
        else InsertText(c.ToString(), "type");

        if (IsWord(c)) ShowCompletion(false); else HidePopup();
    }

    void NewLine()
    {
        HidePopup();
        var a = SelStart; var b = SelEnd;
        string line = _lines[a.Line];
        string before = line.Substring(0, a.Column), after = _lines[b.Line].Substring(b.Column);
        int indent = Math.Min(line.Length - line.TrimStart().Length, a.Column);
        string ind = new string(' ', indent), step = new string(' ', IndentSize);
        if (_language.ShouldSplit(before, after))
        {
            Replace(a, b, "\n" + ind + step + "\n" + ind, "enter");
            _caret = _anchor = new Pos(a.Line + 1, indent + IndentSize); _wantCol = _caret.Column; AfterMove(true);
        }
        else Replace(a, b, "\n" + ind + (_language.ShouldIndent(before) ? step : ""), "enter");
    }

    // ===================================================================
    //  Completion
    // ===================================================================
    void ShowCompletion(bool force)
    {
        var provider = CompletionProvider;
        if (provider == null || _language.Name is "Plain text" or "Markdown") { HidePopup(); return; }
        var s = _lines[_caret.Line]; int st = _caret.Column;
        while (st > 0 && IsWord(s[st - 1])) st--;
        string prefix = s.Substring(st, _caret.Column - st);
        if (prefix.Length < (force ? 0 : 2) || (prefix.Length > 0 && char.IsDigit(prefix[0]))) { HidePopup(); return; }
        var toks = TokensOf(_caret.Line, _tokBuf);
        if (_caret.Column > 0 && IsStringOrComment(toks, _caret.Column - 1)) { HidePopup(); return; }
        var items = provider.GetItems(this, prefix, force);
        if (items == null || items.Count == 0) { HidePopup(); return; }
        _items.Clear(); _items.AddRange(items); _sel = 0; _itemTop = 0; _prefix = prefix; _popup = true; Invalidate();
    }

    void HidePopup() { if (_popup) { _popup = false; Invalidate(); } }
    void KeepSelVisible() { if (_sel < _itemTop) _itemTop = _sel; if (_sel >= _itemTop + 8) _itemTop = _sel - 7; }

    void AcceptCompletion()
    {
        if (!_popup || _items.Count == 0) { HidePopup(); return; }
        string w = _items[_sel].Text; _popup = false;
        Replace(new Pos(_caret.Line, _caret.Column - _prefix.Length), _caret, w, "complete");
    }

    Rectangle PopupRect()
    {
        int rows = Math.Min(8, _items.Count);
        int w = Math.Min(380, (int)Math.Max(180, (_items.Max(x => x.Text.Length) + 6) * _cw + 28));
        int x = XOf(_caret.Column - _prefix.Length), y = YOf(_caret.Line) + _lh;
        int h = rows * _lh + 4;
        if (y + h > ViewH) y = YOf(_caret.Line) - h;
        if (x + w > TextRight) x = TextRight - w;
        return new Rectangle(Math.Max(GutterW, x), y, w, h);
    }

    // ===================================================================
    //  Mouse
    // ===================================================================
    Pos HitTest(Point p)
    {
        int l = Math.Clamp(First + Math.Max(0, p.Y) / _lh, 0, _lines.Count - 1);
        int c = (int)Math.Round((p.X - GutterW + ScrollX) / _cw);
        return new Pos(l, Math.Clamp(c, 0, _lines[l].Length));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button != MouseButtons.Left) return;
        if (_popup)
        {
            var pr = PopupRect();
            if (pr.Contains(e.Location)) { _sel = Math.Clamp(_itemTop + (e.Y - pr.Y - 2) / _lh, 0, _items.Count - 1); AcceptCompletion(); return; }
            HidePopup();
        }
        if (MapW > 0 && e.X >= TextRight && e.X < TextRight + MapW) { ScrollFromMap(e.Y); _dragging = false; _mapDrag = true; Capture = true; return; }
        if (e.X < 18)
        {
            int l = First + e.Y / _lh;
            if (l < _lines.Count) { if (!Breakpoints.Remove(l)) Breakpoints.Add(l); _mapDirty = true; BreakpointsChanged?.Invoke(this, EventArgs.Empty); Invalidate(); }
            return;
        }
        var p = HitTest(e.Location);
        if (e.Clicks == 2)
        {
            var s = _lines[p.Line]; int a = p.Column, b = p.Column;
            while (a > 0 && IsWord(s[a - 1])) a--;
            while (b < s.Length && IsWord(s[b])) b++;
            _anchor = new Pos(p.Line, a); _caret = new Pos(p.Line, b); AfterMove(false); return;
        }
        if (e.Clicks >= 3) { _anchor = new Pos(p.Line, 0); _caret = new Pos(p.Line, _lines[p.Line].Length); AfterMove(false); return; }
        _dragging = true; Capture = true;
        MoveTo(p, ModifierKeys.HasFlag(Keys.Shift));
    }

    void ScrollFromMap(int y) => SetFirstLine(_mapFirst + y / 2 - VisLines / 2);

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_mapDrag) { ScrollFromMap(e.Y); return; }
        if (_dragging)
        {
            var p = HitTest(e.Location);
            if (e.Y < 0) p = new Pos(Math.Max(0, First - 1), 0); else if (e.Y > ViewH) p = ClampPos(new Pos(First + VisLines + 1, p.Column));
            _caret = p; AfterMove(true);
            return;
        }
        bool inText = e.X >= GutterW && e.X < TextRight;
        Cursor = inText ? Cursors.IBeam : Cursors.Arrow;
        string tip = null;
        if (inText && Squiggles.TryGetValue(First + e.Y / _lh, out var sq)) tip = sq.Message;
        if (tip != _tipText) { _tipText = tip; if (tip == null) _tip.Hide(this); else _tip.Show(tip, this, e.X + 12, e.Y + 18, 6000); }
    }

    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _dragging = false; _mapDrag = false; Capture = false; }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _tipText = null; _tip.Hide(this); }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (ModifierKeys.HasFlag(Keys.Control)) { EditorFontSize += e.Delta > 0 ? 1 : -1; return; }
        if (ModifierKeys.HasFlag(Keys.Shift)) { _hs.Value = Math.Clamp(_hs.Value - Math.Sign(e.Delta) * 60, 0, Math.Max(0, _hs.Maximum - _hs.LargeChange + 1)); return; }
        SetFirstLine(First - Math.Sign(e.Delta) * 3);
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); _caretOn = true; _blink.Start(); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); _blink.Stop(); HidePopup(); Invalidate(); }
}
