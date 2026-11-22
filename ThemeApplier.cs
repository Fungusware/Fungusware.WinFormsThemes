// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using Krypton.Toolkit;
using System.Drawing.Drawing2D;

namespace KryptonModern;

/// <summary>
/// Themes the plain WinForms controls that a Krypton palette cannot reach.
/// Call <see cref="Apply"/> on the form after switching palette; it walks the control tree.
/// Controls that cannot be recoloured (ProgressBar, DateTimePicker) are swapped for themed ones.
/// </summary>
public static class ThemeApplier
{
    public static Tokens Current { get; private set; } = ModernTheme.Light;

    public static void Apply(Control root, Tokens t)
    {
        Current = t;
        ToolStripManager.Renderer = new ModernRenderer(t);
        try { Walk(root, t, top: true); } catch (Exception ex) { File.WriteAllText(Path.Combine(Path.GetTempPath(), "km-error.txt"), ex.ToString()); }
        root.Invalidate(true);
    }

    static void Walk(Control c, Tokens t, bool top = false)
    {
        // Krypton controls take the palette; only recurse into them.
        bool krypton = c.GetType().Namespace == "Krypton.Toolkit";
        if (!krypton) { Style(c, t, top); ScrollTheme.Apply(c, t); }
        else StyleKrypton(c, t);

        bool recurse = !krypton || c is KryptonPanel or KryptonForm or KryptonSplitContainer or KryptonSplitterPanel or KryptonGroupBox or KryptonGroup;
        if (recurse)
        foreach (Control child in c.Controls.Cast<Control>().ToList())
            Walk(child, t);
    }

    static void StyleKrypton(Control c, Tokens t)
    {
        switch (c)
        {
            case KryptonLinkLabel link:
                foreach (var o in new[] { link.OverrideNotVisited, link.OverrideVisited, link.OverrideFocus, link.OverridePressed })
                    o.ShortText.Color1 = t.Accent;
                break;
            case KryptonButton b:
                // Krypton's focus/default overrides come from the base palette and ignore ours; align them per button style.
                var (back, text) = b.ButtonStyle switch
                {
                    ButtonStyle.Custom1 => (t.Accent, t.OnAccent),
                    ButtonStyle.Custom2 => (t.SurfaceAlt, t.Text),
                    _ => (t.Surface, t.Text)
                };
                var edge = b.ButtonStyle == ButtonStyle.Custom1 ? t.Accent : t.BorderStrong;
                foreach (var o in new[] { b.OverrideFocus, b.OverrideDefault })
                {
                    o.Back.Color1 = back; o.Back.Color2 = back; o.Back.ColorStyle = PaletteColorStyle.Solid;
                    o.Content.ShortText.Color1 = text;
                    o.Border.Color1 = edge; o.Border.Color2 = edge;
                }
                break;
            case KryptonComboBox cmb:
                cmb.StateCommon.DropBack.Color1 = t.Surface; cmb.StateCommon.DropBack.Color2 = t.Surface;
                cmb.StateCommon.DropBack.ColorStyle = PaletteColorStyle.Solid;
                break;
            case KryptonListBox or KryptonDataGridView or KryptonRichTextBox or KryptonTextBox:
                ScrollTheme.ApplyDeep(c, t);
                break;
            case KryptonHeader h:
                h.Values.Image = null; // drop the default Krypton logo
                break;
        }
    }

    static void Style(Control c, Tokens t, bool top)
    {
        switch (c)
        {
            case Form f:
                f.BackColor = t.Canvas; f.ForeColor = t.Text; break;

            case SplitContainer sc:
                sc.BackColor = t.Border;
                sc.Panel1.BackColor = t.Canvas; sc.Panel2.BackColor = t.Canvas;
                break;

            case ToolStrip ts:
                ts.BackColor = t.Surface; ts.ForeColor = t.Text; ts.RenderMode = ToolStripRenderMode.ManagerRenderMode;
                break;

            case ProgressBar pb:
                ReplaceProgress(pb, t); return;

            case DateTimePicker dt:
                ReplaceDatePicker(dt); return;

            case ListView lv: StyleListView(lv, t); break;
            case TabControl tc: if (tc is not ModernTabControl) { SwapTabs(tc); return; } tc.Invalidate(); break;
            case TabPage tp: tp.UseVisualStyleBackColor = false; tp.BackColor = t.Surface; tp.ForeColor = t.Text; break;
            case GroupBox gb: StyleGroup(gb, t); break;

            case RichTextBox rtb:
                rtb.BackColor = t.Surface; rtb.ForeColor = t.Text; rtb.BorderStyle = BorderStyle.None; break;

            case PropertyGrid pg:
                pg.BackColor = t.Surface; pg.ViewBackColor = t.Surface; pg.ViewForeColor = t.Text;
                pg.LineColor = t.Border; pg.CategoryForeColor = t.TextMuted; pg.CategorySplitterColor = t.Border;
                pg.HelpBackColor = t.SurfaceAlt; pg.HelpForeColor = t.Text; pg.HelpBorderColor = t.Border;
                pg.CommandsBackColor = t.Surface; pg.CommandsForeColor = t.Text; pg.CommandsBorderColor = t.Border;
                pg.DisabledItemForeColor = t.Disabled; pg.SelectedItemWithFocusBackColor = t.Accent;
                pg.SelectedItemWithFocusForeColor = t.OnAccent;
                break;

            case Button b:
                b.UseVisualStyleBackColor = false; b.FlatStyle = FlatStyle.Flat;
                b.BackColor = t.Surface; b.ForeColor = t.Text;
                b.FlatAppearance.BorderColor = t.BorderStrong;
                b.FlatAppearance.MouseOverBackColor = t.SurfaceAlt;
                b.FlatAppearance.MouseDownBackColor = t.Border;
                break;

            case TextBoxBase tb when c is not RichTextBox:
                tb.BackColor = t.Surface; tb.ForeColor = t.Text; tb.BorderStyle = BorderStyle.FixedSingle; break;

            case ComboBox cb:
                cb.FlatStyle = FlatStyle.Flat; cb.BackColor = t.Surface; cb.ForeColor = t.Text; break;

            case NumericUpDown nud:
                nud.BackColor = t.Surface; nud.ForeColor = t.Text; break;

            case ListBox or TreeView:
                c.BackColor = t.Surface; c.ForeColor = t.Text; break;

            case CheckBox or RadioButton or Label:
                c.ForeColor = t.Text; c.BackColor = Color.Transparent; break;

            case UserControl uc:
                uc.BackColor = top ? t.Canvas : Color.Transparent; uc.ForeColor = t.Text; break;

            case Panel or FlowLayoutPanel or TableLayoutPanel:
                if (c is not CardPanel) { c.BackColor = Color.Transparent; c.ForeColor = t.Text; }
                break;
        }
    }

    // ---- ProgressBar -> PillProgress ------------------------------------
    static void ReplaceProgress(ProgressBar pb, Tokens t)
    {
        var parent = pb.Parent; if (parent == null) return;
        int pct = pb.Maximum > pb.Minimum ? (pb.Value - pb.Minimum) * 100 / (pb.Maximum - pb.Minimum) : 0;
        var pill = new PillProgress { Tokens = t, Value = pct, Dock = pb.Dock, Anchor = pb.Anchor, Margin = pb.Margin,
                                      Location = pb.Location, Width = pb.Width, Name = pb.Name };
        int index = parent.Controls.GetChildIndex(pb);
        parent.Controls.Remove(pb);
        parent.Controls.Add(pill);
        parent.Controls.SetChildIndex(pill, index);
    }

    // ---- DateTimePicker -> KryptonDateTimePicker ------------------------
    static void ReplaceDatePicker(DateTimePicker dt)
    {
        var parent = dt.Parent; if (parent == null) return;
        var k = new KryptonDateTimePicker { Value = dt.Value, Dock = dt.Dock, Anchor = dt.Anchor, Margin = dt.Margin,
                                            Location = dt.Location, Width = dt.Width, Name = dt.Name };
        int index = parent.Controls.GetChildIndex(dt);
        parent.Controls.Remove(dt);
        parent.Controls.Add(k);
        parent.Controls.SetChildIndex(k, index);
    }

    // ---- ListView (owner drawn: flat header, accent-tinted selection) ---
    static void StyleListView(ListView lv, Tokens t)
    {
        lv.BackColor = t.Surface; lv.ForeColor = t.Text; lv.BorderStyle = BorderStyle.None;
        if (lv.Tag as string == "themed") { lv.Invalidate(); return; }
        lv.Tag = "themed";
        lv.OwnerDraw = true;
        void Fill() { if (lv.Columns.Count > 0 && lv.View == View.Details) lv.Columns[lv.Columns.Count - 1].Width = -2; }
        lv.SizeChanged += (_, _) => Fill(); Fill();
        lv.DrawColumnHeader += (s, e) =>
        {
            var th = Current;
            using var back = new SolidBrush(th.Surface); e.Graphics.FillRectangle(back, e.Bounds);
            using var pen = new Pen(th.Border); e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, new Font(lv.Font, FontStyle.Bold),
                Rectangle.Inflate(e.Bounds, -6, 0), th.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
        lv.DrawItem += (s, e) => { if (lv.View != View.Details) DrawCell(lv, e.Graphics, e.Bounds, e.Item.Text, e.Item.Selected); };
        lv.DrawSubItem += (s, e) =>
        {
            var th = Current;
            bool sel = e.Item.Selected;
            using var back = new SolidBrush(sel ? ModernTheme.Blend(th.Accent, th.Surface, 0.18f) : th.Surface);
            e.Graphics.FillRectangle(back, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, lv.Font, Rectangle.Inflate(e.Bounds, -6, 0), th.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
    }

    static void DrawCell(ListView lv, Graphics g, Rectangle r, string text, bool sel)
    {
        var th = Current;
        using var back = new SolidBrush(sel ? ModernTheme.Blend(th.Accent, th.Surface, 0.18f) : th.Surface);
        g.FillRectangle(back, r);
        TextRenderer.DrawText(g, text, lv.Font, Rectangle.Inflate(r, -6, 0), th.Text, TextFormatFlags.VerticalCenter);
    }


    static void SwapTabs(TabControl old)
    {
        var parent = old.Parent; if (parent == null) return;
        var n = new ModernTabControl { Dock = old.Dock, Anchor = old.Anchor, Margin = old.Margin, Name = old.Name,
                                       Font = old.Font, Location = old.Location, Size = old.Size, Tag = old.Tag };
        int index = parent.Controls.GetChildIndex(old);
        foreach (TabPage p in old.TabPages.Cast<TabPage>().ToList()) { old.TabPages.Remove(p); n.TabPages.Add(p); }
        parent.Controls.Remove(old);
        parent.Controls.Add(n);
        parent.Controls.SetChildIndex(n, index);
        Walk(n, Current);
    }
    // ---- TabControl (owner drawn: flat tabs with accent underline) ------
    static void StyleTabs(TabControl tc, Tokens t)
    {
        tc.BackColor = t.Surface;
        foreach (TabPage p in tc.TabPages) { p.UseVisualStyleBackColor = false; p.BackColor = t.Surface; p.ForeColor = t.Text; }
        if (tc.Tag as string == "themed") { tc.Invalidate(); return; }
        tc.Tag = "themed";
        tc.DrawMode = TabDrawMode.OwnerDrawFixed;
        tc.Appearance = TabAppearance.Normal;
        tc.ItemSize = new Size(tc.ItemSize.Width, 30);
        tc.DrawItem += (s, e) =>
        {
            var th = Current; var g = e.Graphics;
            bool sel = e.Index == tc.SelectedIndex;
            using (var b = new SolidBrush(th.Surface)) g.FillRectangle(b, e.Bounds);
            TextRenderer.DrawText(g, tc.TabPages[e.Index].Text, tc.Font, e.Bounds, sel ? th.Text : th.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            if (sel) { using var pen = new Pen(th.Accent, 3); g.DrawLine(pen, e.Bounds.Left + 8, e.Bounds.Bottom - 2, e.Bounds.Right - 8, e.Bounds.Bottom - 2); }
        };
    }

    // ---- GroupBox: repaint as a rounded hairline card -------------------
    static void StyleGroup(GroupBox gb, Tokens t)
    {
        gb.ForeColor = t.Text; gb.BackColor = Color.Transparent;
        if (gb.Tag as string == "themed") { gb.Invalidate(); return; }
        gb.Tag = "themed";
        gb.Paint += (s, e) =>
        {
            var th = Current; var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var parentBack = gb.Parent?.BackColor ?? th.Canvas;
            if (parentBack.A == 0) parentBack = th.Canvas;
            g.Clear(parentBack);
            int top = gb.Font.Height / 2;
            using var path = CardPanel.Round(new RectangleF(0.5f, top + 0.5f, gb.Width - 1, gb.Height - top - 1), Math.Min(th.CardRadius, 8));
            using var pen = new Pen(th.Border);
            g.DrawPath(pen, path);
            var size = TextRenderer.MeasureText(g, gb.Text, gb.Font);
            using var gap = new SolidBrush(parentBack);
            g.FillRectangle(gap, 8, 0, size.Width + 8, gb.Font.Height);
            TextRenderer.DrawText(g, gb.Text, gb.Font, new Point(12, 0), th.TextMuted);
        };
    }
}

/// <summary>Menus, toolbars, status bars and context menus in the active theme.</summary>
public sealed class ModernRenderer : ToolStripProfessionalRenderer
{
    readonly Tokens _t;
    public ModernRenderer(Tokens t) : base(new ModernColors(t)) { _t = t; RoundedEdges = false; }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        if (e.ToolStrip is ToolStripDropDown)
        {
            using var pen = new Pen(_t.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? _t.Text : _t.Disabled;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) { e.ArrowColor = _t.TextMuted; base.OnRenderArrow(e); }
}

sealed class ModernColors : ProfessionalColorTable
{
    readonly Tokens _t;
    public ModernColors(Tokens t) { _t = t; UseSystemColors = false; }

    Color Hover => ModernTheme.Blend(_t.Accent, _t.Surface, 0.16f);
    Color Press => ModernTheme.Blend(_t.Accent, _t.Surface, 0.28f);

    public override Color ToolStripGradientBegin => _t.Surface;
    public override Color ToolStripGradientMiddle => _t.Surface;
    public override Color ToolStripGradientEnd => _t.Surface;
    public override Color MenuStripGradientBegin => _t.Surface;
    public override Color MenuStripGradientEnd => _t.Surface;
    public override Color ToolStripDropDownBackground => _t.Surface;
    public override Color ImageMarginGradientBegin => _t.Surface;
    public override Color ImageMarginGradientMiddle => _t.Surface;
    public override Color ImageMarginGradientEnd => _t.Surface;
    public override Color ToolStripBorder => _t.Border;
    public override Color StatusStripGradientBegin => _t.Surface;
    public override Color StatusStripGradientEnd => _t.Surface;
    public override Color SeparatorDark => _t.Border;
    public override Color SeparatorLight => _t.Surface;
    public override Color GripDark => _t.BorderStrong;
    public override Color GripLight => _t.Surface;
    public override Color MenuBorder => _t.Border;
    public override Color MenuItemBorder => Hover;
    public override Color MenuItemSelected => Hover;
    public override Color MenuItemSelectedGradientBegin => Hover;
    public override Color MenuItemSelectedGradientEnd => Hover;
    public override Color MenuItemPressedGradientBegin => Press;
    public override Color MenuItemPressedGradientMiddle => Press;
    public override Color MenuItemPressedGradientEnd => Press;
    public override Color ButtonSelectedBorder => Hover;
    public override Color ButtonSelectedGradientBegin => Hover;
    public override Color ButtonSelectedGradientMiddle => Hover;
    public override Color ButtonSelectedGradientEnd => Hover;
    public override Color ButtonSelectedHighlight => Hover;
    public override Color ButtonPressedBorder => Press;
    public override Color ButtonPressedGradientBegin => Press;
    public override Color ButtonPressedGradientMiddle => Press;
    public override Color ButtonPressedGradientEnd => Press;
    public override Color ButtonPressedHighlight => Press;
    public override Color ButtonCheckedGradientBegin => Press;
    public override Color ButtonCheckedGradientMiddle => Press;
    public override Color ButtonCheckedGradientEnd => Press;
    public override Color CheckBackground => Press;
    public override Color CheckSelectedBackground => Press;
    public override Color CheckPressedBackground => Press;
}

/// <summary>TabControl that paints its own tab strip so no native light chrome shows.</summary>
public class ModernTabControl : TabControl
{
    public ModernTabControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        ItemSize = new Size(100, 32);
        SizeMode = TabSizeMode.Fixed;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var t = ThemeApplier.Current; var g = e.Graphics;
        g.Clear(t.Surface);
        int bottom = 0;
        for (int i = 0; i < TabCount; i++)
        {
            var r = GetTabRect(i); bottom = Math.Max(bottom, r.Bottom);
            bool sel = i == SelectedIndex;
            TextRenderer.DrawText(g, TabPages[i].Text, Font, r, sel ? t.Text : t.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (sel) { using var pen = new Pen(t.Accent, 3); g.DrawLine(pen, r.Left + 10, r.Bottom + 1, r.Right - 10, r.Bottom + 1); }
        }
        using var line = new Pen(t.Border);
        g.DrawLine(line, 0, bottom + 2, Width, bottom + 2);
    }
}

static class ScrollTheme
{
    [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

    public static void ApplyDeep(Control c, Tokens t) { Apply(c, t); foreach (Control k in c.Controls) ApplyDeep(k, t); }

    public static void Apply(Control c, Tokens t)
    {
        bool dark = (t.Canvas.R * 0.299 + t.Canvas.G * 0.587 + t.Canvas.B * 0.114) < 128;
        void Set() { try { SetWindowTheme(c.Handle, c is ComboBox ? (dark ? "DarkMode_CFD" : "CFD") : (dark ? "DarkMode_Explorer" : "Explorer"), null); } catch { } }
        if (c.IsHandleCreated) Set(); else c.HandleCreated += (_, _) => Set();
    }
}
