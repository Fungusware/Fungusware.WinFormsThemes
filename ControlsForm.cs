// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using Krypton.Toolkit;

namespace KryptonModern;

/// <summary>Every control type Xackup uses, left unstyled except for the global palette.
/// Shows which ones follow the theme and which need work.</summary>
public class ControlsForm : KryptonForm
{
    readonly KryptonManager mgr;
    public ControlsForm()
    {
        Text = "Xackup control check";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1100, 760);
        AutoScaleMode = AutoScaleMode.Dpi;
        var t = ModernTheme.All[int.TryParse(Environment.GetEnvironmentVariable("KM_THEME"), out var ti) ? ti : 0];
        mgr = new KryptonManager { GlobalPaletteMode = PaletteMode.Custom };
        mgr.GlobalCustomPalette = ModernTheme.Build(t);
        BackColor = t.Canvas;

        var root = new KryptonPanel { Dock = DockStyle.Fill };
        Controls.Add(root);

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(16) };
        for (int i = 0; i < 3; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        root.Controls.Add(grid);

        FlowLayoutPanel Col()
        {
            var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            grid.Controls.Add(f);
            return f;
        }
        var c1 = Col(); var c2 = Col(); var c3 = Col();

        // ---------------- column 1: Krypton controls ----------------------
        c1.Controls.Add(new KryptonHeader { Text = "KryptonHeader", Values = { Description = "Section title" }, Width = 320 });
        c1.Controls.Add(new KryptonLinkLabel { Text = "KryptonLinkLabel: open the log" });
        c1.Controls.Add(new KryptonWrapLabel { Text = "KryptonWrapLabel: longer text that wraps across more than one line so we can see the colour.", MaximumSize = new Size(320, 0) });
        var lb = new KryptonListBox { Width = 320, Height = 90 };
        lb.Items.AddRange(new object[] { "Nightly VM backup", "Weekly archive", "Test job" });
        lb.SelectedIndex = 1;
        c1.Controls.Add(lb);
        c1.Controls.Add(new KryptonDropButton { Text = "KryptonDropButton", Width = 190, Height = 34 });
        c1.Controls.Add(new KryptonCheckButton { Text = "KryptonCheckButton", Width = 190, Height = 34, Checked = true });
        c1.Controls.Add(new KryptonCheckButton { Text = "CheckButton (off)", Width = 190, Height = 34 });
        c1.Controls.Add(new KryptonTrackBar { Width = 320, Value = 4 });
        c1.Controls.Add(new KryptonTextBox { Width = 320, Multiline = true, Height = 60, Text = "Multiline KryptonTextBox\r\nsecond line" });
        c1.Controls.Add(new KryptonTextBox { Width = 320, Enabled = false, Text = "Disabled text box" });
        c1.Controls.Add(new KryptonCheckBox { Text = "Disabled check box", Enabled = false, Checked = true });
        c1.Controls.Add(new KryptonRadioButton { Text = "Disabled radio", Enabled = false });
        c1.Controls.Add(new KryptonButton { Text = "Disabled button", Enabled = false, Width = 160, Height = 34 });
        var ctx = new KryptonContextMenu();
        ctx.Items.Add(new KryptonContextMenuItems());
        c1.Controls.Add(new KryptonSplitContainer { Width = 320, Height = 80 });

        // ---------------- column 2: plain WinForms ------------------------
        var menu = new MenuStrip();
        menu.Items.Add(new ToolStripMenuItem("File", null, new ToolStripMenuItem("Open"), new ToolStripMenuItem("Exit")));
        menu.Items.Add(new ToolStripMenuItem("Help"));
        menu.Dock = DockStyle.None; menu.Width = 320;
        c2.Controls.Add(menu);

        var tool = new ToolStrip { Dock = DockStyle.None, Width = 320 };
        tool.Items.Add(new ToolStripButton("Run"));
        tool.Items.Add(new ToolStripButton("Stop"));
        tool.Items.Add(new ToolStripSeparator());
        tool.Items.Add(new ToolStripLabel("ToolStrip"));
        c2.Controls.Add(tool);

        var gb = new GroupBox { Text = "GroupBox (WinForms)", Width = 320, Height = 70 };
        gb.Controls.Add(new Label { Text = "plain Label inside", Location = new Point(12, 28), AutoSize = true });
        c2.Controls.Add(gb);

        var lv = new ListView { Width = 320, Height = 110, View = View.Details, FullRowSelect = true };
        lv.Columns.Add("Job", 130); lv.Columns.Add("Result", 90);
        lv.Items.Add(new ListViewItem(new[] { "web-01", "Success" }));
        lv.Items.Add(new ListViewItem(new[] { "db-prod", "Failed" }) { Selected = true });
        c2.Controls.Add(lv);

        c2.Controls.Add(new ProgressBar { Width = 320, Value = 60 });

        var tabs = new TabControl { Width = 320, Height = 90 };
        tabs.TabPages.Add("General"); tabs.TabPages.Add("Advanced");
        c2.Controls.Add(tabs);

        c2.Controls.Add(new RichTextBox { Width = 320, Height = 70, Text = "RichTextBox (log viewer)" });
        c2.Controls.Add(new Button { Text = "Plain Button", Width = 130, Height = 32 });
        c2.Controls.Add(new CheckBox { Text = "Plain CheckBox", AutoSize = true });
        c2.Controls.Add(new TextBox { Width = 320, Text = "Plain TextBox" });
        c2.Controls.Add(new ComboBox { Width = 320, Text = "Plain ComboBox" });
        c2.Controls.Add(new DateTimePicker { Width = 320 });

        // ---------------- column 3: dialogs & menus -----------------------
        c3.Controls.Add(new KryptonLabel { Text = "Right-click the grey box for a context menu", LabelStyle = LabelStyle.BoldPanel });
        var cm = new ContextMenuStrip();
        cm.Items.Add("Run now"); cm.Items.Add("Edit job"); cm.Items.Add(new ToolStripSeparator()); cm.Items.Add("Delete");
        var area = new Panel { Width = 320, Height = 60, BackColor = Color.Gray, ContextMenuStrip = cm };
        c3.Controls.Add(area);

        var kcm = new KryptonContextMenu();
        var items = new KryptonContextMenuItems();
        items.Items.Add(new KryptonContextMenuItem("Krypton item one"));
        items.Items.Add(new KryptonContextMenuSeparator());
        items.Items.Add(new KryptonContextMenuCheckBox { Text = "Krypton check item", Checked = true });
        kcm.Items.Add(items);
        var kbtn = new KryptonButton { Text = "Show KryptonContextMenu", Width = 220, Height = 34 };
        kbtn.Click += (_, _) => kcm.Show(kbtn, new Point(0, kbtn.Height));
        c3.Controls.Add(kbtn);

        var msg = new KryptonButton { Text = "Show KryptonMessageBox", Width = 220, Height = 34 };
        msg.Click += (_, _) => KryptonMessageBox.Show(this, "Delete this job?\nThis cannot be undone.", "Xackup",
            KryptonMessageBoxButtons.YesNo, KryptonMessageBoxIcon.Question);
        c3.Controls.Add(msg);

        var native = new KryptonButton { Text = "Show plain MessageBox", Width = 220, Height = 34 };
        native.Click += (_, _) => MessageBox.Show(this, "Plain WinForms MessageBox", "Xackup", MessageBoxButtons.YesNo);
        c3.Controls.Add(native);

        c3.Controls.Add(new KryptonBorderEdge { Width = 320, Height = 3 });
        c3.Controls.Add(new KryptonNumericUpDown { Width = 120, Value = 5 });
        c3.Controls.Add(new KryptonDateTimePicker { Width = 280 });
        var sp = new SplitContainer { Width = 320, Height = 70 };
        c3.Controls.Add(sp);
        var pg = new PropertyGrid { Width = 320, Height = 120, SelectedObject = new Button() };
        var picker = new KryptonComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        foreach (var th in ModernTheme.All) picker.Items.Add(th.Name);
        c3.Controls.Add(picker);
        picker.SelectedIndexChanged += (_, _) =>
        {
            var tk = ModernTheme.All[picker.SelectedIndex];
            var np = ModernTheme.Build(tk);
            mgr.GlobalCustomPalette = np;
            BackColor = tk.Canvas;
            ThemeApplier.Apply(this, tk);
        };
        picker.SelectedIndex = ModernTheme.All.ToList().IndexOf(t);
        Shown += (_, _) => ThemeApplier.Apply(this, ModernTheme.All[picker.SelectedIndex]);
        c3.Controls.Add(pg);
    }
}
