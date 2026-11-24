// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Krypton.Toolkit;
using Fungusware.WinFormsThemes.Editor;

namespace Fungusware.WinFormsThemes.Demo.VsClone;

public partial class VsCloneForm
{
    // tool-window controls
    ThemedTree _tree, _toolboxTree;
    KryptonTextBox _treeFilter, _findBox, _termInput;
    PropertyGrid _grid;
    ThemedList _errList, _gitList, _gitLog, _findList;
    RichTextBox _outputBox, _debugBox, _termBox;
    KryptonLabel _gitHeader, _findStatus;
    KryptonCheckBox _findCase;
    ToolStripButton _tbErrors, _tbWarnings, _tbInfo;
    string _termDir;
    readonly List<string> _termHistory = new(); int _termHistoryPos;

    record Diag(bool IsError, string Code, string Message, string Project, string File, int Line, int Col);
    readonly List<Diag> _diags = new();

    static void Stack(Control parent, Control fill, params Control[] topsOuterFirst)
    {
        parent.Controls.Add(fill);
        foreach (var c in topsOuterFirst) parent.Controls.Add(c);
        parent.Controls.SetChildIndex(fill, 0);
        for (int i = 0; i < topsOuterFirst.Length; i++) parent.Controls.SetChildIndex(topsOuterFirst[i], topsOuterFirst.Length - i);
    }

    ToolStrip MiniStrip()
    {
        var ts = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Top, ImageScalingSize = new Size(16, 16), AutoSize = false, Height = 30, Padding = new Padding(4, 2, 4, 0), Font = new Font("Segoe UI", 9.5f) };
        return ts;
    }

    ToolStripButton MiniButton(ToolStrip ts, char glyph, string tip, Action a)
    {
        var b = new ToolStripButton { DisplayStyle = ToolStripItemDisplayStyle.Image, ToolTipText = tip, Margin = new Padding(1, 1, 1, 1) };
        b.Click += (_, _) => a(); _glyphItems.Add((b, glyph)); ts.Items.Add(b); return b;
    }

    // ===================================================================
    //  Solution Explorer
    // ===================================================================
    Control BuildSolutionExplorer()
    {
        var root = new KryptonPanel();
        var ts = MiniStrip();
        MiniButton(ts, Ico.Home, "Home", () => { if (_tree.Nodes.Count > 0) { _tree.CollapseAll(); _tree.Nodes[0].Expand(); foreach (TreeNode n in _tree.Nodes[0].Nodes) n.Expand(); } });
        MiniButton(ts, Ico.Sync, "Refresh", () => { BuildTree(CurrentFilter()); RefreshGit(); });
        MiniButton(ts, Ico.Collapse, "Collapse All", () => { _tree.CollapseAll(); if (_tree.Nodes.Count > 0) _tree.Nodes[0].Expand(); });
        MiniButton(ts, Ico.NewFile, "New File", NewFile);

        var filterHost = new KryptonPanel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(6, 4, 6, 4) };
        _treeFilter = new KryptonTextBox { Dock = DockStyle.Fill };
        _treeFilter.CueHint.CueHintText = "Search Solution Explorer";
        _treeFilter.TextChanged += (_, _) => { _filterTimer.Stop(); _filterTimer.Start(); };
        filterHost.Controls.Add(_treeFilter);
        _filterTimer.Tick += (_, _) => { _filterTimer.Stop(); BuildTree(CurrentFilter()); };

        _tree = new ThemedTree { Dock = DockStyle.Fill, ImageList = Ico.FileImages(_t.TextMuted) };
        _tree.BeforeExpand += (_, e) => Populate(e.Node);
        _tree.NodeMouseDoubleClick += (_, e) => { if (e.Node.Tag is string p && File.Exists(p)) OpenFileOrProject(p); };
        _tree.AfterSelect += (_, e) => { if (e.Node?.Tag is string p && (File.Exists(p) || Directory.Exists(p))) _grid.SelectedObject = new FileProps(p); };
        _tree.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter && _tree.SelectedNode?.Tag is string p && File.Exists(p)) { OpenFileOrProject(p); e.Handled = true; } };
        _tree.NodeMouseClick += (_, e) => { if (e.Button == MouseButtons.Right) { _tree.SelectedNode = e.Node; TreeMenu(e.Node).Show(_tree, e.Location); } };
        Stack(root, _tree, ts, filterHost);
        return root;
    }

    readonly System.Windows.Forms.Timer _filterTimer = new() { Interval = 300 };
    string CurrentFilter() { var s = _treeFilter?.Text.Trim(); return string.IsNullOrEmpty(s) ? null : s; }

    void OpenFileOrProject(string p)
    {
        string ext = Path.GetExtension(p).ToLowerInvariant();
        OpenFile(p);
    }

    ContextMenuStrip TreeMenu(TreeNode n)
    {
        var cm = new ContextMenuStrip { Font = new Font("Segoe UI", 9.5f) };
        string p = n.Tag as string;
        bool isFile = p != null && File.Exists(p), isDir = p != null && Directory.Exists(p);
        if (isFile) cm.Items.Add("Open", null, (_, _) => OpenFile(p));
        if (p != null && (isDir || isFile)) cm.Items.Add("Add New File...", null, (_, _) => NewFile());
        if (isFile && Path.GetExtension(p).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            cm.Items.Add("Set as Startup Project", null, (_, _) => { _startup = p; UpdateStartButton(); BuildTree(CurrentFilter()); });
            cm.Items.Add("Build", null, async (_, _) => { _startup = p; await BuildAsync(false, false); });
        }
        if (p != null) { cm.Items.Add(new ToolStripSeparator()); cm.Items.Add("Copy Full Path", null, (_, _) => { try { Clipboard.SetText(p); } catch { } }); cm.Items.Add("Open Folder in File Explorer", null, (_, _) => { try { Process.Start("explorer.exe", isFile ? $"/select,\"{p}\"" : $"\"{p}\""); } catch { } }); }
        cm.Items.Add(new ToolStripSeparator());
        cm.Items.Add("Refresh", null, (_, _) => BuildTree(CurrentFilter()));
        return cm;
    }

    void BuildTree(string filter)
    {
        if (_tree == null) return;
        _tree.BeginUpdate();
        try
        {
            _tree.Nodes.Clear();
            if (_rootDir == null) return;
            string name = Path.GetFileNameWithoutExtension(_slnPath.TrimEnd('\\', '/'));
            if (name.Length == 0) name = Path.GetFileName(_rootDir);
            if (_projects.Count == 0)
            {
                var folder = new TreeNode(Path.GetFileName(_rootDir.TrimEnd('\\', '/')) + " (folder)") { ImageKey = "folder", Tag = _rootDir };
                AddDir(folder.Nodes, _rootDir, filter);
                _tree.Nodes.Add(folder); folder.Expand();
                return;
            }
            var sol = new TreeNode($"Solution '{name}' ({_projects.Count} of {_projects.Count} project{(_projects.Count == 1 ? "" : "s")})") { ImageKey = "solution", Tag = _slnPath };
            _tree.Nodes.Add(sol);
            foreach (var proj in _projects)
            {
                var pn = new TreeNode(Path.GetFileNameWithoutExtension(proj)) { ImageKey = "proj", Tag = proj };
                if (proj == _startup) pn.NodeFont = new Font(_tree.Font, FontStyle.Bold);
                sol.Nodes.Add(pn);
                if (filter == null)
                {
                    var deps = new TreeNode("Dependencies") { ImageKey = "deps" };
                    try
                    {
                        var doc = XDocument.Load(proj);
                        var pk = new TreeNode("Packages") { ImageKey = "pkg" };
                        foreach (var r in doc.Descendants("PackageReference")) pk.Nodes.Add(new TreeNode($"{(string)r.Attribute("Include")} ({(string)r.Attribute("Version")})") { ImageKey = "pkg" });
                        if (pk.Nodes.Count > 0) deps.Nodes.Add(pk);
                        var fw = (string)doc.Descendants("TargetFramework").FirstOrDefault();
                        if (fw != null) deps.Nodes.Add(new TreeNode("Frameworks") { ImageKey = "deps" }.Also(f => f.Nodes.Add(new TreeNode("Microsoft.WindowsDesktop.App " + fw) { ImageKey = "ref" })));
                        foreach (var r in doc.Descendants("ProjectReference")) deps.Nodes.Add(new TreeNode(Path.GetFileNameWithoutExtension((string)r.Attribute("Include"))) { ImageKey = "proj" });
                    }
                    catch { }
                    if (deps.Nodes.Count > 0) pn.Nodes.Add(deps);
                }
                AddDir(pn.Nodes, Path.GetDirectoryName(proj), filter);
            }
            sol.Expand();
            foreach (TreeNode n in sol.Nodes) n.Expand();
            if (filter != null) _tree.ExpandAll();
        }
        finally { _tree.EndUpdate(); }
    }

    void AddDir(TreeNodeCollection nodes, string dir, string filter)
    {
        try
        {
            foreach (var d in Directory.GetDirectories(dir).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                string n = Path.GetFileName(d);
                if (IsExcluded(n)) continue;
                var node = new TreeNode(n) { ImageKey = "folder", Tag = d };
                if (filter == null) { node.Nodes.Add(new TreeNode("...") { Tag = "dummy" }); nodes.Add(node); }
                else { AddDir(node.Nodes, d, filter); if (node.Nodes.Count > 0) nodes.Add(node); }
            }
            foreach (var f in Directory.GetFiles(dir).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                string n = Path.GetFileName(f);
                if (filter != null && n.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (n.EndsWith(".csproj.user", StringComparison.OrdinalIgnoreCase)) continue;
                nodes.Add(new TreeNode(n) { ImageKey = Ico.KeyFor(f), Tag = f });
            }
        }
        catch { }
    }

    void Populate(TreeNode n)
    {
        if (n.Nodes.Count == 1 && n.Nodes[0].Tag as string == "dummy" && n.Tag is string dir)
        {
            n.Nodes.Clear(); AddDir(n.Nodes, dir, null);
        }
    }

    void SelectInTree(string path)
    {
        if (_tree == null || _tree.Nodes.Count == 0 || !_tree.Visible) return;
        try
        {
            foreach (TreeNode pn in _tree.Nodes[0].Nodes)
            {
                if (pn.Tag is not string proj) continue;
                string dir = Path.GetDirectoryName(proj);
                if (!path.StartsWith(dir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                var node = pn;
                foreach (var seg in path.Substring(dir.Length + 1).Split(Path.DirectorySeparatorChar))
                {
                    node.Expand();
                    node = node.Nodes.Cast<TreeNode>().FirstOrDefault(c => c.Text.Equals(seg, StringComparison.OrdinalIgnoreCase));
                    if (node == null) return;
                }
                if (_tree.SelectedNode != node) _tree.SelectedNode = node;
                node.EnsureVisible();
                return;
            }
        }
        catch { }
    }

    string SelectedDir()
    {
        if (_tree?.SelectedNode?.Tag is not string p) return null;
        if (Directory.Exists(p)) return p;
        if (File.Exists(p)) return Path.GetDirectoryName(p);
        return null;
    }

    // ===================================================================
    //  Properties
    // ===================================================================
    Control BuildProperties()
    {
        _grid = new PropertyGrid { PropertySort = PropertySort.Categorized, HelpVisible = false, ToolbarVisible = true };
        return _grid;
    }

    // ===================================================================
    //  Toolbox
    // ===================================================================
    Control BuildToolbox()
    {
        _toolboxTree = new ThemedTree { ImageList = Ico.FileImages(_t.TextMuted) };
        void Cat(string name, params (string n, string code)[] items)
        {
            var c = new TreeNode(name) { ImageKey = "folder" };
            foreach (var (n, code) in items) c.Nodes.Add(new TreeNode(n) { ImageKey = "component", Tag = code });
            _toolboxTree.Nodes.Add(c); c.Expand();
        }
        Cat("Krypton Standard Controls",
            ("KryptonButton", "new KryptonButton { Text = \"Button\", ButtonStyle = ButtonStyle.Custom1 }"),
            ("KryptonLabel", "new KryptonLabel { Text = \"Label\" }"),
            ("KryptonTextBox", "new KryptonTextBox { CueHint = { CueHintText = \"Type here\" } }"),
            ("KryptonComboBox", "new KryptonComboBox { DropDownStyle = ComboBoxStyle.DropDownList }"),
            ("KryptonCheckBox", "new KryptonCheckBox { Text = \"Check me\" }"),
            ("KryptonRadioButton", "new KryptonRadioButton { Text = \"Option\" }"),
            ("KryptonNumericUpDown", "new KryptonNumericUpDown { Minimum = 0, Maximum = 100 }"),
            ("KryptonDateTimePicker", "new KryptonDateTimePicker()"),
            ("KryptonListBox", "new KryptonListBox()"),
            ("KryptonRichTextBox", "new KryptonRichTextBox()"),
            ("KryptonDataGridView", "new KryptonDataGridView { Dock = DockStyle.Fill }"));
        Cat("Krypton Layout",
            ("KryptonPanel", "new KryptonPanel { Dock = DockStyle.Fill }"),
            ("KryptonGroupBox", "new KryptonGroupBox { Values = { Heading = \"Group\" } }"),
            ("KryptonSplitContainer", "new KryptonSplitContainer { Dock = DockStyle.Fill }"),
            ("KryptonHeader", "new KryptonHeader { Values = { Heading = \"Header\" } }"));
        Cat("Krypton Navigator / Docking",
            ("KryptonNavigator", "new KryptonNavigator { Dock = DockStyle.Fill }"),
            ("KryptonPage", "new KryptonPage { Text = \"Page\" }"),
            ("KryptonWorkspace", "new KryptonWorkspace { Dock = DockStyle.Fill }"),
            ("KryptonDockingManager", "new KryptonDockingManager()"),
            ("KryptonRibbon", "new KryptonRibbon { Dock = DockStyle.Top }"));
        Cat("Krypton Dialogs",
            ("KryptonMessageBox", "KMessage.Show(this, \"Hello\", \"Title\")"),
            ("KryptonForm", "public class MainForm : KryptonForm { }"),
            ("KryptonManager", "var manager = new KryptonManager();"));
        _toolboxTree.NodeMouseDoubleClick += (_, e) => { if (e.Node.Tag is string code && Ed != null) { Ed.InsertText(code); Ed.Focus(); } };
        return _toolboxTree;
    }

    // ===================================================================
    //  Error list
    // ===================================================================
    Control BuildErrorList()
    {
        var root = new KryptonPanel();
        var ts = MiniStrip();
        ts.Items.Add(new ToolStripLabel("Entire Solution") { Margin = new Padding(6, 1, 12, 2) });
        _tbErrors = new ToolStripButton("0 Errors") { CheckOnClick = true, Checked = true, DisplayStyle = ToolStripItemDisplayStyle.Text };
        _tbWarnings = new ToolStripButton("0 Warnings") { CheckOnClick = true, Checked = true, DisplayStyle = ToolStripItemDisplayStyle.Text };
        _tbInfo = new ToolStripButton("0 Messages") { CheckOnClick = true, Checked = true, DisplayStyle = ToolStripItemDisplayStyle.Text };
        foreach (var b in new[] { _tbErrors, _tbWarnings, _tbInfo }) { b.CheckedChanged += (_, _) => FillErrors(); ts.Items.Add(b); }
        _errList = new ThemedList { Dock = DockStyle.Fill };
        foreach (var (t, w) in new[] { ("Code", 110), ("Description", 560), ("Project", 130), ("File", 190), ("Line", 60) }) _errList.Columns.Add(t, w);
        _errList.RowColor = it => it.ImageKey == "error" ? (_t.Canvas.GetBrightness() < 0.5 ? Color.FromArgb(0xFF, 0x9A, 0x9A) : Color.FromArgb(0xB0, 0x1A, 0x1A)) : null;
        _errList.DoubleClick += (_, _) => { if (_errList.SelectedItems.Count > 0 && _errList.SelectedItems[0].Tag is Diag d) GotoDiag(d); };
        Stack(root, _errList, ts);
        return root;
    }

    void GotoDiag(Diag d) { if (File.Exists(d.File)) OpenFile(d.File, d.Line); }

    void FillErrors()
    {
        if (_errList == null) return;
        _errList.BeginUpdate(); _errList.Items.Clear();
        foreach (var d in _diags)
        {
            if (d.IsError && !_tbErrors.Checked) continue;
            if (!d.IsError && !_tbWarnings.Checked) continue;
            var it = new ListViewItem(new[] { d.Code, d.Message, d.Project, Path.GetFileName(d.File), d.Line.ToString() }) { ImageKey = d.IsError ? "error" : "warning", Tag = d };
            _errList.Items.Add(it);
        }
        _errList.EndUpdate();
        _tbErrors.Text = $"{_diags.Count(d => d.IsError)} Errors"; _tbWarnings.Text = $"{_diags.Count(d => !d.IsError)} Warnings"; _tbInfo.Text = "0 Messages";
        _errList.Fit();
    }

    void ApplyDiagnostics(Doc d)
    {
        d.View.Editor.Squiggles.Clear();
        foreach (var x in _diags.Where(x => string.Equals(x.File, d.Path, StringComparison.OrdinalIgnoreCase)))
            d.View.Editor.Squiggles[Math.Max(0, x.Line - 1)] = new Squiggle(x.IsError ? Severity.Error : Severity.Warning, $"{x.Code}: {x.Message}");
        d.View.Editor.Invalidate();
    }

    // ===================================================================
    //  Output
    // ===================================================================
    Control BuildOutput()
    {
        var root = new KryptonPanel();
        var ts = MiniStrip();
        ts.Items.Add(new ToolStripLabel("Show output from:") { Margin = new Padding(4, 1, 6, 2) });
        var cb = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        cb.Items.AddRange(new object[] { "Build", "Debug" }); cb.SelectedIndex = 0;
        cb.SelectedIndexChanged += (_, _) => { _outputBox.Visible = cb.SelectedIndex == 0; _debugBox.Visible = cb.SelectedIndex == 1; };
        ts.Items.Add(cb);
        ts.Items.Add(new ToolStripSeparator());
        MiniButton(ts, Ico.Delete, "Clear All", () => { _outputBox.Clear(); _debugBox.Clear(); });
        RichTextBox Box() => new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, Font = MonoFont(9.5f), WordWrap = false, DetectUrls = false, ScrollBars = RichTextBoxScrollBars.Both };
        _outputBox = Box(); _debugBox = Box(); _debugBox.Visible = false;
        var host = new KryptonPanel { Dock = DockStyle.Fill };
        host.Controls.Add(_outputBox); host.Controls.Add(_debugBox);
        Stack(root, host, ts);
        return root;
    }

    static Font MonoFont(float size)
    {
        var f = new Font("Cascadia Mono", size);
        if (f.Name != "Cascadia Mono") f = new Font("Consolas", size);
        return f;
    }

    void Out(string text, Color? color = null, bool debug = false)
    {
        if (InvokeRequired) { BeginInvoke(() => Out(text, color, debug)); return; }
        var box = debug ? _debugBox : _outputBox;
        box.SelectionStart = box.TextLength; box.SelectionLength = 0;
        box.SelectionColor = color ?? _t.Text;
        box.AppendText(text + "\n");
        box.SelectionColor = _t.Text;
        box.ScrollToCaret();
    }

    // ===================================================================
    //  Find in files
    // ===================================================================
    Control BuildFindResults()
    {
        var root = new KryptonPanel();
        var bar = new KryptonPanel { Dock = DockStyle.Top, Height = 40 };
        _findBox = new KryptonTextBox { Location = new Point(8, 7), Width = 300 };
        _findBox.CueHint.CueHintText = "Find in Files";
        _findBox.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await FindInFilesAsync(); } };
        _findCase = new KryptonCheckBox { Text = "Match case", Location = new Point(318, 10) };
        var go = new KryptonButton { Text = "Find All", ButtonStyle = ButtonStyle.Custom1, Location = new Point(420, 6), Size = new Size(84, 28) };
        go.Click += async (_, _) => await FindInFilesAsync();
        _findStatus = new KryptonLabel { LabelStyle = LabelStyle.Custom1, Location = new Point(514, 10) };
        bar.Controls.AddRange(new Control[] { _findBox, _findCase, go, _findStatus });
        _findList = new ThemedList { Dock = DockStyle.Fill };
        foreach (var (t, w) in new[] { ("File", 280), ("Line", 60), ("Text", 700) }) _findList.Columns.Add(t, w);
        _findList.DoubleClick += (_, _) =>
        {
            if (_findList.SelectedItems.Count > 0 && _findList.SelectedItems[0].Tag is (string f, int line)) OpenFile(f, line, _findBox.Text);
        };
        Stack(root, _findList, bar);
        return root;
    }

    static readonly HashSet<string> TextExt = new(StringComparer.OrdinalIgnoreCase) { ".cs", ".csproj", ".slnx", ".sln", ".json", ".xml", ".md", ".txt", ".props", ".targets", ".config", ".xaml", ".html", ".ps1", ".bat", ".sh", ".gitignore", ".resx", ".yml", ".yaml", ".css", ".js", ".ts" };

    async Task FindInFilesAsync()
    {
        string term = _findBox.Text; if (term.Length == 0 || _rootDir == null) return;
        bool mc = _findCase.Checked; string root = _rootDir;
        _findStatus.Values.Text = "Searching...";
        var res = await Task.Run(() =>
        {
            var list = new List<(string f, int line, string text)>();
            var cmp = mc ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            void Rec(string d)
            {
                try
                {
                    foreach (var f in Directory.GetFiles(d))
                    {
                        if (list.Count >= 2000) return;
                        if (!TextExt.Contains(Path.GetExtension(f)) && !Path.GetFileName(f).StartsWith(".git")) continue;
                        try { if (new FileInfo(f).Length > 2_000_000) continue; int n = 0; foreach (var line in File.ReadLines(f)) { n++; if (line.IndexOf(term, cmp) >= 0) list.Add((f, n, line.Trim())); } } catch { }
                    }
                    foreach (var s in Directory.GetDirectories(d)) if (!IsExcluded(Path.GetFileName(s))) Rec(s);
                }
                catch { }
            }
            Rec(root); return list;
        });
        _findList.BeginUpdate(); _findList.Items.Clear();
        foreach (var (f, line, text) in res)
            _findList.Items.Add(new ListViewItem(new[] { Path.GetRelativePath(root, f), line.ToString(), text }) { ImageKey = Ico.KeyFor(f), Tag = (f, line) });
        _findList.EndUpdate(); _findList.Fit();
        _findStatus.Values.Text = $"Matching lines: {res.Count}  Matching files: {res.Select(r => r.f).Distinct().Count()}";
    }

    // ===================================================================
    //  Terminal
    // ===================================================================
    Control BuildTerminal()
    {
        var root = new KryptonPanel();
        _termBox = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, Font = MonoFont(9.5f), DetectUrls = false, WordWrap = true };
        var inputHost = new KryptonPanel { Dock = DockStyle.Bottom, Height = 38, Padding = new Padding(6, 5, 6, 5) };
        _termInput = new KryptonTextBox { Dock = DockStyle.Fill, StateCommon = { Content = { Font = MonoFont(9.5f) } } };
        _termInput.CueHint.CueHintText = "PowerShell command, Enter to run";
        _termInput.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; var c = _termInput.Text; _termInput.Clear(); if (c.Trim().Length > 0) { _termHistory.Add(c); _termHistoryPos = _termHistory.Count; RunTerm(c); } }
            else if (e.KeyCode == Keys.Up && _termHistory.Count > 0) { _termHistoryPos = Math.Max(0, _termHistoryPos - 1); _termInput.Text = _termHistory[_termHistoryPos]; e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Down && _termHistory.Count > 0) { _termHistoryPos = Math.Min(_termHistory.Count, _termHistoryPos + 1); _termInput.Text = _termHistoryPos < _termHistory.Count ? _termHistory[_termHistoryPos] : ""; e.SuppressKeyPress = true; }
        };
        inputHost.Controls.Add(_termInput);
        root.Controls.Add(_termBox); root.Controls.Add(inputHost);
        root.Controls.SetChildIndex(_termBox, 0); root.Controls.SetChildIndex(inputHost, 1);
        return root;
    }

    void TermWrite(string text, Color? c = null)
    {
        if (InvokeRequired) { BeginInvoke(() => TermWrite(text, c)); return; }
        _termBox.SelectionStart = _termBox.TextLength; _termBox.SelectionColor = c ?? _t.Text; _termBox.AppendText(text + "\n"); _termBox.SelectionColor = _t.Text; _termBox.ScrollToCaret();
    }

    void RunTerm(string cmd)
    {
        _termDir ??= _rootDir ?? Environment.CurrentDirectory;
        TermWrite($"PS {_termDir}> {cmd}", _t.Accent);
        var m = Regex.Match(cmd.Trim(), @"^(?:cd|set-location)\s+(.+)$", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var target = m.Groups[1].Value.Trim('"');
            var p = Path.GetFullPath(Path.Combine(_termDir, target));
            if (Directory.Exists(p)) _termDir = p; else TermWrite("Cannot find path: " + p, Color.IndianRed);
            return;
        }
        var dir = _termDir;
        Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe") { WorkingDirectory = dir, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                psi.ArgumentList.Add("-NoLogo"); psi.ArgumentList.Add("-NoProfile"); psi.ArgumentList.Add("-NonInteractive"); psi.ArgumentList.Add("-Command"); psi.ArgumentList.Add(cmd);
                using var p = Process.Start(psi);
                p.OutputDataReceived += (_, e) => { if (e.Data != null) TermWrite(e.Data); };
                p.ErrorDataReceived += (_, e) => { if (e.Data != null) TermWrite(e.Data, Color.IndianRed); };
                p.BeginOutputReadLine(); p.BeginErrorReadLine();
                if (!p.WaitForExit(120000)) { try { p.Kill(true); } catch { } TermWrite("(timed out)", Color.IndianRed); }
            }
            catch (Exception ex) { TermWrite(ex.Message, Color.IndianRed); }
        });
    }

    // ===================================================================
    //  Git
    // ===================================================================
    Control BuildGitPanel()
    {
        var root = new KryptonPanel();
        _gitHeader = new KryptonLabel { Dock = DockStyle.Top, LabelStyle = LabelStyle.BoldPanel, Values = { Text = "No repository" }, Padding = new Padding(8, 6, 0, 6) };
        _gitList = new ThemedList { Dock = DockStyle.Fill };
        _gitList.Columns.Add("Status", 90); _gitList.Columns.Add("File", 300);
        _gitList.DoubleClick += (_, _) => { if (_gitList.SelectedItems.Count > 0 && _gitList.SelectedItems[0].Tag is string f) OpenFile(f); };
        _gitLog = new ThemedList { Dock = DockStyle.Fill };
        _gitLog.Columns.Add("Commit", 70); _gitLog.Columns.Add("Message", 240); _gitLog.Columns.Add("When", 90);
        var split = new KryptonSplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        split.Panel1.Controls.Add(_gitList); split.Panel2.Controls.Add(_gitLog);
        split.SplitterDistance = 220;
        Stack(root, split, _gitHeader);
        return root;
    }

    static string RunGit(string dir, params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo("git") { WorkingDirectory = dir, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8 };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi);
            var o = p.StandardOutput.ReadToEnd(); p.StandardError.ReadToEnd();
            if (!p.WaitForExit(8000)) { try { p.Kill(true); } catch { } return null; }
            return p.ExitCode == 0 ? o : null;
        }
        catch { return null; }
    }

    async void RefreshGit()
    {
        if (_rootDir == null || _gitList == null) return;
        string root = Directory.Exists(_rootDir) ? _rootDir : Path.GetDirectoryName(_rootDir);
        var (branch, status, log) = await Task.Run(() => (
            RunGit(root, "rev-parse", "--abbrev-ref", "HEAD")?.Trim(),
            RunGit(root, "status", "--porcelain"),
            RunGit(root, "log", "-15", "--pretty=format:%h%x09%s%x09%ar")));
        if (IsDisposed) return;
        _gitList.BeginUpdate(); _gitList.Items.Clear(); _gitLog.Items.Clear();
        if (branch == null)
        {
            _gitHeader.Values.Text = "Not a git repository"; _status.Set(_sBranch, "⎇ no repo");
        }
        else
        {
            _gitHeader.Values.Text = $"⎇ {branch}";
            _status.Set(_sBranch, "⎇ " + branch);
            string top = RunGitTop(root);
            foreach (var line in (status ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length < 4) continue;
                string xy = line.Substring(0, 2), rel = line.Substring(3).Trim('"');
                if (rel.Contains(" -> ")) rel = rel.Substring(rel.IndexOf(" -> ") + 4);
                string st = xy.Contains('?') ? "Untracked" : xy.Contains('A') ? "Added" : xy.Contains('D') ? "Deleted" : xy.Contains('R') ? "Renamed" : "Modified";
                string full = Path.GetFullPath(Path.Combine(top ?? root, rel));
                _gitList.Items.Add(new ListViewItem(new[] { st, rel }) { ImageKey = Ico.KeyFor(full), Tag = full });
            }
            foreach (var line in (log ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var p = line.Split('\t'); if (p.Length >= 3) _gitLog.Items.Add(new ListViewItem(new[] { p[0], p[1], p[2] }));
            }
        }
        _gitList.EndUpdate(); _gitList.Fit(); _gitLog.Fit();
    }

    static string RunGitTop(string dir) => RunGit(dir, "rev-parse", "--show-toplevel")?.Trim().Replace('/', Path.DirectorySeparatorChar);
}

static class Ext
{
    public static T Also<T>(this T t, Action<T> a) { a(t); return t; }
}
