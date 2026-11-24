// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
namespace Fungusware.WinFormsThemes.Editor;

public sealed partial class CodeEditor
{
    // ---- view -----------------------------------------------------------
    readonly VScrollBar _vs = new() { Dock = DockStyle.Right, TabStop = false };
    readonly HScrollBar _hs = new() { Dock = DockStyle.Bottom, TabStop = false };
    readonly System.Windows.Forms.Timer _blink = new() { Interval = 530 };
    readonly System.Windows.Forms.Timer _lexDebounce = new() { Interval = 60 };
    readonly ToolTip _tip = new();
    Font _font; float _cw; int _lh; float _fontSize = 10.5f; string _fontFamily = "Cascadia Mono";
    bool _showMinimap = true, _showLineNumbers = true, _showGuides = true;
    bool _caretOn = true, _dragging, _mapDrag;
    string _tipText;
    ILanguage _language = Languages.Plain;
    EditorTheme _theme = EditorTheme.Dark;
    Pos? _braceA, _braceB;

    public CodeEditor()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.Opaque, true);
        TabStop = true; Cursor = Cursors.IBeam;
        Controls.Add(_hs); Controls.Add(_vs);
        _vs.ValueChanged += (_, _) => Invalidate();
        _hs.ValueChanged += (_, _) => Invalidate();
        _blink.Tick += (_, _) => { _caretOn = !_caretOn; Invalidate(CaretRect()); };
        _lexDebounce.Tick += (_, _) => { _lexDebounce.Stop(); StartLexing(); };
        BackColor = _theme.Background;
        RebuildFont();
        _tip.InitialDelay = 300;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _lexCts?.Cancel(); _blink.Dispose(); _lexDebounce.Dispose(); _tip.Dispose(); _font?.Dispose(); _map?.Dispose(); }
        base.Dispose(disposing);
    }

    // ---- appearance -----------------------------------------------------
    public EditorTheme Theme { get => _theme; set { _theme = value ?? EditorTheme.Dark; BackColor = _theme.Background; _mapDirty = true; Invalidate(); } }

    public ILanguage Language
    {
        get => _language;
        set { _language = value ?? Languages.Plain; ResetLexing(0, immediate: true); _mapDirty = true; UpdateBrace(); Invalidate(); }
    }

    public bool ShowMinimap { get => _showMinimap; set { _showMinimap = value; _mapDirty = true; LayoutScroll(); Invalidate(); } }
    public bool ShowLineNumbers { get => _showLineNumbers; set { _showLineNumbers = value; Invalidate(); } }
    public bool ShowIndentGuides { get => _showGuides; set { _showGuides = value; Invalidate(); } }
    public float EditorFontSize { get => _fontSize; set { _fontSize = Math.Clamp(value, 7f, 40f); RebuildFont(); } }
    /// <summary>Monospaced family to use. Falls back to Consolas if it is not installed.</summary>
    public string EditorFontFamily { get => _fontFamily; set { _fontFamily = value; RebuildFont(); } }
    /// <summary>Provides the completion list. Set to null to turn completion off.</summary>
    public ICompletionProvider CompletionProvider { get; set; } = new WordCompletionProvider();

    // ---- metrics --------------------------------------------------------
    static readonly TextFormatFlags Tff = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.NoClipping | TextFormatFlags.Left;

    void RebuildFont()
    {
        _font?.Dispose();
        _font = new Font(_fontFamily, _fontSize);
        if (!string.Equals(_font.Name, _fontFamily, StringComparison.OrdinalIgnoreCase)) { _font.Dispose(); _font = new Font("Consolas", _fontSize + 0.5f); }
        var sz = TextRenderer.MeasureText(new string('M', 40), _font, new Size(int.MaxValue, int.MaxValue), Tff);
        _cw = sz.Width / 40f;
        _lh = Math.Max(14, TextRenderer.MeasureText("Ag", _font, new Size(int.MaxValue, int.MaxValue), Tff).Height + 3);
        _mapDirty = true;
        LayoutScroll(); Invalidate();
    }

    int MapW => _showMinimap && ClientSize.Width > 520 ? 100 : 0;
    int GutterW => 20 + (_showLineNumbers ? (int)Math.Ceiling(Math.Max(3, _lines.Count.ToString().Length) * _cw) + 14 : 4);
    int ViewH => Math.Max(1, ClientSize.Height - (_hs.Visible ? _hs.Height : 0));
    int TextRight => ClientSize.Width - (_vs.Visible ? _vs.Width : 0) - MapW;
    int ViewW => Math.Max(1, TextRight - GutterW);
    int VisLines => Math.Max(1, ViewH / _lh);
    int First => _vs.Value;
    int ScrollX => _hs.Value;
    int XOf(int col) => GutterW + (int)(col * _cw) - ScrollX;
    int YOf(int line) => (line - First) * _lh;
    Rectangle CaretRect() => new(XOf(_caret.Column) - 2, YOf(_caret.Line), 6, _lh);

    void LayoutScroll()
    {
        if (_maxDirty || _maxStale > 400) { _maxLen = 0; foreach (var l in _lines) if (l.Length > _maxLen) _maxLen = l.Length; _maxDirty = false; _maxStale = 0; }
        int vis = VisLines;
        _vs.Minimum = 0; _vs.SmallChange = 1; _vs.LargeChange = vis;
        _vs.Maximum = Math.Max(0, _lines.Count + vis - 2);
        if (_vs.Value > _lines.Count - 1) _vs.Value = Math.Max(0, _lines.Count - 1);
        _hs.Minimum = 0; _hs.SmallChange = Math.Max(1, (int)_cw * 4); _hs.LargeChange = ViewW;
        _hs.Maximum = Math.Max(0, (int)(_maxLen * _cw) + ViewW - 20);
        if (_hs.Value > _hs.Maximum - _hs.LargeChange + 1) _hs.Value = Math.Max(0, _hs.Maximum - _hs.LargeChange + 1);
        if (_hs.Maximum < _hs.LargeChange) _hs.Value = 0;
    }

    void SetFirstLine(int v) { _vs.Value = Math.Clamp(v, _vs.Minimum, Math.Max(_vs.Minimum, _vs.Maximum - _vs.LargeChange + 1)); }

    void EnsureCaretVisible()
    {
        int vis = VisLines;
        if (_caret.Line < First) SetFirstLine(_caret.Line);
        else if (_caret.Line >= First + vis) SetFirstLine(_caret.Line - vis + 1);
        int cx = (int)(_caret.Column * _cw);
        if (cx < ScrollX) _hs.Value = Math.Max(0, cx - 40);
        else if (cx > ScrollX + ViewW - 20) _hs.Value = Math.Min(Math.Max(0, _hs.Maximum - _hs.LargeChange + 1), cx - ViewW + 60);
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); _mapDirty = true; LayoutScroll(); }

    // ===================================================================
    //  Lexing: one int of state per line, computed on a worker thread
    // ===================================================================
    readonly List<int> _endStates = new();   // end state of line i, valid for i < _validCount
    int _validCount, _lexVersion;
    CancellationTokenSource _lexCts;
    readonly List<Token> _tokBuf = new(), _scratch = new();

    /// <summary>Forget states from <paramref name="fromLine"/> down and start recomputing them in the background.</summary>
    void ResetLexing(int fromLine, bool immediate)
    {
        _lexVersion++;
        _lexCts?.Cancel();
        _validCount = Math.Min(_validCount, fromLine);
        if (_endStates.Count > _validCount) _endStates.RemoveRange(_validCount, _endStates.Count - _validCount);
        if (immediate) { _lexDebounce.Stop(); StartLexing(); }
        else { _lexDebounce.Stop(); _lexDebounce.Start(); }
    }

    void StartLexing()
    {
        if (_validCount >= _lines.Count || IsDisposed) return;
        var cts = _lexCts = new CancellationTokenSource();
        var token = cts.Token;
        var lang = _language; int from = _validCount, ver = _lexVersion;
        int state = from == 0 ? 0 : _endStates[from - 1];
        var snapshot = _lines.ToArray();   // copies references only; the worker never touches the live list
        Task.Run(() =>
        {
            var tmp = new List<Token>(); var chunk = new List<int>(4096); int chunkStart = from;
            for (int i = from; i < snapshot.Length; i++)
            {
                if (token.IsCancellationRequested) return;
                tmp.Clear(); state = lang.Tokenize(snapshot[i], state, tmp); chunk.Add(state);
                if (chunk.Count == 4096 || i == snapshot.Length - 1)
                {
                    var arr = chunk.ToArray(); int start = chunkStart; chunk.Clear(); chunkStart = i + 1;
                    try { if (IsHandleCreated && !IsDisposed) BeginInvoke(() => Deliver(ver, start, arr)); } catch { return; }
                }
            }
        }, token);
    }

    void Deliver(int ver, int start, int[] chunk)
    {
        if (ver != _lexVersion || start > _validCount) return;
        int skip = _validCount - start; if (skip >= chunk.Length) return;
        for (int i = skip; i < chunk.Length; i++) _endStates.Add(chunk[i]);
        _validCount += chunk.Length - skip;
        _mapDirty = true; Invalidate();
    }

    /// <summary>Lexer state at the start of a line. If the background pass has not got that far, extend it here (bounded), otherwise approximate with 0.</summary>
    int StartStateOf(int line)
    {
        if (line == 0) return 0;
        if (line - 1 < _validCount) return _endStates[line - 1];
        if (line - _validCount > 6000) return 0;
        while (_validCount < line)
        {
            int st = _validCount == 0 ? 0 : _endStates[_validCount - 1];
            _scratch.Clear();
            _endStates.Add(_language.Tokenize(_lines[_validCount], st, _scratch));
            _validCount++;
        }
        return _endStates[line - 1];
    }

    /// <summary>Tokens for one line, computed on demand (cheap for a single line).</summary>
    List<Token> TokensOf(int line, List<Token> into)
    {
        into.Clear();
        _language.Tokenize(_lines[line], StartStateOf(line), into);
        return into;
    }

    bool IsStringOrComment(List<Token> toks, int col)
    {
        foreach (var t in toks) if (col >= t.Start && col < t.Start + t.Length) return t.Kind is TokenKind.String or TokenKind.Comment;
        return false;
    }

    // ===================================================================
    //  Brace matching
    // ===================================================================
    void UpdateBrace()
    {
        _braceA = _braceB = null;
        if (_language.Name == "Plain text" || _language.Name == "Markdown") return;
        var line = _lines[_caret.Line];
        int c = _caret.Column, idx = -1;
        const string B = "()[]{}";
        if (c < line.Length && B.IndexOf(line[c]) >= 0) idx = c;
        else if (c > 0 && c - 1 < line.Length && B.IndexOf(line[c - 1]) >= 0) idx = c - 1;
        if (idx < 0) return;
        char ch = line[idx]; int pi = B.IndexOf(ch);
        char other = B[pi ^ 1]; bool fwd = pi % 2 == 0;
        var toks = new List<Token>();
        if (IsStringOrComment(TokensOf(_caret.Line, toks), idx)) return;
        int depth = 0, scanned = 0, l = _caret.Line, col = idx;
        while (l >= 0 && l < _lines.Count && scanned < 4000)
        {
            string s = _lines[l];
            TokensOf(l, toks);
            for (; fwd ? col < s.Length : col >= 0; col += fwd ? 1 : -1)
            {
                char x = s[col];
                if (x != ch && x != other) continue;
                if (IsStringOrComment(toks, col)) continue;
                if (x == ch) depth++; else depth--;
                if (depth == 0) { _braceA = new Pos(_caret.Line, idx); _braceB = new Pos(l, col); return; }
            }
            l += fwd ? 1 : -1; scanned++;
            if (l >= 0 && l < _lines.Count) col = fwd ? 0 : _lines[l].Length - 1;
        }
    }
}

/// <summary>Default completion: the language's keywords plus identifiers found in the document. The document scan is cached and refreshed every few dozen edits.</summary>
public sealed class WordCompletionProvider : ICompletionProvider
{
    HashSet<string> _words = new(); int _cachedVersion = int.MinValue; int _cachedLines;
    public int MinWordLength { get; set; } = 3;

    public IReadOnlyList<CompletionItem> GetItems(CodeEditor ed, string prefix, bool forced)
    {
        if (ed.Version - _cachedVersion > 150 || _cachedVersion == int.MinValue || ed.LineCount < _cachedLines / 2) { Rescan(ed); }
        var result = new List<CompletionItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var k in ed.Language.Keywords) if (k.Length > prefix.Length && k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && seen.Add(k)) result.Add(new CompletionItem(k, TokenKind.Keyword));
        var current = new HashSet<string>(); Collect(ed.GetLine(ed.Caret.Line), current);
        foreach (var w in _words.Concat(current))
            if (w.Length > prefix.Length && w.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && seen.Add(w)) result.Add(new CompletionItem(w, TokenKind.Type));
        result.Sort((x, y) =>
        {
            bool kx = x.Kind == TokenKind.Keyword, ky = y.Kind == TokenKind.Keyword;
            return kx != ky ? (kx ? 1 : -1) : string.Compare(x.Text, y.Text, StringComparison.OrdinalIgnoreCase);
        });
        if (result.Count > 60) result.RemoveRange(60, result.Count - 60);
        return result;
    }

    void Rescan(CodeEditor ed)
    {
        var set = new HashSet<string>();
        int n = ed.LineCount, from = Math.Max(0, ed.Caret.Line - 2500), to = Math.Min(n, from + 5000);
        for (int i = from; i < to; i++) Collect(ed.GetLine(i), set);
        _words = set; _cachedVersion = ed.Version; _cachedLines = n;
    }

    void Collect(string line, HashSet<string> set)
    {
        int i = 0, n = line.Length;
        while (i < n)
        {
            if (char.IsLetter(line[i]) || line[i] == '_')
            {
                int j = i + 1; while (j < n && (char.IsLetterOrDigit(line[j]) || line[j] == '_')) j++;
                if (j - i >= MinWordLength && set.Count < 50000) set.Add(line.Substring(i, j - i));
                i = j;
            }
            else i++;
        }
    }
}
