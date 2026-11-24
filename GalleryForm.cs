// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using Krypton.Docking;
using Krypton.Navigator;
using Krypton.Ribbon;
using Krypton.Toolkit;
using Krypton.Workspace;

namespace Fungusware.WinFormsThemes.Demo;

/// <summary>
/// Shows the wider Krypton suite (Ribbon, Navigator, Workspace, Docking) under every theme.
/// Open with KM_FORM=3.
/// </summary>
public class GalleryForm : KryptonForm
{
    readonly KryptonManager _manager = new();
    KryptonCustomPaletteBase _palette;
    KryptonComboBox _picker;
    Tokens _pending;

    public GalleryForm()
    {
        Text = "Krypton Modern - suite gallery";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1240, 780);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildUi();
        var start = int.TryParse(Environment.GetEnvironmentVariable("KM_THEME"), out var t0) ? t0 : 0;
        _picker.SelectedIndex = start;
        ApplyTheme(ModernTheme.All[start]);
        Shown += (_, _) => ApplyTheme(ModernTheme.All[_picker.SelectedIndex]); // ThemeApplier needs the form shown
    }

    void BuildUi()
    {
        // ---- ribbon --------------------------------------------------
        var ribbon = new KryptonRibbon { Dock = DockStyle.Top };
        var tabHome = new KryptonRibbonTab { Text = "Home" };
        var g1 = new KryptonRibbonGroup { TextLine1 = "Clipboard" };
        var tri = new KryptonRibbonGroupTriple();
        tri.Items.Add(new KryptonRibbonGroupButton { TextLine1 = "Paste", ImageLarge = GlyphIcon("Paste"[..1]), ImageSmall = GlyphIcon("Paste"[..1], 16) });
        g1.Items.Add(tri);
        var tri2 = new KryptonRibbonGroupTriple();
        tri2.Items.Add(new KryptonRibbonGroupButton { TextLine1 = "Cut", ImageLarge = GlyphIcon("Cut"[..1]), ImageSmall = GlyphIcon("Cut"[..1], 16) });
        tri2.Items.Add(new KryptonRibbonGroupButton { TextLine1 = "Copy", ImageLarge = GlyphIcon("Copy"[..1]), ImageSmall = GlyphIcon("Copy"[..1], 16) });
        tri2.Items.Add(new KryptonRibbonGroupButton { TextLine1 = "Format", ImageLarge = GlyphIcon("Format"[..1]), ImageSmall = GlyphIcon("Format"[..1], 16) });
        g1.Items.Add(tri2);
        var g2 = new KryptonRibbonGroup { TextLine1 = "Options" };
        var tri3 = new KryptonRibbonGroupTriple();
        tri3.Items.Add(new KryptonRibbonGroupCheckBox { TextLine1 = "Compress", Checked = true });
        tri3.Items.Add(new KryptonRibbonGroupCheckBox { TextLine1 = "Verify" });
        tri3.Items.Add(new KryptonRibbonGroupRadioButton { TextLine1 = "Daily", Checked = true });
        g2.Items.Add(tri3);
        var tri4 = new KryptonRibbonGroupTriple();
        var rc = new KryptonRibbonGroupComboBox { Text = "SFTP" };
        rc.Items.AddRange(new object[] { "SFTP", "FTPS", "SMB share" });
        tri4.Items.Add(rc);
        tri4.Items.Add(new KryptonRibbonGroupTextBox { Text = "host.example.com" });
        tri4.Items.Add(new KryptonRibbonGroupDateTimePicker());
        g2.Items.Add(tri4);
        tabHome.Groups.Add(g1); tabHome.Groups.Add(g2);
        var tabView = new KryptonRibbonTab { Text = "View" };
        var g3 = new KryptonRibbonGroup { TextLine1 = "Layout" };
        var tri5 = new KryptonRibbonGroupTriple();
        tri5.Items.Add(new KryptonRibbonGroupButton { TextLine1 = "Zoom in", ImageLarge = GlyphIcon("Zoom in"[..1]), ImageSmall = GlyphIcon("Zoom in"[..1], 16) });
        tri5.Items.Add(new KryptonRibbonGroupButton { TextLine1 = "Zoom out", ImageLarge = GlyphIcon("Zoom out"[..1]), ImageSmall = GlyphIcon("Zoom out"[..1], 16) });
        g3.Items.Add(tri5);
        tabView.Groups.Add(g3);
        ribbon.RibbonTabs.Add(tabHome); ribbon.RibbonTabs.Add(tabView);
        ribbon.SelectedTab = tabHome;
        Controls.Add(ribbon);

        // ---- status bar ----------------------------------------------
        var status = new KryptonStatusStrip { Dock = DockStyle.Bottom };
        status.Items.Add(new ToolStripStatusLabel("Ready"));
        Controls.Add(status);

        // ---- theme picker bar ----------------------------------------
        var bar = new KryptonPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(16, 6, 16, 6) };
        _picker = new KryptonComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Dock = DockStyle.Right };
        foreach (var th in ModernTheme.All) _picker.Items.Add(th.Name);
        _picker.SelectedIndexChanged += (_, _) =>
        {
            if (!IsHandleCreated) return; // initial selection is applied by the constructor
            var th = ModernTheme.All[_picker.SelectedIndex];
            // apply after the native drop-down has closed (see README "Gotchas")
            BeginInvoke(() => { if (!_picker.DroppedDown) ApplyTheme(th); else _pending = th; });
        };
        _picker.DropDownClosed += (_, _) => { if (_pending != null) { var th = _pending; _pending = null; BeginInvoke(() => ApplyTheme(th)); } };
        bar.Controls.Add(_picker);
        bar.Controls.Add(new KryptonLabel { Text = "Theme", Dock = DockStyle.Right });
        Controls.Add(bar);

        // ---- main navigator (tabs) -----------------------------------
        var nav = new KryptonNavigator { Dock = DockStyle.Fill };
        nav.Pages.Add(NavigatorPage());
        nav.Pages.Add(WorkspacePage());
        nav.Pages.Add(DockingPage());
        nav.Pages.Add(OutlookPage());
        Controls.Add(nav);
        nav.BringToFront();
        if (int.TryParse(Environment.GetEnvironmentVariable("KM_PAGE"), out var pg)) nav.SelectedIndex = pg; // dev: open on a given tab
    }

    static KryptonPage Page(string text, Control content = null)
    {
        var p = new KryptonPage { Text = text, TextTitle = text, TextDescription = text + " page" };
        if (content != null) { content.Dock = DockStyle.Fill; p.Controls.Add(content); }
        return p;
    }

    static Image GlyphIcon(string glyph, int size = 32)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        using var b = new SolidBrush(Color.FromArgb(0x5B, 0x8D, 0xFF));
        using var path = CardPanel.Round(new RectangleF(1, 1, size - 2, size - 2), size / 4f);
        g.FillPath(b, path);
        using var f = new Font("Segoe UI", size * 0.42f, FontStyle.Bold, GraphicsUnit.Pixel);
        var sz = g.MeasureString(glyph, f);
        g.DrawString(glyph, f, Brushes.White, (size - sz.Width) / 2, (size - sz.Height) / 2);
        return bmp;
    }

    static Control Sample(string text)
    {
        var panel = new KryptonPanel { Padding = new Padding(16) };
        panel.Controls.Add(new KryptonLabel { Text = text, LabelStyle = LabelStyle.BoldPanel, Location = new Point(16, 16) });
        panel.Controls.Add(new KryptonTextBox { Text = "Text box", Location = new Point(16, 48), Width = 220 });
        var cb = new KryptonComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(16, 84), Width = 220 };
        cb.Items.AddRange(new object[] { "One", "Two", "Three" }); cb.SelectedIndex = 0;
        panel.Controls.Add(cb);
        panel.Controls.Add(new KryptonCheckBox { Text = "Check box", Checked = true, Location = new Point(16, 120) });
        panel.Controls.Add(new KryptonButton { Text = "Button", Location = new Point(16, 152), Size = new Size(110, 32), ButtonStyle = ButtonStyle.Custom1 });
        return panel;
    }

    // ---- Navigator: the common display modes -----------------------------
    KryptonPage NavigatorPage()
    {
        var grid = new TableLayoutPanel { ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent, Padding = new Padding(12) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        KryptonNavigator Make(NavigatorMode mode, int pages = 3)
        {
            var n = new KryptonNavigator { Dock = DockStyle.Fill, NavigatorMode = mode, Margin = new Padding(6) };
            for (int i = 1; i <= pages; i++) n.Pages.Add(Page($"Page {i}", Sample($"{mode} - page {i}")));
            return n;
        }
        grid.Controls.Add(Make(NavigatorMode.BarTabGroup), 0, 0);
        grid.Controls.Add(Make(NavigatorMode.BarRibbonTabGroup), 1, 0);
        grid.Controls.Add(Make(NavigatorMode.BarCheckButtonGroupOutside), 0, 1);
        grid.Controls.Add(Make(NavigatorMode.HeaderBarCheckButtonGroup), 1, 1);
        return Page("Navigator", grid);
    }

    // ---- Workspace: split cells ------------------------------------------
    KryptonPage WorkspacePage()
    {
        var ws = new KryptonWorkspace { Dock = DockStyle.Fill };
        ws.Root.Orientation = Orientation.Horizontal;
        var left = new KryptonWorkspaceCell();
        left.Pages.Add(Page("Jobs", Sample("Jobs"))); left.Pages.Add(Page("Schedules", Sample("Schedules")));
        var seq = new KryptonWorkspaceSequence(Orientation.Vertical);
        var c1 = new KryptonWorkspaceCell(); c1.Pages.Add(Page("History", Sample("History")));
        var c2 = new KryptonWorkspaceCell(); c2.Pages.Add(Page("Log", Sample("Log"))); c2.Pages.Add(Page("Alerts", Sample("Alerts")));
        seq.Children.Add(c1); seq.Children.Add(c2);
        ws.Root.Children.Add(left); ws.Root.Children.Add(seq);
        return Page("Workspace", ws);
    }

    // ---- Docking: dockspaces + auto-hide + floating ----------------------
    KryptonPage DockingPage()
    {
        var host = new KryptonPanel();
        var dockWs = new KryptonDockableWorkspace { Dock = DockStyle.Fill };
        host.Controls.Add(dockWs);
        var dm = new KryptonDockingManager();
        var w = dm.ManageWorkspace("Workspace", dockWs);
        dm.ManageControl("Control", host, w);
        dm.ManageFloating(this);
        dm.AddDockspace("Control", DockingEdge.Left, new[] { Page("Explorer", Sample("Explorer")), Page("Search", Sample("Search")) });
        dm.AddDockspace("Control", DockingEdge.Bottom, new[] { Page("Output", Sample("Output")), Page("Errors", Sample("Errors")) });
        dm.AddAutoHiddenGroup("Control", DockingEdge.Right, new[] { Page("Properties", Sample("Properties")) });
        dm.AddToWorkspace("Workspace", new[] { Page("Document 1", Sample("Document 1")), Page("Document 2", Sample("Document 2")) });
        return Page("Docking", host);
    }

    // ---- Outlook-style stack ---------------------------------------------
    KryptonPage OutlookPage()
    {
        var nav = new KryptonNavigator { Dock = DockStyle.Fill, NavigatorMode = NavigatorMode.OutlookFull };
        nav.Pages.Add(Page("Mail", Sample("Mail")));
        nav.Pages.Add(Page("Calendar", Sample("Calendar")));
        nav.Pages.Add(Page("Contacts", Sample("Contacts")));
        nav.Pages.Add(Page("Tasks", Sample("Tasks")));
        return Page("Outlook", nav);
    }

    void ApplyTheme(Tokens t)
    {
        var old = _palette;
        _palette = KryptonTheme.Build(t);
        _manager.GlobalPaletteMode = PaletteMode.Custom;
        _manager.GlobalCustomPalette = _palette;
        old?.Dispose();
        BackColor = t.Canvas;
        ThemeApplier.Apply(this, t);
    }
}
