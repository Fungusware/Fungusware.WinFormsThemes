// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using Krypton.Toolkit;
using System.Drawing.Drawing2D;

namespace KryptonModern;

public class DemoForm : KryptonForm
{
    readonly KryptonManager _manager = new();
    KryptonCustomPaletteBase _palette;
    KryptonComboBox picker, combo;
    Tokens _t = ModernTheme.Light, _pending;

    readonly List<CardPanel> _cards = new();
    readonly List<PillProgress> _bars = new();
    readonly List<Control> _surfaceControls = new();
    readonly System.Windows.Forms.Timer _timer = new() { Interval = 60 };

    public DemoForm()
    {
        Text = "Krypton Modern";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1040, 700);
        MinimumSize = new Size(900, 620);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildUi();
        ApplyTheme(ModernTheme.All[int.TryParse(Environment.GetEnvironmentVariable("KM_THEME"), out var t0) ? t0 : 0]);
        Shown += (_, _) => ApplyTheme(_t); // ThemeApplier needs the form shown to reach its child controls

        _timer.Tick += (_, _) => { foreach (var b in _bars) b.Value = (b.Value + 1) % 101; };
        _timer.Start();
    }

    // ------------------------------------------------------------------
    void BuildUi()
    {
        var root = new KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(0) };
        Controls.Add(root);

        // ---- side navigation ------------------------------------------
        var nav = new KryptonPanel
        {
            Dock = DockStyle.Left, Width = 210,
            PanelBackStyle = PaletteBackStyle.PanelCustom2,
            Padding = new Padding(16, 20, 8, 16)
        };
        var navStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, BackColor = Color.Transparent
        };
        navStack.Controls.Add(new KryptonLabel { Text = "Backups", LabelStyle = LabelStyle.BoldPanel, Margin = new Padding(8, 0, 0, 12) });
        foreach (var name in new[] { "Overview", "Jobs", "Destinations", "Schedules", "History", "Settings" })
        {
            navStack.Controls.Add(new KryptonButton
            {
                Text = name, ButtonStyle = ButtonStyle.Custom2,
                Size = new Size(184, 36), Margin = new Padding(0, 0, 0, 2), TabStop = false,
                StateCommon = { Content = { ShortText = { TextH = PaletteRelativeAlign.Near } } },
                Values = { Text = name }
            });
        }
        nav.Controls.Add(navStack);
        root.Controls.Add(nav);

        // ---- content ---------------------------------------------------
        var content = new KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(8, 20, 28, 24) };
        root.Controls.Add(content);
        content.BringToFront();

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
            BackColor = Color.Transparent
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(grid);

        // header row
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(new KryptonLabel
        {
            Text = "Settings", LabelStyle = LabelStyle.TitlePanel, Anchor = AnchorStyles.Left
        }, 0, 0);
        picker = new KryptonComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190, Anchor = AnchorStyles.Right };
        foreach (var th in ModernTheme.All) picker.Items.Add(th.Name);
        picker.SelectedIndex = int.TryParse(Environment.GetEnvironmentVariable("KM_THEME"), out var ti) ? ti : 0;
        // Apply after the native drop-down has finished closing: changing fonts/palette while it is open crashes the combo (AccessViolation).
        picker.SelectedIndexChanged += (_, _) => { var th = ModernTheme.All[picker.SelectedIndex]; BeginInvoke(() => { if (!picker.DroppedDown) ApplyTheme(th); else _pending = th; }); };
        picker.DropDownClosed += (_, _) => { if (_pending != null) { var th = _pending; _pending = null; BeginInvoke(() => ApplyTheme(th)); } };
        header.Controls.Add(picker, 1, 0);
        grid.Controls.Add(header, 0, 0);
        grid.SetColumnSpan(header, 2);

        // ---- card 1: profile form -------------------------------------
        var profile = NewCard(new Padding(0, 0, 10, 0));
        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, BackColor = Color.Transparent, AutoScroll = true
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        void Row(Control c, int top = 0) { c.Margin = new Padding(0, top, 0, 0); c.Dock = DockStyle.Top; form.Controls.Add(c); }

        Row(new KryptonLabel { Text = "Server connection", LabelStyle = LabelStyle.BoldPanel });
        Row(new KryptonLabel { Text = "Where Xackup stores finished backups.", LabelStyle = LabelStyle.Custom1 });
        Row(new KryptonLabel { Text = "Host name" }, 14);
        Row(new KryptonTextBox { Text = "backup01.example.com" }, 2);
        Row(new KryptonLabel { Text = "Protocol" }, 12);
        combo = new KryptonComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        combo.Items.AddRange(new object[] { "SFTP", "FTPS", "SMB share", "Local folder" });
        combo.SelectedIndex = 0;
        Row(combo, 2);
        Row(new KryptonLabel { Text = "Keep backups for (days)" }, 12);
        Row(new KryptonNumericUpDown { Value = 30, Minimum = 1, Maximum = 365 }, 2);
        Row(new KryptonCheckBox { Text = "Compress before upload", Checked = true }, 14);
        Row(new KryptonCheckBox { Text = "Verify after copy" }, 6);
        Row(new KryptonRadioButton { Text = "Run on a schedule", Checked = true }, 12);
        Row(new KryptonRadioButton { Text = "Run only when I start it" }, 4);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent
        };
        buttons.Controls.Add(new KryptonButton { Text = "Save changes", ButtonStyle = ButtonStyle.Custom1, Size = new Size(124, 34) });
        buttons.Controls.Add(new KryptonButton { Text = "Test connection", ButtonStyle = ButtonStyle.Standalone, Size = new Size(140, 34) });
        buttons.Controls.Add(new KryptonButton { Text = "Cancel", ButtonStyle = ButtonStyle.Standalone, Size = new Size(76, 34) });
        Row(buttons, 18);

        profile.Controls.Add(form);
        grid.Controls.Add(profile, 0, 1);

        // ---- card 2: status + table -----------------------------------
        var status = NewCard(new Padding(10, 0, 0, 0));
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = Color.Transparent };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        void RRow(Control c, int top = 0) { c.Margin = new Padding(0, top, 0, 0); c.Dock = DockStyle.Top; right.Controls.Add(c); }

        RRow(new KryptonLabel { Text = "Last run", LabelStyle = LabelStyle.BoldPanel });
        RRow(new KryptonLabel { Text = "Nightly VM backup, 214 GB of 380 GB", LabelStyle = LabelStyle.Custom1 });
        var bar = new PillProgress { Value = 56 };
        _bars.Add(bar);
        RRow(bar, 14);

        var gridView = new KryptonDataGridView
        {
            ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Height = 220
        };
        gridView.Columns.Add("job", "Job");
        gridView.Columns.Add("when", "Finished");
        gridView.Columns.Add("size", "Size");
        gridView.Columns.Add("result", "Result");
        gridView.Rows.Add("web-01", "Today 02:14", "38 GB", "Success");
        gridView.Rows.Add("db-prod", "Today 01:40", "112 GB", "Success");
        gridView.Rows.Add("files-hq", "Yesterday", "64 GB", "Warning");
        gridView.Rows.Add("mail", "Yesterday", "21 GB", "Success");
        gridView.Rows.Add("test-vm", "Mon", "9 GB", "Failed");
        RRow(gridView, 18);

        var tabs = new KryptonTabControl_Placeholder();
        right.Controls.Add(tabs.Host);
        status.Controls.Add(right);
        grid.Controls.Add(status, 1, 1);
    }

    CardPanel NewCard(Padding margin)
    {
        var c = new CardPanel { Dock = DockStyle.Fill, Margin = margin };
        _cards.Add(c);
        return c;
    }

    void ApplyTheme(Tokens t)
    {
        _t = t;
        var old = _palette;
        _palette = ModernTheme.Build(t);
        _manager.GlobalPaletteMode = PaletteMode.Custom;
        _manager.GlobalCustomPalette = _palette;
        old?.Dispose();

        BackColor = t.Canvas;
        ThemeApplier.Apply(this, t);
        foreach (var c in _cards) { c.Tokens = t; c.Invalidate(); }
        foreach (var b in _bars) { b.Tokens = t; b.Invalidate(); }
    }
}

/// <summary>Small helper so the right column has a bottom spacer.</summary>
sealed class KryptonTabControl_Placeholder
{
    public Control Host { get; } = new Panel { Height = 1, Dock = DockStyle.Top, BackColor = Color.Transparent };
}
