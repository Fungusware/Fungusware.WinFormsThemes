// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using Krypton.Navigator;
using Krypton.Toolkit;

namespace Fungusware.WinFormsThemes.Demo.VsClone;

/// <summary>Small "enter a value" dialog.</summary>
public sealed class PromptDialog : KryptonForm
{
    readonly KryptonTextBox _box = new();
    PromptDialog(string title, string label, string def)
    {
        Text = title; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
        ClientSize = new Size(440, 150); AutoScaleMode = AutoScaleMode.Dpi;
        var root = new KryptonPanel { Dock = DockStyle.Fill };
        Controls.Add(root);
        root.Controls.Add(new KryptonLabel { Text = label, Location = new Point(20, 18) });
        _box.SetBounds(20, 50, 400, 28); _box.Text = def; root.Controls.Add(_box);
        var ok = new KryptonButton { Text = "OK", ButtonStyle = ButtonStyle.Custom1, DialogResult = DialogResult.OK, Location = new Point(240, 98), Size = new Size(86, 30) };
        var cancel = new KryptonButton { Text = "Cancel", ButtonStyle = ButtonStyle.Standalone, DialogResult = DialogResult.Cancel, Location = new Point(334, 98), Size = new Size(86, 30) };
        root.Controls.Add(ok); root.Controls.Add(cancel);
        AcceptButton = ok; CancelButton = cancel;
        Shown += (_, _) => { ThemeApplier.Apply(this, ThemeApplier.Current); _box.Focus(); _box.SelectAll(); };
    }
    public static string Ask(IWin32Window owner, string title, string label, string def)
    {
        using var d = new PromptDialog(title, label, def);
        return d.ShowDialog(owner) == DialogResult.OK ? d._box.Text : null;
    }
}

/// <summary>Tools > Options: Krypton navigator tabs with a few real settings.</summary>
public sealed class OptionsDialog : KryptonForm
{
    public OptionsDialog(StudioSettings cfg, Tokens t)
    {
        Text = "Options"; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
        ClientSize = new Size(640, 420); AutoScaleMode = AutoScaleMode.Dpi;
        var root = new KryptonPanel { Dock = DockStyle.Fill };
        Controls.Add(root);

        var nav = new KryptonNavigator { NavigatorMode = NavigatorMode.BarTabGroup, Location = new Point(14, 14), Size = new Size(612, 340) };
        KryptonPage Page(string n) { var p = new KryptonPage { Text = n, TextTitle = n, TextDescription = n }; nav.Pages.Add(p); return p; }

        var env = Page("Environment");
        env.Controls.Add(new KryptonLabel { Text = "Colour theme", LabelStyle = LabelStyle.BoldPanel, Location = new Point(20, 20) });
        var theme = new KryptonComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(20, 52), Width = 260 };
        foreach (var th in ModernTheme.All) theme.Items.Add(th.Name);
        theme.SelectedItem = cfg.Theme; if (theme.SelectedIndex < 0) theme.SelectedIndex = 1;
        env.Controls.Add(theme);
        env.Controls.Add(new KryptonLabel { Text = "Every theme is just twelve colours; Bezier mimics the DevExpress skin.", LabelStyle = LabelStyle.Custom1, Location = new Point(20, 90) });

        var ed = Page("Text Editor");
        ed.Controls.Add(new KryptonLabel { Text = "Font size", LabelStyle = LabelStyle.BoldPanel, Location = new Point(20, 20) });
        var size = new KryptonNumericUpDown { Location = new Point(20, 52), Width = 100, Minimum = 7, Maximum = 32, DecimalPlaces = 1, Increment = 0.5m, Value = (decimal)cfg.FontSize };
        ed.Controls.Add(size);
        var mini = new KryptonCheckBox { Text = "Show minimap", Checked = cfg.Minimap, Location = new Point(20, 100) };
        var nums = new KryptonCheckBox { Text = "Show line numbers", Checked = cfg.LineNumbers, Location = new Point(20, 130) };
        ed.Controls.Add(mini); ed.Controls.Add(nums);

        var bld = Page("Projects and Solutions");
        bld.Controls.Add(new KryptonLabel { Text = "Builds run `dotnet build` into a temp folder, so the project you are editing can be the one that is running.", LabelStyle = LabelStyle.Custom1, Location = new Point(20, 20) });
        root.Controls.Add(nav);

        var ok = new KryptonButton { Text = "OK", ButtonStyle = ButtonStyle.Custom1, DialogResult = DialogResult.OK, Location = new Point(446, 372), Size = new Size(86, 30) };
        var cancel = new KryptonButton { Text = "Cancel", ButtonStyle = ButtonStyle.Standalone, DialogResult = DialogResult.Cancel, Location = new Point(540, 372), Size = new Size(86, 30) };
        root.Controls.Add(ok); root.Controls.Add(cancel);
        AcceptButton = ok; CancelButton = cancel;
        ok.Click += (_, _) => { cfg.Theme = (string)theme.SelectedItem ?? cfg.Theme; cfg.FontSize = (float)size.Value; cfg.Minimap = mini.Checked; cfg.LineNumbers = nums.Checked; };
        Shown += (_, _) => ThemeApplier.Apply(this, ThemeApplier.Current);
    }
}

public sealed class AboutDialog : KryptonForm
{
    public AboutDialog(Tokens t)
    {
        Text = "About Krypton Studio"; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
        ClientSize = new Size(560, 340); AutoScaleMode = AutoScaleMode.Dpi;
        var root = new KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(28) };
        Controls.Add(root);
        var logo = new PictureBox { Image = Ico.AppIcon(t.Accent).ToBitmap(), SizeMode = PictureBoxSizeMode.Zoom, Location = new Point(28, 28), Size = new Size(56, 56), BackColor = Color.Transparent };
        root.Controls.Add(logo);
        root.Controls.Add(new KryptonLabel { Text = "Krypton Studio", LabelStyle = LabelStyle.TitlePanel, Location = new Point(96, 28) });
        root.Controls.Add(new KryptonLabel { Text = "A Visual Studio-style IDE, built as a showcase.", LabelStyle = LabelStyle.Custom1, Location = new Point(98, 64) });
        var ver = typeof(KryptonForm).Assembly.GetName().Version;
        string[] lines =
        {
            $"Krypton Toolkit {ver}",
            "Fungusware.WinFormsThemes",
            "",
            "Docking, workspace tabs, navigator, palettes,",
            "buttons, text boxes, combo boxes, dialogs, split containers:",
            "all Krypton. The code editor, minimap and file tree are",
            "small custom controls painted from the same colour tokens.",
        };
        for (int i = 0; i < lines.Length; i++) root.Controls.Add(new KryptonLabel { Text = lines[i], Location = new Point(28, 118 + i * 24) });
        var ok = new KryptonButton { Text = "OK", ButtonStyle = ButtonStyle.Custom1, DialogResult = DialogResult.OK, Location = new Point(446, 288), Size = new Size(86, 30) };
        root.Controls.Add(ok); AcceptButton = ok; CancelButton = ok;
        Shown += (_, _) => ThemeApplier.Apply(this, ThemeApplier.Current);
    }
}

/// <summary>
/// Themed replacement for KryptonMessageBox. The stock one is built outside ThemeApplier's reach, so under a custom palette its default
/// button keeps the base palette's light fill and its text comes out washed out. This one is an ordinary KryptonForm that ThemeApplier walks.
/// </summary>
public static class KMessage
{
    public static DialogResult Show(IWin32Window owner, string text, string caption, KryptonMessageBoxButtons buttons = KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon icon = KryptonMessageBoxIcon.None)
    {
        var t = ThemeApplier.Current;
        using var f = new KryptonForm { Text = caption, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, AutoScaleMode = AutoScaleMode.Dpi };
        var root = new KryptonPanel { Dock = DockStyle.Fill };
        f.Controls.Add(root);
        var (glyph, color) = icon switch
        {
            KryptonMessageBoxIcon.Error or KryptonMessageBoxIcon.Stop => (Ico.Error, Color.FromArgb(0xE5, 0x4B, 0x4B)),
            KryptonMessageBoxIcon.Warning => (Ico.Warning, Color.FromArgb(0xE0, 0xA8, 0x1E)),
            KryptonMessageBoxIcon.Question => (Ico.Help, t.Accent),
            KryptonMessageBoxIcon.Information => (Ico.Info, t.Accent),
            _ => ('\0', t.Accent)
        };
        int x = 24;
        if (glyph != '\0') { root.Controls.Add(new PictureBox { Image = Ico.Glyph(glyph, color, 36), SizeMode = PictureBoxSizeMode.CenterImage, Location = new Point(22, 24), Size = new Size(40, 40), BackColor = Color.Transparent }); x = 74; }
        var label = new KryptonWrapLabel { Text = text, Location = new Point(x, 26), MaximumSize = new Size(420, 0), AutoSize = true };
        root.Controls.Add(label);

        (string text, DialogResult r)[] defs = buttons switch
        {
            KryptonMessageBoxButtons.YesNoCancel => new[] { ("Yes", DialogResult.Yes), ("No", DialogResult.No), ("Cancel", DialogResult.Cancel) },
            KryptonMessageBoxButtons.YesNo => new[] { ("Yes", DialogResult.Yes), ("No", DialogResult.No) },
            KryptonMessageBoxButtons.OKCancel => new[] { ("OK", DialogResult.OK), ("Cancel", DialogResult.Cancel) },
            _ => new[] { ("OK", DialogResult.OK) }
        };
        int bw = 92, gap = 8, bh = 30;
        int width = Math.Max(x + 440, 40 + defs.Length * (bw + gap));
        f.ClientSize = new Size(width, 0);
        int textBottom = Math.Max(label.Bottom, 64) + 28;
        f.ClientSize = new Size(width, textBottom + bh + 22);
        int bx = width - 22 - defs.Length * (bw + gap) + gap;
        KryptonButton first = null;
        foreach (var (txt, r) in defs)
        {
            var b = new KryptonButton { Text = txt, DialogResult = r, Location = new Point(bx, textBottom), Size = new Size(bw, bh), ButtonStyle = first == null ? ButtonStyle.Custom1 : ButtonStyle.Standalone };
            first ??= b; root.Controls.Add(b); bx += bw + gap;
        }
        f.AcceptButton = first;
        f.CancelButton = defs.Any(d => d.r == DialogResult.Cancel) ? root.Controls.OfType<KryptonButton>().First(b => b.DialogResult == DialogResult.Cancel)
                       : defs.Any(d => d.r == DialogResult.No) ? root.Controls.OfType<KryptonButton>().First(b => b.DialogResult == DialogResult.No) : first;
        f.Shown += (_, _) => ThemeApplier.Apply(f, t);
        return f.ShowDialog(owner);
    }
}
