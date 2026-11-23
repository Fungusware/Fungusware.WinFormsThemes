// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using System.Text;
using System.Xml.Linq;
using Krypton.Docking;
using Krypton.Navigator;
using Krypton.Toolkit;
using Krypton.Workspace;
using KryptonModern.Editor;

namespace KryptonModern.VsClone;

/// <summary>
/// "Krypton Studio": a Visual Studio-style IDE built only from Krypton Toolkit controls + KryptonModern themes.
/// Open with KM_FORM=4 (or the launcher).
/// </summary>
public partial class VsCloneForm : KryptonForm
{
    sealed class Doc { public KryptonPage Page; public DocumentView View; public string Path; public bool Bom; }

    readonly KryptonManager _manager = new();
    KryptonCustomPaletteBase _palette;
    Tokens _t = ModernTheme.Dark;
    readonly StudioSettings _cfg = StudioSettings.Load();

    // chrome
    KryptonPanel _top; MenuStrip _menu; ToolStrip _tool; StatusBarEx _status; KryptonTextBox _quick; ListBox _quickList;
    StatusBarEx.Item _sReady, _sLn, _sCol, _sCh, _sSpc, _sEol, _sEnc, _sBranch, _sProj;

    // docking
    KryptonDockingManager _dm; KryptonDockableWorkspace _ws; KryptonPanel _host;
    byte[] _defaultLayout;
    readonly Dictionary<string, Doc> _docs = new(StringComparer.OrdinalIgnoreCase);
    readonly List<(KryptonPage page, char glyph)> _pageGlyphs = new();
    readonly List<(ToolStripItem item, char glyph)> _glyphItems = new();
    KryptonPage _pgSolution, _pgProps, _pgErrors, _pgOutput, _pgToolbox, _pgGit, _pgFind, _pgTerminal, _pgStart;

    // solution state
    string _slnPath, _rootDir, _startup;
    readonly List<string> _projects = new();

    public VsCloneForm()
    {
        Text = "Krypton Studio";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1480, 900);
        AutoScaleMode = AutoScaleMode.Dpi;
        KeyPreview = true;

        int ti = ModernTheme.All.ToList().FindIndex(x => x.Name == _cfg.Theme);
        if (int.TryParse(Environment.GetEnvironmentVariable("KM_THEME"), out var env) && env >= 0 && env < ModernTheme.All.Length) ti = env;
        _t = ModernTheme.All[Math.Max(0, ti < 0 ? 1 : ti)];

        BuildMenu();
        BuildToolbar();
        BuildStatus();
        BuildDocking();
        BuildQuickLaunch();

        // outermost first
        var order = new Control[] { _top, _tool, _status, _host };
        foreach (var c in order) Controls.Add(c);
        for (int i = 0; i < order.Length; i++) Controls.SetChildIndex(order[i], order.Length - 1 - i);

        Shown += (_, _) =>
        {
            ApplyTheme(_t);
            var sln = Environment.GetEnvironmentVariable("KS_SOLUTION") ?? FindDefaultSolution();
            OpenStartPage();
            if (sln != null) LoadSolution(sln);
            SizeDockspaces();
            if (Environment.GetEnvironmentVariable("KS_BUILD") != null) BeginInvoke(async () => await BuildAsync(false, false));
            _defaultLayout = SafeSaveLayout();
            if (Environment.GetEnvironmentVariable("KS_OPEN") is { Length: > 0 } open) foreach (var f in open.Split(';')) OpenFile(Path.IsPathRooted(f) ? f : Path.Combine(_rootDir ?? "", f));
        };
    }

    static string FindDefaultSolution()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
        {
            var f = d.GetFiles("*.slnx").Concat(d.GetFiles("*.sln")).FirstOrDefault();
            if (f != null) return f.FullName;
        }
        return null;
    }

    void SizeDockspaces()
    {
        float k = DeviceDpi / 96f;
        void Rec(Control c)
        {
            foreach (Control ch in c.Controls)
            {
                if (ch.GetType().Name == "KryptonDockspace")
                {
                    if (ch.Dock == DockStyle.Right || ch.Dock == DockStyle.Left)
                    {
                        ch.Width = (int)(380 * k);
                        if (ch is KryptonWorkspace kw) { var c1 = kw.FirstCell(); var c2 = c1 != null ? kw.NextCell(c1) : null; if (c2 != null) { c1.StarSize = "50*,62*"; c2.StarSize = "50*,38*"; } }
                    }
                    else if (ch.Dock == DockStyle.Bottom || ch.Dock == DockStyle.Top) ch.Height = (int)(270 * k);
                }
                Rec(ch);
            }
        }
        Rec(_host);
        _host.PerformLayout();
    }


    byte[] SafeSaveLayout() { try { return _dm.SaveConfigToArray(); } catch { return null; } }

    // ===================================================================
    //  Docking
    // ===================================================================
    void BuildDocking()
    {
        _host = new KryptonPanel { Dock = DockStyle.Fill };
        _ws = new KryptonDockableWorkspace { Dock = DockStyle.Fill };
        _host.Controls.Add(_ws);
        _dm = new KryptonDockingManager();
        var w = _dm.ManageWorkspace("Workspace", _ws);
        _dm.ManageControl("Control", _host, w);
        _dm.ManageFloating(this);

        _pgToolbox = Tool("Toolbox", Ico.Toolbox, "toolbox", BuildToolbox(), new Size(260, 400));
        _pgSolution = Tool("Solution Explorer", Ico.List, "solution", BuildSolutionExplorer(), new Size(340, 420));
        _pgGit = Tool("Git Changes", Ico.Git, "git", BuildGitPanel(), new Size(340, 420));
        _pgProps = Tool("Properties", Ico.Properties, "props", BuildProperties(), new Size(340, 300));
        _pgErrors = Tool("Error List", Ico.Error, "errors", BuildErrorList(), new Size(900, 250));
        _pgOutput = Tool("Output", Ico.Terminal, "output", BuildOutput(), new Size(900, 250));
        _pgFind = Tool("Find Results", Ico.Search, "find", BuildFindResults(), new Size(900, 250));
        _pgTerminal = Tool("Developer PowerShell", Ico.Code, "terminal", BuildTerminal(), new Size(900, 250));

        _dm.AddAutoHiddenGroup("Control", DockingEdge.Left, new[] { _pgToolbox });
        _dm.AddDockspace("Control", DockingEdge.Bottom, new[] { _pgErrors, _pgOutput, _pgFind, _pgTerminal });
        _dm.AddDockspace("Control", DockingEdge.Right, new[] { _pgSolution, _pgGit }, new[] { new[] { _pgProps } });

        _ws.ActivePageChanged += (_, _) => OnActiveDocChanged();
        _dm.PageCloseRequest += OnPageCloseRequest;
    }

    KryptonPage Tool(string title, char glyph, string name, Control content, Size size)
    {
        var p = new KryptonPage { Text = title, TextTitle = title, TextDescription = title, UniqueName = name, Name = name };
        content.Dock = DockStyle.Fill; p.Controls.Add(content); p.Size = size;
        _pageGlyphs.Add((p, glyph));
        return p;
    }

    void ShowTool(KryptonPage p)
    {
        try { _dm.ShowPage(p.UniqueName); } catch { }
        // bring it to the front of its cell
        try
        {
            foreach (var ws in AllControls(_host).OfType<KryptonWorkspace>())
            {
                var cell = ws.CellForPage(p);
                if (cell != null) { cell.SelectedPage = p; break; }
            }
        }
        catch { }
    }

    static IEnumerable<Control> AllControls(Control c)
    {
        foreach (Control ch in c.Controls) { yield return ch; foreach (var d in AllControls(ch)) yield return d; }
    }

    void OnPageCloseRequest(object sender, CloseRequestEventArgs e)
    {
        if (e.UniqueName != null && _docs.TryGetValue(DocKey(e.UniqueName), out var d))
        {
            if (d.View.Editor.Modified)
            {
                var r = KryptonMessageBox.Show(this, $"Save changes to {Path.GetFileName(d.Path)}?", "Krypton Studio", KryptonMessageBoxButtons.YesNoCancel, KryptonMessageBoxIcon.Question);
                if (r == DialogResult.Cancel) { e.CloseRequest = DockingCloseRequest.None; return; }
                if (r == DialogResult.Yes && !SaveDoc(d)) { e.CloseRequest = DockingCloseRequest.None; return; }
            }
            _docs.Remove(d.Path);
            e.CloseRequest = DockingCloseRequest.RemovePageAndDispose;
            BeginInvoke(OnActiveDocChanged);
        }
        else e.CloseRequest = DockingCloseRequest.HidePage;
    }

    static string DocKey(string uniqueName) => uniqueName.StartsWith("doc:") ? uniqueName.Substring(4) : uniqueName;

    // ===================================================================
    //  Documents
    // ===================================================================
    Doc ActiveDoc
    {
        get
        {
            var p = _ws?.ActivePage;
            return p == null ? null : _docs.Values.FirstOrDefault(d => d.Page == p);
        }
    }
    CodeEditor Ed => ActiveDoc?.View.Editor;

    Doc OpenFile(string path, int line = 0, string select = null)
    {
        try
        {
            path = Path.GetFullPath(path);
            if (Directory.Exists(path)) return null;
            if (_docs.TryGetValue(path, out var existing))
            {
                Activate(existing.Page);
                if (line > 0) { existing.View.Editor.GoTo(line - 1); SelectInLine(existing.View.Editor, line - 1, select); }
                return existing;
            }
            if (!File.Exists(path)) { KryptonMessageBox.Show(this, "File not found:\n" + path, "Krypton Studio", KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Warning); return null; }
            var info = new FileInfo(path);
            if (info.Length > 40_000_000) { KryptonMessageBox.Show(this, "That file is too large for the editor.", "Krypton Studio", KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Warning); return null; }
            var bytes = File.ReadAllBytes(path);
            if (Array.IndexOf(bytes, (byte)0, 0, Math.Min(bytes.Length, 4096)) >= 0)
            { KryptonMessageBox.Show(this, "Binary files cannot be edited here.", "Krypton Studio", KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Information); return null; }
            bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            string text = new UTF8Encoding(false).GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));

            var view = new DocumentView(path, _projects.Count > 0 ? Path.GetFileNameWithoutExtension(_projects.FirstOrDefault(p => path.StartsWith(Path.GetDirectoryName(p) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) ?? _projects[0]) : "Miscellaneous Files");
            var ed = view.Editor;
            ed.Language = Languages.FromPath(path); ed.EditorFontSize = _cfg.FontSize; ed.ShowMinimap = _cfg.Minimap; ed.ShowLineNumbers = _cfg.LineNumbers;
            ed.LoadText(text); ed.Theme = EditorThemes.From(_t); view.Find.RefreshIcons(_t);

            var page = new KryptonPage { Text = Path.GetFileName(path), TextTitle = Path.GetFileName(path), TextDescription = path, UniqueName = "doc:" + path, Name = "doc:" + path };
            view.Dock = DockStyle.Fill; page.Controls.Add(view);
            page.ImageSmall = DocIcon(path);
            var d = new Doc { Page = page, View = view, Path = path, Bom = bom };
            _docs[path] = d;

            ed.Changed += (_, _) => { page.Text = Path.GetFileName(path) + (ed.Modified ? " \u25CF" : ""); UpdateToolbarState(); };
            ed.CaretMoved += (_, _) => { if (ActiveDoc == d) UpdateStatus(); };
            ed.BreakpointsChanged += (_, _) => Say($"Breakpoint {(ed.Breakpoints.Count > 0 ? "set" : "cleared")}");

            _dm.AddToWorkspace("Workspace", new[] { page });
            ThemeApplier.Apply(view, _t);
            Activate(page);
            ApplyDiagnostics(d);
            if (line > 0) { ed.GoTo(line - 1); SelectInLine(ed, line - 1, select); }
            ed.Focus();
            return d;
        }
        catch (Exception ex)
        {
            KryptonMessageBox.Show(this, ex.Message, "Krypton Studio", KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Error);
            return null;
        }
    }

    static void SelectInLine(CodeEditor ed, int line, string term)
    {
        if (string.IsNullOrEmpty(term) || line >= ed.LineCount) return;
        int i = ed.GetLine(line).IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (i >= 0) ed.Select(new Pos(line, i), new Pos(line, i + term.Length));
    }

    void Activate(KryptonPage page)
    {
        try { var cell = _ws.CellForPage(page); if (cell != null) { cell.SelectedPage = page; _ws.ActiveCell = cell; } } catch { }
        BeginInvoke(OnActiveDocChanged);
    }

    Bitmap DocIcon(string path)
    {
        using var il = Ico.FileImages(_t.TextMuted);
        string k = Ico.KeyFor(path);
        return new Bitmap(il.Images[k]);
    }

    bool SaveDoc(Doc d, bool saveAs = false)
    {
        try
        {
            string path = d.Path;
            if (saveAs)
            {
                using var dlg = new SaveFileDialog { FileName = Path.GetFileName(path), InitialDirectory = Path.GetDirectoryName(path) };
                if (dlg.ShowDialog(this) != DialogResult.OK) return false;
                path = dlg.FileName;
            }
            var enc = new UTF8Encoding(d.Bom);
            File.WriteAllText(path, d.View.Editor.GetText(), enc);
            if (!string.Equals(path, d.Path, StringComparison.OrdinalIgnoreCase)) { OpenFile(path); return true; }
            d.View.Editor.MarkSaved();
            Say("Saved " + Path.GetFileName(path));
            RefreshGit();
            return true;
        }
        catch (Exception ex)
        {
            KryptonMessageBox.Show(this, ex.Message, "Save failed", KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Error);
            return false;
        }
    }

    void SaveAll() { foreach (var d in _docs.Values.ToList()) if (d.View.Editor.Modified) SaveDoc(d); }

    void OnActiveDocChanged()
    {
        var d = ActiveDoc;
        Text = (d != null ? Path.GetFileName(d.Path) + " - " : "") + (_slnPath != null ? Path.GetFileNameWithoutExtension(_slnPath) + " - " : "") + "Krypton Studio";
        if (d != null) { var cs = d.View.Editor; SelectInTree(d.Path); }
        UpdateStatus(); UpdateToolbarState();
    }

    void UpdateStatus()
    {
        var ed = Ed;
        if (ed == null) { _status.Set(_sLn, ""); _status.Set(_sCol, ""); _status.Set(_sCh, ""); _status.Set(_sEol, ""); _status.Set(_sSpc, ""); _status.Set(_sEnc, ""); return; }
        _status.Set(_sLn, $"Ln: {ed.Caret.Line + 1}");
        _status.Set(_sCol, $"Col: {ed.Caret.Column + 1}");
        _status.Set(_sCh, ed.HasSelection ? $"Sel: {ed.SelectedText.Length}" : $"Ch: {ed.Caret.Column + 1}");
        _status.Set(_sSpc, "SPC");
        _status.Set(_sEol, ed.EolName);
        _status.Set(_sEnc, "UTF-8");
    }

    void Say(string text) => _status?.Set(_sReady, text);

    // ===================================================================
    //  Theme
    // ===================================================================
    void ApplyTheme(Tokens t)
    {
        _t = t; _cfg.Theme = t.Name;
        var old = _palette;
        _palette = ModernTheme.Build(t);
        _manager.GlobalPaletteMode = PaletteMode.Custom;
        _manager.GlobalCustomPalette = _palette;
        old?.Dispose();
        BackColor = t.Canvas;
        Icon = Ico.AppIcon(t.Accent);

        // images tinted for this theme
        foreach (var (item, glyph) in _glyphItems) { var old2 = item.Image; item.Image = Ico.Glyph(glyph, t.Text); }
        foreach (var (page, glyph) in _pageGlyphs) page.ImageSmall = Ico.Glyph(glyph, t.TextMuted);
        _tree.ImageList = Ico.FileImages(t.TextMuted);
        _toolboxTree.ImageList = Ico.FileImages(t.TextMuted);
        foreach (var l in new[] { _errList, _gitList, _gitLog, _findList }) { l.SmallImageList.Images.Clear(); foreach (string k in _tree.ImageList.Images.Keys) l.SmallImageList.Images.Add(k, new Bitmap(_tree.ImageList.Images[k])); }
        foreach (var d in _docs.Values) { d.Page.ImageSmall = DocIcon(d.Path); d.View.Editor.Theme = EditorThemes.From(t); d.View.Find.RefreshIcons(t); d.View.Nav.Invalidate(); }

        // status bar: accent, orange while something runs
        RecolorStatus();

        ThemeApplier.Apply(this, t);
        foreach (var d in _docs.Values) ThemeApplier.Apply(d.View, t);
        foreach (var p in new[] { _pgSolution, _pgProps, _pgErrors, _pgOutput, _pgToolbox, _pgGit, _pgFind, _pgTerminal, _pgStart })
            if (p != null) ThemeApplier.Apply(p, t);
        UpdateThemeChecks();
        _startPage?.ApplyTheme(t);
        _outputBox.BackColor = t.Surface; _termBox.BackColor = t.Surface;
        _menu.BackColor = t.Surface; _tool.BackColor = t.Surface;
        Invalidate(true);
    }

    void RecolorStatus()
    {
        if (_status == null) return;
        var running = _runProc != null && !_runProc.HasExited;
        _status.Back = _building ? Color.FromArgb(0x7A, 0x4F, 0xD0) : running ? Color.FromArgb(0xCA, 0x51, 0x21) : _t.Accent;
        _status.Fore = (_building || running) ? Color.White : _t.OnAccent;
        _status.Invalidate();
    }

    // ===================================================================
    //  Solution
    // ===================================================================
    void LoadSolution(string path)
    {
        try
        {
            _projects.Clear(); _startup = null;
            if (Directory.Exists(path)) { _rootDir = path; _slnPath = path; foreach (var p in SafeEnumerate(path, "*.csproj", 3)) _projects.Add(p); }
            else
            {
                _slnPath = path; _rootDir = Path.GetDirectoryName(path);
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".csproj" || ext == ".vbproj" || ext == ".fsproj") _projects.Add(path);
                else if (ext == ".slnx")
                    foreach (var pe in XDocument.Load(path).Descendants("Project")) { var rel = (string)pe.Attribute("Path"); if (rel != null) _projects.Add(Path.GetFullPath(Path.Combine(_rootDir, rel))); }
                else if (ext == ".sln")
                    foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(path), "Project\\(\"[^\"]+\"\\)\\s*=\\s*\"[^\"]+\",\\s*\"([^\"]+proj)\""))
                        _projects.Add(Path.GetFullPath(Path.Combine(_rootDir, m.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar))));
            }
            _projects.RemoveAll(p => !File.Exists(p));
            _startup = _projects.FirstOrDefault(p => { try { return File.ReadAllText(p).Contains("Exe</OutputType>"); } catch { return false; } }) ?? _projects.FirstOrDefault();
            _cfg.AddRecent(path); _cfg.Save();
            BuildTree(null);
            OnActiveDocChanged();
            _status.Set(_sProj, _startup != null ? Path.GetFileNameWithoutExtension(_startup) : Path.GetFileName(_rootDir));
            UpdateStartButton();
            RefreshGit();
            Say("Opened " + Path.GetFileName(path));
        }
        catch (Exception ex) { KryptonMessageBox.Show(this, ex.Message, "Open failed", KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Error); }
    }

    static IEnumerable<string> SafeEnumerate(string dir, string pattern, int depth)
    {
        var res = new List<string>();
        void Rec(string d, int lvl)
        {
            try
            {
                res.AddRange(Directory.GetFiles(d, pattern));
                if (lvl <= 0) return;
                foreach (var s in Directory.GetDirectories(d)) { var n = Path.GetFileName(s); if (!IsExcluded(n)) Rec(s, lvl - 1); }
            }
            catch { }
        }
        Rec(dir, depth); return res.Take(30);
    }

    static bool IsExcluded(string name) => name is "bin" or "obj" or ".git" or ".vs" or "node_modules" or "packages" or ".idea";

    void OpenSolutionDialog()
    {
        using var dlg = new OpenFileDialog { Title = "Open Project or Solution", Filter = "Solutions and projects|*.sln;*.slnx;*.csproj|All files|*.*" };
        if (dlg.ShowDialog(this) == DialogResult.OK) LoadSolution(dlg.FileName);
    }
    void OpenFolderDialog()
    {
        using var dlg = new FolderBrowserDialog { Description = "Open Folder" };
        if (dlg.ShowDialog(this) == DialogResult.OK) LoadSolution(dlg.SelectedPath);
    }
    void OpenFileDialogCmd()
    {
        using var dlg = new OpenFileDialog { Title = "Open File", InitialDirectory = _rootDir };
        if (dlg.ShowDialog(this) == DialogResult.OK) OpenFile(dlg.FileName);
    }

    void NewFile()
    {
        string dir = SelectedDir() ?? _rootDir;
        if (dir == null) return;
        var name = PromptDialog.Ask(this, "Add New File", "File name:", "NewClass.cs");
        if (string.IsNullOrWhiteSpace(name)) return;
        string p = Path.Combine(dir, name);
        if (!File.Exists(p))
        {
            string body = Path.GetExtension(p).Equals(".cs", StringComparison.OrdinalIgnoreCase)
                ? $"namespace {(_startup != null ? Path.GetFileNameWithoutExtension(_startup) : "App")};\r\n\r\npublic class {Path.GetFileNameWithoutExtension(p)}\r\n{{\r\n    \r\n}}\r\n" : "";
            File.WriteAllText(p, body, new UTF8Encoding(false));
        }
        BuildTree(_treeFilter.Text.Trim().Length == 0 ? null : _treeFilter.Text.Trim());
        OpenFile(p); RefreshGit();
    }

    string FindDefaultSolutionPublic() => FindDefaultSolution();

    // ===================================================================
    //  Start page
    // ===================================================================
    StartPage _startPage;
    void OpenStartPage()
    {
        if (_pgStart != null && !_pgStart.IsDisposed) { Activate(_pgStart); return; }
        _startPage = new StartPage(this, _cfg) { Dock = DockStyle.Fill };
        _pgStart = new KryptonPage { Text = "Start Page", TextTitle = "Start Page", TextDescription = "Start Page", UniqueName = "start", Name = "start" };
        _pgStart.Controls.Add(_startPage);
        _pgStart.ImageSmall = Ico.Glyph(Ico.Home, _t.TextMuted);
        _dm.AddToWorkspace("Workspace", new[] { _pgStart });
        _startPage.ApplyTheme(_t);
        ThemeApplier.Apply(_pgStart, _t);
        Activate(_pgStart);
    }

    internal void StartAction(string what)
    {
        switch (what)
        {
            case "solution": OpenSolutionDialog(); break;
            case "folder": OpenFolderDialog(); break;
            case "file": NewFile(); break;
            case "theme": CycleTheme(); break;
            case "options": ShowOptions(); break;
            default:
                if (what.StartsWith("theme:")) { var th = ModernTheme.All.FirstOrDefault(x => x.Name == what.Substring(6)); if (th != null) BeginInvoke(() => ApplyTheme(th)); break; }
                if (File.Exists(what) || Directory.Exists(what)) LoadSolution(what); break;
        }
    }

    void CycleTheme()
    {
        int i = Array.IndexOf(ModernTheme.All, _t);
        ApplyTheme(ModernTheme.All[(i + 1) % ModernTheme.All.Length]);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        var dirty = _docs.Values.Where(d => d.View.Editor.Modified).ToList();
        if (dirty.Count > 0)
        {
            var r = KryptonMessageBox.Show(this, $"Save changes to {dirty.Count} modified file{(dirty.Count == 1 ? "" : "s")}?", "Krypton Studio", KryptonMessageBoxButtons.YesNoCancel, KryptonMessageBoxIcon.Question);
            if (r == DialogResult.Cancel) { e.Cancel = true; return; }
            if (r == DialogResult.Yes) SaveAll();
        }
        StopRun(); _cfg.Save();
        try { var d = Path.Combine(Path.GetTempPath(), "KryptonStudio"); if (Directory.Exists(d)) Directory.Delete(d, true); } catch { }
        base.OnFormClosing(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Q)) { _quick.Focus(); _quick.SelectAll(); return true; }
        if (keyData == (Keys.Control | Keys.F)) { ActiveDoc?.View.Find.Open(false); return true; }
        if (keyData == (Keys.Control | Keys.H)) { ActiveDoc?.View.Find.Open(true); return true; }
        if (keyData == (Keys.Control | Keys.G)) { GoToLine(); return true; }
        if (keyData == (Keys.Control | Keys.Shift | Keys.F)) { ShowTool(_pgFind); _findBox.Focus(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    void GoToLine()
    {
        var ed = Ed; if (ed == null) return;
        var s = PromptDialog.Ask(this, "Go To Line", $"Line number (1 - {ed.LineCount}):", (ed.Caret.Line + 1).ToString());
        if (int.TryParse(s, out var n)) { ed.GoTo(n - 1); ed.Focus(); }
    }
}
