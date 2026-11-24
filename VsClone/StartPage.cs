// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using Krypton.Toolkit;

namespace Fungusware.WinFormsThemes.Demo.VsClone;

/// <summary>The "start window": recent projects, get-started cards and a one-click theme strip.</summary>
public sealed class StartPage : KryptonPanel
{
    readonly VsCloneForm _form;
    readonly List<CardPanel> _cards = new();

    public StartPage(VsCloneForm form, StudioSettings cfg)
    {
        _form = form;
        AutoScroll = true;
        Padding = new Padding(48, 36, 48, 36);

        var title = new KryptonLabel { Text = "Krypton Studio", LabelStyle = LabelStyle.TitlePanel, Location = new Point(48, 30) };
        var sub = new KryptonLabel { Text = "Everything you see here is Krypton Toolkit, wearing the Fungusware.WinFormsThemes theme.", LabelStyle = LabelStyle.Custom1, Location = new Point(50, 90) };
        Controls.Add(title); Controls.Add(sub);

        // ---- recent ---------------------------------------------------
        Controls.Add(new KryptonLabel { Text = "Open recent", LabelStyle = LabelStyle.BoldPanel, Location = new Point(48, 140) });
        int y = 174;
        var recent = cfg.Recent.Where(p => File.Exists(p) || Directory.Exists(p)).Take(6).ToList();
        if (recent.Count == 0) { Controls.Add(new KryptonLabel { Text = "Nothing yet. Open a solution to see it here.", LabelStyle = LabelStyle.Custom1, Location = new Point(50, y) }); }
        foreach (var p in recent)
        {
            var b = new KryptonButton { ButtonStyle = ButtonStyle.Custom2, Location = new Point(48, y), Size = new Size(430, 44), Text = Path.GetFileName(p.TrimEnd('\\', '/')) };
            b.Values.ExtraText = Path.GetDirectoryName(p.TrimEnd('\\', '/'));
            b.StateCommon.Content.ShortText.TextH = PaletteRelativeAlign.Near;
            b.StateCommon.Content.LongText.TextH = PaletteRelativeAlign.Near;
            var path = p; b.Click += (_, _) => _form.StartAction(path);
            Controls.Add(b); y += 48;
        }

        // ---- get started ----------------------------------------------
        Controls.Add(new KryptonLabel { Text = "Get started", LabelStyle = LabelStyle.BoldPanel, Location = new Point(540, 140) });
        (string t, string d, string a)[] tiles =
        {
            ("Open a project or solution", "Browse for a .sln, .slnx or .csproj", "solution"),
            ("Open a local folder", "Navigate and edit any folder of code", "folder"),
            ("Create a new file", "Add a class or text file to the project", "file"),
            ("Change the theme", "Cycle through all ten themes, Bezier included", "theme"),
            ("Customize the editor", "Font size, minimap and line numbers", "options"),
        };
        int ty = 174;
        foreach (var (t, d, a) in tiles)
        {
            var card = new CardPanel { Location = new Point(540, ty), Size = new Size(440, 70), Padding = new Padding(16, 10, 12, 8), Cursor = Cursors.Hand };
            card.Controls.Add(new KryptonLabel { Text = t, LabelStyle = LabelStyle.BoldPanel, Location = new Point(14, 8) });
            card.Controls.Add(new KryptonLabel { Text = d, LabelStyle = LabelStyle.Custom1, Location = new Point(16, 36) });
            string act = a;
            void Hook(Control c) { c.Click += (_, _) => _form.StartAction(act); c.Cursor = Cursors.Hand; foreach (Control k in c.Controls) Hook(k); }
            Hook(card);
            _cards.Add(card); Controls.Add(card); ty += 78;
        }

        // ---- theme strip ----------------------------------------------
        int sy = Math.Max(y, ty) + 24;
        Controls.Add(new KryptonLabel { Text = "Try a theme", LabelStyle = LabelStyle.BoldPanel, Location = new Point(48, sy) });
        int x = 48, row = sy + 34;
        foreach (var th in ModernTheme.All)
        {
            var b = new KryptonButton { Text = th.Name, ButtonStyle = ButtonStyle.Standalone, Location = new Point(x, row), Size = new Size(142, 32) };
            string name = th.Name; b.Click += (_, _) => _form.StartAction("theme:" + name);
            Controls.Add(b); x += 150;
            if (x > 48 + 150 * 6) { x = 48; row += 40; }
        }
    }

    public void ApplyTheme(Tokens t)
    {
        foreach (var c in _cards) { c.Tokens = t; c.Invalidate(); }
        BackColor = t.Canvas;
    }
}
