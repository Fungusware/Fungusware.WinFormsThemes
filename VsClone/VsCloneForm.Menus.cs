// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using System.Diagnostics;
using Krypton.Toolkit;
using Fungusware.WinFormsThemes.Editor;

namespace Fungusware.WinFormsThemes.Demo.VsClone;

public partial class VsCloneForm
{
    readonly List<(ToolStripMenuItem item, Tokens theme)> _themeItems = new();
    ToolStripMenuItem _miMinimap, _miLineNumbers;

    ToolStripMenuItem Mi(string text, Action a, Keys keys = Keys.None, char glyph = '\0', string hint = null)
    {
        var mi = new ToolStripMenuItem(text);
        if (a != null) mi.Click += (_, _) => a();
        if (keys != Keys.None) mi.ShortcutKeys = keys; else if (hint != null) mi.ShortcutKeyDisplayString = hint;
        if (glyph != '\0') _glyphItems.Add((mi, glyph));
        return mi;
    }

    static ToolStripMenuItem Menu(string text, params ToolStripItem[] items)
    {
        var m = new ToolStripMenuItem(text); m.DropDownItems.AddRange(items); return m;
    }

    static ToolStripSeparator Sep() => new();

    const Keys C = Keys.Control, S = Keys.Shift, A = Keys.Alt;

    void BuildMenu()
    {
        _menu = new MenuStrip { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f), GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(4, 3, 4, 1) };

        var file = Menu("&File",
            Menu("&New", Mi("File...", NewFile, C | S | Keys.N, Ico.NewFile), Mi("Project...", () => Info("Project templates are not part of this demo."))),
            Menu("&Open", Mi("Project/Solution...", OpenSolutionDialog, C | S | Keys.O, Ico.Folder), Mi("Folder...", OpenFolderDialog), Mi("File...", OpenFileDialogCmd, C | Keys.O, Ico.OpenFile)),
            Mi("Start Window", OpenStartPage),
            Sep(),
            Mi("&Close", () => CloseActive(), C | Keys.F4),
            Mi("Close Solution", () => { CloseAllDocs(); _projects.Clear(); _rootDir = null; _slnPath = null; _startup = null; BuildTree(null); OnActiveDocChanged(); }),
            Sep(),
            Mi("&Save", () => { if (ActiveDoc != null) SaveDoc(ActiveDoc); }, C | Keys.S, Ico.Save),
            Mi("Save &As...", () => { if (ActiveDoc != null) SaveDoc(ActiveDoc, true); }),
            Mi("Save A&ll", SaveAll, C | S | Keys.S, Ico.Save),
            Sep(),
            Mi("E&xit", Close, A | Keys.F4));

        var edit = Menu("&Edit",
            Mi("&Undo", () => Ed?.Undo(), Keys.None, Ico.Undo, "Ctrl+Z"),
            Mi("&Redo", () => Ed?.Redo(), Keys.None, Ico.Redo, "Ctrl+Y"),
            Sep(),
            Mi("Cu&t", () => Ed?.Cut(), Keys.None, Ico.Cut, "Ctrl+X"),
            Mi("&Copy", () => Ed?.Copy(), Keys.None, Ico.Copy, "Ctrl+C"),
            Mi("&Paste", () => Ed?.Paste(), Keys.None, Ico.Paste, "Ctrl+V"),
            Mi("&Delete", () => Ed?.DeleteLine(), Keys.None, Ico.Delete, "Ctrl+Shift+L"),
            Sep(),
            Mi("Select &All", () => Ed?.SelectAll(), Keys.None, '\0', "Ctrl+A"),
            Sep(),
            Menu("&Find and Replace",
                Mi("Quick &Find", () => ActiveDoc?.View.Find.Open(false), Keys.None, Ico.Search, "Ctrl+F"),
                Mi("Quick &Replace", () => ActiveDoc?.View.Find.Open(true), Keys.None, Ico.Replace, "Ctrl+H"),
                Mi("Find in &Files", () => { ShowTool(_pgFind); _findBox.Focus(); }, Keys.None, '\0', "Ctrl+Shift+F")),
            Menu("&Go To", Mi("Go To &Line...", GoToLine, Keys.None, '\0', "Ctrl+G")),
            Menu("A&dvanced",
                Mi("Comment/Uncomment Selection", () => Ed?.ToggleComment(), Keys.None, '\0', "Ctrl+/"),
                Mi("Duplicate Line", () => Ed?.DuplicateLine(), Keys.None, '\0', "Ctrl+D"),
                Mi("Move Line Up", () => Ed?.MoveLines(-1), Keys.None, '\0', "Alt+Up"),
                Mi("Move Line Down", () => Ed?.MoveLines(1), Keys.None, '\0', "Alt+Down"),
                Mi("Trim Trailing Whitespace", () => Ed?.TrimTrailingWhitespace()),
                Mi("List Members", () => Ed?.Focus(), Keys.None, '\0', "Ctrl+Space")));

        _miMinimap = Mi("Minimap", () => { _cfg.Minimap = !_cfg.Minimap; foreach (var d in _docs.Values) d.View.Editor.ShowMinimap = _cfg.Minimap; _miMinimap.Checked = _cfg.Minimap; });
        _miMinimap.Checked = _cfg.Minimap;
        _miLineNumbers = Mi("Line Numbers", () => { _cfg.LineNumbers = !_cfg.LineNumbers; foreach (var d in _docs.Values) d.View.Editor.ShowLineNumbers = _cfg.LineNumbers; _miLineNumbers.Checked = _cfg.LineNumbers; });
        _miLineNumbers.Checked = _cfg.LineNumbers;
        var themeMenu = Menu("&Theme");
        foreach (var th in ModernTheme.All)
        {
            var mi = new ToolStripMenuItem(th.Name);
            var theme = th; mi.Click += (_, _) => BeginInvoke(() => ApplyTheme(theme));
            themeMenu.DropDownItems.Add(mi); _themeItems.Add((mi, th));
        }

        var view = Menu("&View",
            Mi("Solution &Explorer", () => ShowTool(_pgSolution), C | A | Keys.L, Ico.List),
            Mi("&Git Changes", () => ShowTool(_pgGit), C | Keys.D0, Ico.Git),
            Mi("&Properties Window", () => ShowTool(_pgProps), Keys.F4, Ico.Properties),
            Mi("&Toolbox", () => ShowTool(_pgToolbox), C | A | Keys.X, Ico.Toolbox),
            Sep(),
            Mi("Error Lis&t", () => ShowTool(_pgErrors), Keys.None, Ico.Error, "Ctrl+\\, E"),
            Mi("&Output", () => ShowTool(_pgOutput), C | A | Keys.O, Ico.Terminal),
            Mi("Find &Results", () => ShowTool(_pgFind), Keys.None, Ico.Search),
            Mi("Developer Power&Shell", () => { ShowTool(_pgTerminal); _termInput.Focus(); }, C | Keys.Oemtilde, Ico.Code),
            Sep(),
            Mi("&Start Page", OpenStartPage, Keys.None, Ico.Home),
            Sep(),
            themeMenu,
            _miMinimap, _miLineNumbers,
            Sep(),
            Mi("Zoom In", () => { if (Ed != null) Ed.EditorFontSize += 1; }, Keys.None, '\0', "Ctrl+Wheel"),
            Mi("Zoom Out", () => { if (Ed != null) Ed.EditorFontSize -= 1; }),
            Mi("Reset Zoom", () => { if (Ed != null) Ed.EditorFontSize = 10.5f; }),
            Sep(),
            Mi("Full Screen", () => { if (FormBorderStyle == FormBorderStyle.None) { FormBorderStyle = FormBorderStyle.Sizable; WindowState = FormWindowState.Normal; } else { WindowState = FormWindowState.Normal; FormBorderStyle = FormBorderStyle.None; WindowState = FormWindowState.Maximized; } }, S | A | Keys.Enter));

        var git = Menu("&Git",
            Mi("&Refresh Changes", RefreshGit, Keys.None, Ico.Sync),
            Mi("Git Changes", () => ShowTool(_pgGit), Keys.None, Ico.Git),
            Sep(),
            Mi("Open Repository in File Explorer", () => { if (_rootDir != null) try { Process.Start("explorer.exe", $"\"{(Directory.Exists(_rootDir) ? _rootDir : Path.GetDirectoryName(_rootDir))}\""); } catch { } }));

        var project = Menu("&Project",
            Mi("Add New &File...", NewFile, C | S | Keys.A, Ico.NewFile),
            Sep(),
            Mi("Set as Startup Project", () => { if (_tree.SelectedNode?.Tag is string p && p.EndsWith("proj", StringComparison.OrdinalIgnoreCase)) { _startup = p; UpdateStartButton(); BuildTree(CurrentFilter()); } }),
            Mi("Open Project Folder", () => { if (_startup != null) try { Process.Start("explorer.exe", $"\"{Path.GetDirectoryName(_startup)}\""); } catch { } }));

        var build = Menu("&Build",
            Mi("&Build Solution", async () => await BuildAsync(false, false), C | S | Keys.B, Ico.Build),
            Mi("&Rebuild Solution", async () => await BuildAsync(true, false)),
            Mi("&Clean Solution", async () => await BuildAsync(false, true)),
            Sep(),
            Mi("Cancel", StopRun, C | Keys.Pause));

        var debug = Menu("&Debug",
            Mi("Start (dotnet run)", async () => await StartAsync(true), Keys.F5, Ico.Play),
            Mi("Start Without Debugging", async () => await StartAsync(false), C | Keys.F5, Ico.Play),
            Mi("Stop", StopRun, S | Keys.F5, Ico.Stop),
            Sep(),
            Mi("Toggle Breakpoint", () => { var ed = Ed; if (ed != null && !ed.Breakpoints.Remove(ed.Caret.Line)) ed?.Breakpoints.Add(ed.Caret.Line); Ed?.Invalidate(); }, Keys.F9));

        var test = Menu("T&est", Mi("Run All Tests", async () => { if (_startup == null) return; ShowTool(_pgOutput); Out("dotnet test ...", _t.TextMuted); await RunDotnet(new[] { "test", _startup, "-nologo" }, Path.GetDirectoryName(_startup), l => Out(l)); }, C | Keys.R));
        var analyze = Menu("&Analyze", Mi("Format Document (trim whitespace)", () => Ed?.TrimTrailingWhitespace()), Mi("Run Code Analysis (dotnet build)", async () => await BuildAsync(false, false)));

        var tools = Menu("&Tools",
            Mi("Developer PowerShell", () => { ShowTool(_pgTerminal); _termInput.Focus(); }, Keys.None, Ico.Code),
            Mi("Command Prompt Here", () => { if (_rootDir != null) try { Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = Directory.Exists(_rootDir) ? _rootDir : Path.GetDirectoryName(_rootDir), UseShellExecute = true }); } catch { } }),
            Sep(),
            Mi("&Options...", ShowOptions, Keys.None, Ico.Settings));

        var ext = Menu("E&xtensions", Mi("Manage Extensions", () => Info("All of this window is Krypton Toolkit: docking, workspace tabs, ribbon-class buttons, text boxes, dialogs.\nThere is nothing to install.")));

        var window = Menu("&Window",
            Mi("Close All Documents", CloseAllDocs),
            Mi("Reset Window Layout", ResetLayout),
            Sep(),
            Mi("Auto Hide Toolbox", () => ShowTool(_pgToolbox)));

        var help = Menu("&Help",
            Mi("View Krypton Toolkit on GitHub", () => OpenUrl("https://github.com/Krypton-Suite/Standard-Toolkit")),
            Mi("Fungusware.WinFormsThemes on GitHub", () => OpenUrl("https://github.com/Fungusware/Fungusware.WinFormsThemes")),
            Sep(),
            Mi("&About Krypton Studio", () => new AboutDialog(_t).ShowDialog(this), Keys.None, Ico.Info));

        _menu.Items.AddRange(new ToolStripItem[] { file, edit, view, git, project, build, debug, test, analyze, tools, ext, window, help });
        UpdateThemeChecks();

        _top = new KryptonPanel { Dock = DockStyle.Top, Height = 32 };
        _top.Controls.Add(_menu);
    }

    static void OpenUrl(string u) { try { Process.Start(new ProcessStartInfo(u) { UseShellExecute = true }); } catch { } }
    void Info(string m) => KMessage.Show(this, m, "Krypton Studio", KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Information);

    void UpdateThemeChecks() { foreach (var (mi, th) in _themeItems) mi.Checked = th == _t || th.Name == _t.Name; }

    void CloseActive() { var d = ActiveDoc; if (d != null) _dm.CloseRequest(new[] { d.Page.UniqueName }); }
    void CloseAllDocs() { foreach (var d in _docs.Values.ToList()) _dm.CloseRequest(new[] { d.Page.UniqueName }); }

    void ResetLayout()
    {
        if (_defaultLayout == null) return;
        try { _dm.LoadConfigFromArray(_defaultLayout); } catch (Exception ex) { Info("Could not reset layout: " + ex.Message); }
    }

    // ===================================================================
    //  Toolbar
    // ===================================================================
    void BuildToolbar()
    {
        _tool = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden, ImageScalingSize = new Size(18, 18), AutoSize = false, Height = 38, Padding = new Padding(6, 3, 6, 3), Font = new Font("Segoe UI", 9.5f) };
        ToolStripButton B(char glyph, string tip, Action a)
        {
            var b = new ToolStripButton { DisplayStyle = ToolStripItemDisplayStyle.Image, ToolTipText = tip, Margin = new Padding(1, 0, 1, 0), Padding = new Padding(4) };
            b.Click += (_, _) => a(); _glyphItems.Add((b, glyph)); _tool.Items.Add(b); return b;
        }
        B(Ico.Back, "Navigate Backward", () => Info("Navigation history is not implemented in this demo."));
        B(Ico.Forward, "Navigate Forward", () => Info("Navigation history is not implemented in this demo."));
        _tool.Items.Add(new ToolStripSeparator());
        B(Ico.OpenFile, "Open File (Ctrl+O)", OpenFileDialogCmd);
        _tbSave = B(Ico.Save, "Save (Ctrl+S)", () => { if (ActiveDoc != null) SaveDoc(ActiveDoc); });
        _tbSaveAll = B(Ico.Save, "Save All (Ctrl+Shift+S)", SaveAll);
        _tool.Items.Add(new ToolStripSeparator());
        _tbUndo = B(Ico.Undo, "Undo (Ctrl+Z)", () => Ed?.Undo());
        _tbRedo = B(Ico.Redo, "Redo (Ctrl+Y)", () => Ed?.Redo());
        _tool.Items.Add(new ToolStripSeparator());

        _cfgCombo = new KryptonComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 92 };
        _cfgCombo.Items.AddRange(new object[] { "Debug", "Release" }); _cfgCombo.SelectedIndex = 0;
        _tool.Items.Add(new ToolStripControlHost(_cfgCombo) { AutoSize = false, Size = new Size(96, 28), Margin = new Padding(4, 0, 2, 0) });
        var plat = new KryptonComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 96 };
        plat.Items.AddRange(new object[] { "Any CPU", "x64", "x86" }); plat.SelectedIndex = 0;
        _tool.Items.Add(new ToolStripControlHost(plat) { AutoSize = false, Size = new Size(100, 28), Margin = new Padding(2, 0, 8, 0) });

        _startBtn = new KryptonButton { ButtonStyle = ButtonStyle.Custom1, Size = new Size(170, 28), Text = "Start" };
        _startBtn.Values.Text = "  Start";
        _startBtn.Click += async (_, _) => await StartAsync(false);
        _tool.Items.Add(new ToolStripControlHost(_startBtn) { AutoSize = false, Size = new Size(174, 28), Margin = new Padding(0, 0, 4, 0) });
        _tbStop = B(Ico.Stop, "Stop (Shift+F5)", StopRun);
        _tbBuild = B(Ico.Build, "Build (Ctrl+Shift+B)", async () => await BuildAsync(false, false));
        _tool.Items.Add(new ToolStripSeparator());
        B(Ico.Search, "Find in Files (Ctrl+Shift+F)", () => { ShowTool(_pgFind); _findBox.Focus(); });
        B(Ico.Terminal, "Developer PowerShell", () => { ShowTool(_pgTerminal); _termInput.Focus(); });
        B(Ico.Settings, "Options", ShowOptions);
        UpdateToolbarState();
    }

    // ===================================================================
    //  Status bar
    // ===================================================================
    void BuildStatus()
    {
        _status = new StatusBarEx();
        _sReady = _status.Add("Ready", false);
        _sProj = _status.Add("", true, () => ShowTool(_pgSolution));
        _sBranch = _status.Add("⎇ -", true, () => ShowTool(_pgGit));
        _sEol = _status.Add("", true);
        _sEnc = _status.Add("", true);
        _sSpc = _status.Add("", true);
        _sCh = _status.Add("", true);
        _sCol = _status.Add("", true);
        _sLn = _status.Add("", true, GoToLine);
        _status.Add("✓ Krypton Toolkit", false);
    }

    // ===================================================================
    //  Quick launch (Ctrl+Q)
    // ===================================================================
    void BuildQuickLaunch()
    {
        var host = new KryptonPanel { Dock = DockStyle.Right, Width = 290, Padding = new Padding(4, 3, 10, 3) };
        _quick = new KryptonTextBox { Dock = DockStyle.Fill };
        _quick.CueHint.CueHintText = "Search (Ctrl+Q)";
        host.Controls.Add(_quick);
        _top.Controls.Add(host);
        _top.Controls.SetChildIndex(_menu, 0); _top.Controls.SetChildIndex(host, 1);

        _quickList = new ListBox { Visible = false, Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Height = 260, Width = 460 };
        Controls.Add(_quickList);
        _quick.TextChanged += (_, _) => RefreshQuick();
        _quick.LostFocus += (_, _) => BeginInvoke(() => { if (!_quickList.Focused) _quickList.Visible = false; });
        _quick.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Down && _quickList.Visible) { _quickList.SelectedIndex = Math.Min(_quickList.Items.Count - 1, _quickList.SelectedIndex + 1); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Up && _quickList.Visible) { _quickList.SelectedIndex = Math.Max(0, _quickList.SelectedIndex - 1); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Enter) { RunQuick(); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Escape) { _quickList.Visible = false; Ed?.Focus(); e.SuppressKeyPress = true; }
        };
        _quickList.MouseClick += (_, _) => RunQuick();
    }

    sealed record Cmd(string Title, ToolStripMenuItem Item) { public override string ToString() => Title; }

    IEnumerable<Cmd> AllCommands(ToolStripItemCollection items, string path)
    {
        foreach (ToolStripItem it in items)
        {
            if (it is not ToolStripMenuItem mi) continue;
            string name = mi.Text.Replace("&", "").Replace("...", "");
            if (mi.HasDropDownItems) { foreach (var c in AllCommands(mi.DropDownItems, path.Length == 0 ? name : path + " > " + name)) yield return c; }
            else yield return new Cmd((path.Length > 0 ? path + "  ›  " : "") + name, mi);
        }
    }

    void RefreshQuick()
    {
        string q = _quick.Text.Trim();
        if (q.Length == 0) { _quickList.Visible = false; return; }
        var hits = AllCommands(_menu.Items, "").Where(c => c.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).Take(14).ToList();
        _quickList.BeginUpdate(); _quickList.Items.Clear(); foreach (var h in hits) _quickList.Items.Add(h); _quickList.EndUpdate();
        if (hits.Count == 0) { _quickList.Visible = false; return; }
        var pt = PointToClient(_quick.PointToScreen(new Point(_quick.Width - _quickList.Width, _quick.Height + 2)));
        _quickList.Location = pt; _quickList.Height = Math.Min(260, hits.Count * 20 + 6);
        _quickList.BackColor = _t.Surface; _quickList.ForeColor = _t.Text;
        _quickList.SelectedIndex = 0; _quickList.Visible = true; _quickList.BringToFront();
    }

    void RunQuick()
    {
        var cmd = _quickList.SelectedItem as Cmd ?? (_quickList.Items.Count > 0 ? _quickList.Items[0] as Cmd : null);
        _quickList.Visible = false; _quick.Clear();
        cmd?.Item.PerformClick();
    }

    // ===================================================================
    //  Dialogs
    // ===================================================================
    void ShowOptions()
    {
        using var dlg = new OptionsDialog(_cfg, _t);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var th = ModernTheme.All.FirstOrDefault(x => x.Name == _cfg.Theme) ?? _t;
        foreach (var d in _docs.Values) { d.View.Editor.EditorFontSize = _cfg.FontSize; d.View.Editor.ShowMinimap = _cfg.Minimap; d.View.Editor.ShowLineNumbers = _cfg.LineNumbers; }
        _miMinimap.Checked = _cfg.Minimap; _miLineNumbers.Checked = _cfg.LineNumbers;
        _cfg.Save();
        BeginInvoke(() => ApplyTheme(th));
    }
}
