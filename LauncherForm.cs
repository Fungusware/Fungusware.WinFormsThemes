// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using Krypton.Toolkit;

namespace Fungusware.WinFormsThemes.Demo;

/// <summary>Start window for the demo: pick which test window to open. Closing a test window returns here.</summary>
public class LauncherForm : KryptonForm
{
    readonly KryptonManager _manager = new();
    readonly KryptonCustomPaletteBase _palette = KryptonTheme.Build(ModernTheme.Light);

    public LauncherForm()
    {
        Text = "Krypton Modern";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 420);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;

        _manager.GlobalPaletteMode = PaletteMode.Custom;
        _manager.GlobalCustomPalette = _palette;

        var root = new KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(28, 24, 28, 24) };
        Controls.Add(root);

        var stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent };
        root.Controls.Add(stack);
        stack.Controls.Add(new KryptonLabel { Text = "Krypton Modern", LabelStyle = LabelStyle.TitlePanel });
        stack.Controls.Add(new KryptonLabel { Text = "Choose a window to test. Each one has its own theme picker.", LabelStyle = LabelStyle.Custom1, Margin = new Padding(3, 0, 3, 14) });

        Add(stack, "Settings demo", "A realistic settings page: forms, table, progress.", () => new DemoForm());
        Add(stack, "Control check", "Every Krypton Toolkit control and plain WinForms controls.", () => new ControlsForm());
        Add(stack, "Suite gallery", "Ribbon, Navigator, Workspace and Docking.", () => new GalleryForm());
        Add(stack, "Krypton Studio", "A full Visual Studio-style IDE built with Krypton.", () => new Fungusware.WinFormsThemes.Demo.VsClone.VsCloneForm());
        Add(stack, "Lists", "ObjectListView with themed header, rows and empty message.", () => new ListForm());
        Shown += (_, _) => ThemeApplier.Apply(this, ModernTheme.Light); // controls exist only once the form is shown
    }

    void Add(Control parent, string title, string description, Func<Form> create)
    {
        var btn = new KryptonButton { Text = title, ButtonStyle = ButtonStyle.Custom1, Size = new Size(240, 38), Margin = new Padding(3, 8, 3, 0) };
        btn.Click += (_, _) =>
        {
            Hide();
            using var f = create();
            f.ShowDialog();
            // the test window changed the global palette; restore ours for the launcher
            _manager.GlobalCustomPalette = _palette;
            ThemeApplier.Apply(this, ModernTheme.Light);
            Show();
        };
        parent.Controls.Add(btn);
        parent.Controls.Add(new KryptonLabel { Text = description, LabelStyle = LabelStyle.Custom1, Margin = new Padding(3, 2, 3, 0) });
    }
}
