using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Fungusware.WinFormsThemes
{
    /// <summary>
    /// Switches the whole app to the modern look. Call <see cref="Install"/> once, before the first form is created.
    /// <para>
    /// The Krypton palette themes Krypton controls. Plain WinForms controls (panels, list views, tool strips, group boxes,
    /// buttons and so on) cannot be reached by a palette, so every form that opens is walked once and those controls are
    /// restyled in place. Controls added to a form later are picked up as well. Nothing is replaced or recreated, so
    /// designer fields and event handlers keep working.
    /// </para>
    /// </summary>
    public static class ThemeApplier
    {
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

        // Native scroll bars follow the Windows theme of the control, so dark themes ask for the dark one.
        public static void ScrollBarTheme(Control c, Tokens t, string darkClass = "DarkMode_Explorer")
        {
            void Set() { try { bool d = ModernTheme.IsDark(t); SetWindowTheme(c.Handle, d ? darkClass : null, null); try { AllowDarkModeForWindow(c.Handle, d); } catch { } SendMessage(c.Handle, 0x031A, IntPtr.Zero, IntPtr.Zero); } catch { } }
            if (c.IsHandleCreated) Set();
            else if (!ScrollHooked.TryGetValue(c, out _)) { ScrollHooked.Add(c, new object()); c.HandleCreated += (_, _) => Set(); }
        }
        static readonly ConditionalWeakTable<Control, object> ScrollHooked = new();

        // Undocumented uxtheme entry points (Windows 10 1903+) that make native scroll bars, context menus and
        // list views follow dark mode. Failing is harmless: those parts just stay light.
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", EntryPoint = "#135")] static extern int SetPreferredAppMode(int mode);
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", EntryPoint = "#104")] static extern void RefreshImmersiveColorPolicyState();

        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", EntryPoint = "#133")] static extern bool AllowDarkModeForWindow(IntPtr hwnd, bool allow);

        static void AppDarkMode(Tokens t)
        {
            try { SetPreferredAppMode(ModernTheme.IsDark(t) ? 2 /*ForceDark*/ : 3 /*ForceLight*/); RefreshImmersiveColorPolicyState(); }
            catch { }
        }


        static readonly ConditionalWeakTable<Control, object> Seen = new();
        static readonly ConditionalWeakTable<Form, object> ThemedForms = new();
        static bool _installed;

        public static Tokens Current { get; private set; } = ModernTheme.FluentLight;

        /// <summary>Raised after the theme has been switched at run time.</summary>
        public static event EventHandler ThemeChanged;

        // ---- Hooks for the host application ---------------------------------
        // The engine knows nothing about the app that uses it. The app plugs in where the chosen theme is stored,
        // where problems are logged, and how to style its own custom controls.

        /// <summary>Returns the saved theme name (or null/empty for the default). Used by <see cref="UseSavedTheme"/>.</summary>
        public static Func<string> ReadSavedThemeName { get; set; }

        /// <summary>Stores the chosen theme name. Called whenever the theme is switched with save = true.</summary>
        public static Action<string> WriteSavedThemeName { get; set; }

        public static Action<string> LogWarning { get; set; }
        public static Action<string, Exception> LogError { get; set; }

        static readonly List<(Type Type, Action<Control, Tokens> Apply)> Stylers = new();

        /// <summary>
        /// Styles a control type the engine does not know about (a custom control, or a third-party one). A registered
        /// styler replaces the built-in styling for that control type, and runs again whenever the theme changes.
        /// </summary>
        public static void RegisterStyler<T>(Action<T, Tokens> style) where T : Control
            => Stylers.Add((typeof(T), (c, t) => style((T)c, t)));

        static readonly List<ThemeAdapter> Adapters = new();

        /// <summary>
        /// Plugs a control family (Krypton, a third-party suite) into the engine. The theme stays the master: the adapter is
        /// told when the theme is installed and switched, and styles the controls it owns. Registered before
        /// <see cref="Install"/> it is installed with the engine; registered later it is installed straight away.
        /// </summary>
        public static void RegisterAdapter(ThemeAdapter adapter)
        {
            if (adapter == null || Adapters.Contains(adapter)) return;
            Adapters.Add(adapter);
            if (_installed) adapter.Install(Current);
        }

        static readonly List<(Type Type, Action<Control> Refresh)> Refreshers = new();

        /// <summary>
        /// Runs after a run-time theme switch for every control of this type, once it has been restyled. For controls whose
        /// row or cell colours are chosen when the data is built (lists, grids), so they can rebuild them.
        /// </summary>
        public static void RegisterRefresh<T>(Action<T> refresh) where T : Control
            => Refreshers.Add((typeof(T), c => refresh((T)c)));

        /// <summary>
        /// Switches to <paramref name="t"/> and themes one control tree, for apps that manage the Krypton palette themselves
        /// (see <see cref="ModernTheme.Build"/>) instead of calling <see cref="Install"/>.
        /// </summary>
        public static void Apply(Control root, Tokens t)
        {
            Current = t;
            ToolStripManager.Renderer = new ModernRenderer(t);
            root.SuspendLayout();
            try { Walk(root, t, top: true); }
            finally { root.ResumeLayout(true); }
            root.Invalidate(true);
        }

        /// <summary>Applies the theme saved by the host. Call once the host's settings can be read.</summary>
        public static void UseSavedTheme()
        {
            try
            {
                string name = ReadSavedThemeName?.Invoke();
                var saved = ModernTheme.All.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (saved != null) SetTheme(saved, save: false);
            }
            catch (Exception ex) { LogWarning?.Invoke("Theme: could not read the saved theme: " + ex.Message); }
        }

        static void Save(Tokens t)
        {
            try { WriteSavedThemeName?.Invoke(t.Name); }
            catch (Exception ex) { LogWarning?.Invoke("Theme: could not save the chosen theme: " + ex.Message); }
        }

        /// <summary>Switches every open form, and every form opened later, to another theme.</summary>
        public static void SetTheme(Tokens t, bool save = true)
        {
            if (t == null) return;
            if (!_installed) { Install(t); if (save) Save(t); return; }
            if (ReferenceEquals(t, Current)) return;

            Current = t;
            AppDarkMode(t);
            foreach (var a in Adapters) a.ThemeChanged(t);
            ToolStripManager.Renderer = new ModernRenderer(t);
            foreach (Form f in Application.OpenForms.Cast<Form>().ToList()) { Apply(f); RefreshRows(f); }
            if (save) Save(t);
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        // Row colours (job status and so on) are chosen when a row is built, so rebuild them after a switch.
        static void RefreshRows(Control root)
        {
            foreach (Control c in root.Controls)
            {
                foreach (var (type, refresh) in Refreshers)
                {
                    if (!type.IsInstanceOfType(c)) continue;
                    try { refresh(c); }
                    catch { /* a control that cannot refresh keeps its old colours until it is next filled */ }
                }
                RefreshRows(c);
            }
        }

        /// <summary>Adds a "Theme" sub-menu (one checked entry per theme) to a menu item.</summary>
        public static void AddThemeMenu(ToolStripMenuItem parent)
        {
            var menu = new ToolStripMenuItem("Theme");
            foreach (var t in ModernTheme.All)
            {
                var item = new ToolStripMenuItem(t.Name) { Tag = t };
                item.Click += (_, _) => SetTheme(t);
                menu.DropDownItems.Add(item);
            }
            menu.DropDownOpening += (_, _) =>
            {
                foreach (ToolStripMenuItem i in menu.DropDownItems) i.Checked = ReferenceEquals(i.Tag, Current);
            };
            parent.DropDownItems.Add(new ToolStripSeparator());
            parent.DropDownItems.Add(menu);
        }

        /// <summary>Sets the global palette and starts theming every form as it opens.</summary>
        public static void Install(Tokens tokens = null)
        {
            if (_installed) return;
            _installed = true;
            Current = tokens ?? ModernTheme.FluentLight;

            AppDarkMode(Current);
            foreach (var a in Adapters) a.Install(Current);
            ToolStripManager.Renderer = new ModernRenderer(Current);

            // Forms are themed on the first idle after they appear. This covers modal dialogs too.
            Application.Idle += (_, _) => ThemeOpenForms();
        }

        static void ThemeOpenForms()
        {
            foreach (Form f in Application.OpenForms.Cast<Form>().ToList())
            {
                if (ThemedForms.TryGetValue(f, out _)) continue;
                ThemedForms.Add(f, new object());
                Apply(f);
            }
        }

        /// <summary>Themes a form (or any control tree) and keeps it themed as controls are added.</summary>
        public static void Apply(Control root)
        {
            var t = Current;
            root.SuspendLayout();
            try { Walk(root, t, top: true); }
            finally { root.ResumeLayout(true); }
            root.Invalidate(true);
        }

        static void Walk(Control c, Tokens t, bool top = false)
        {
            if (!Seen.TryGetValue(c, out _))
            {
                Seen.Add(c, new object());
                c.ControlAdded += (_, e) => Walk(e.Control, Current);
            }
            var owner = Adapters.FirstOrDefault(a => a.Owns(c));
            try { if (owner == null) Style(c, t, top); else owner.Style(c, t); }
            catch (Exception ex) { LogError?.Invoke("Theme: could not style " + c.GetType().Name, ex); }

            // An adapter decides whether the parts inside its own controls are walked (they may belong to its palette).
            if (owner != null && !owner.WalkInto(c)) return;
            foreach (Control child in c.Controls.Cast<Control>().ToList()) Walk(child, t);
        }

        // Inputs that fill a table row would touch each other; give them a little room.
        public static void Breathe(Control c)
        {
            if (c.Dock == DockStyle.Fill && c.Parent is TableLayoutPanel && !Hooked.TryGetValue(c, out _))
            {
                Hooked.Add(c, new object());
                c.Margin = new Padding(c.Margin.Left, 3, c.Margin.Right, 3);
            }
        }

        static void Style(Control c, Tokens t, bool top)
        {
            foreach (var (type, apply) in Stylers)
                if (type.IsInstanceOfType(c)) { apply(c, t); return; }

            switch (c)
            {
                case Form f:
                    f.BackColor = t.Canvas; f.ForeColor = t.Text;
                    TitleBar.Apply(f, t);
                    break;

                case SplitContainer sc:
                    sc.BackColor = t.Border;
                    sc.Panel1.BackColor = t.Canvas; sc.Panel2.BackColor = t.Canvas;
                    break;

                case ToolStrip ts:
                    ts.BackColor = ts is ToolStripDropDown ? t.Surface : ModernTheme.Chrome(t); ts.ForeColor = t.Text; ts.RenderMode = ToolStripRenderMode.ManagerRenderMode;
                    PadToolbarButtons(ts);
                    break;

                case TabControl tc: StyleTabs(tc, t); break;
                case TabPage tp: tp.UseVisualStyleBackColor = false; tp.BackColor = t.Surface; tp.ForeColor = t.Text; break;

                case ListView lv: StyleListView(lv, t); ScrollBarTheme(lv, t, "DarkMode_ItemsView"); break;
                case GroupBox gb: StyleGroup(gb, t); break;

                case RichTextBox rtb:
                    rtb.BackColor = t.Surface; rtb.ForeColor = t.Text; rtb.BorderStyle = BorderStyle.None; ScrollBarTheme(rtb, t); break;

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

                case TextBoxBase tb:
                    tb.BackColor = t.Surface; tb.ForeColor = t.Text; if (tb.Multiline) ScrollBarTheme(tb, t); break;

                case ComboBox cb:
                    cb.FlatStyle = FlatStyle.Flat; cb.BackColor = t.Surface; cb.ForeColor = t.Text; break;

                case NumericUpDown nud:
                    nud.BackColor = t.Surface; nud.ForeColor = t.Text; break;

                case ListBox or TreeView:
                    c.BackColor = t.Surface; c.ForeColor = t.Text; ScrollBarTheme(c, t); break;

                case CheckBox or RadioButton or Label:
                    c.ForeColor = t.Text; c.BackColor = Color.Transparent; break;

                case UserControl uc:
                    uc.BackColor = top ? t.Canvas : Color.Transparent; uc.ForeColor = t.Text; break;

                case ProgressBar pb:
                    SetWindowTheme(pb.Handle, "", ""); // drop the green visual style so our colours apply
                    pb.ForeColor = t.Accent; pb.BackColor = t.SurfaceAlt;
                    break;
                case SplitterPanel: break; // coloured by the SplitContainer case above
                case Panel or FlowLayoutPanel or TableLayoutPanel:
                    c.BackColor = Color.Transparent; c.ForeColor = t.Text;
                    break;
            }
        }

        // A little horizontal breathing room around toolbar buttons (not menu items or drop-down lists).
        const int ButtonPadX = 6;
        static readonly ConditionalWeakTable<ToolStripItem, object> PaddedItems = new();

        public static void PadToolbarButtons(ToolStrip ts)
        {
            if (ts is ToolStripDropDown or MenuStrip or StatusStrip) return;
            foreach (ToolStripItem item in ts.Items) PadItem(item);
            if (!Tagged(ts)) ts.ItemAdded += (_, e) => PadItem(e.Item);
        }

        static void PadItem(ToolStripItem item)
        {
            if (item is not (ToolStripButton or ToolStripDropDownButton or ToolStripSplitButton)) return;
            if (PaddedItems.TryGetValue(item, out _)) return;
            PaddedItems.Add(item, new object());
            var p = item.Padding;
            item.Padding = new Padding(p.Left + ButtonPadX, p.Top, p.Right + ButtonPadX, p.Bottom);
        }

        // ---- plain ListView: owner drawn flat header, accent-tinted selection
        static void StyleListView(ListView lv, Tokens t)
        {
            lv.BackColor = t.Surface; lv.ForeColor = t.Text; lv.BorderStyle = BorderStyle.None;
            if (Tagged(lv)) { lv.Invalidate(); return; }
            if (lv.OwnerDraw) return; // something else already draws this list
            lv.OwnerDraw = true;
            if (lv.View == View.Details && lv.Columns.Count > 0)
            {
                // Fixed designer column widths leave a dead strip and clip the last column ("T.."): let it take the rest.
                void FillLast() { if (lv.IsHandleCreated && lv.Columns.Count > 0) lv.Columns[lv.Columns.Count - 1].Width = -2; }
                lv.Resize += (_, _) => FillLast();
                lv.HandleCreated += (_, _) => lv.BeginInvoke((Action)FillLast);
                FillLast();
            }
            lv.DrawColumnHeader += (s, e) =>
            {
                var th = Current;
                using var back = new SolidBrush(th.Surface); e.Graphics.FillRectangle(back, e.Bounds);
                using var pen = new Pen(th.Border); e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                using var bold = new Font(lv.Font, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, bold, Rectangle.Inflate(e.Bounds, -6, 0), th.TextMuted,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            };
            lv.DrawItem += (s, e) => { if (lv.View != View.Details) DrawCell(lv, e.Graphics, e.Bounds, e.Item.Text, e.Item.Selected); };
            lv.DrawSubItem += (s, e) => DrawCell(lv, e.Graphics, e.Bounds, e.SubItem.Text, e.Item.Selected);
        }

        static void DrawCell(ListView lv, Graphics g, Rectangle r, string text, bool sel)
        {
            var th = Current;
            using var back = new SolidBrush(sel ? ModernTheme.Blend(th.Accent, th.Surface, 0.18f) : th.Surface);
            g.FillRectangle(back, r);
            TextRenderer.DrawText(g, text, lv.Font, Rectangle.Inflate(r, -6, 0), th.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        // ---- standard TabControl: owner drawn flat tabs with accent underline
        static void StyleTabs(TabControl tc, Tokens t)
        {
            tc.BackColor = t.Surface;
            foreach (TabPage p in tc.TabPages) { p.UseVisualStyleBackColor = false; p.BackColor = t.Surface; p.ForeColor = t.Text; }
            if (Tagged(tc)) { tc.Invalidate(); return; }
            if (tc.DrawMode != TabDrawMode.Normal || tc.Alignment != TabAlignment.Top) return;
            tc.DrawMode = TabDrawMode.OwnerDrawFixed;
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

        // ---- GroupBox: repaint as a rounded hairline outline ----------------
        static void StyleGroup(GroupBox gb, Tokens t)
        {
            gb.ForeColor = t.Text; gb.BackColor = Color.Transparent;
            if (Tagged(gb)) { gb.Invalidate(); return; }
            gb.Paint += (s, e) =>
            {
                var th = Current; var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var parentBack = gb.Parent?.BackColor ?? th.Canvas;
                if (parentBack.A == 0) parentBack = th.Canvas;
                g.Clear(parentBack);
                int top = gb.Font.Height / 2;
                using var path = RoundedRect(new RectangleF(0.5f, top + 0.5f, gb.Width - 1, gb.Height - top - 1), Math.Min(th.CardRadius, 8));
                using var pen = new Pen(th.Border);
                g.DrawPath(pen, path);
                var size = TextRenderer.MeasureText(g, gb.Text, gb.Font);
                using var gap = new SolidBrush(parentBack);
                g.FillRectangle(gap, 8, 0, size.Width + 8, gb.Font.Height);
                TextRenderer.DrawText(g, gb.Text, gb.Font, new Point(12, 0), th.TextMuted);
            };
        }

        static readonly ConditionalWeakTable<Control, object> Hooked = new();
        public static bool Tagged(Control c)
        {
            if (Hooked.TryGetValue(c, out _)) return true;
            Hooked.Add(c, new object());
            return false;
        }

        public static GraphicsPath RoundedRect(RectangleF r, float rad)
        {
            float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    /// <summary>Menus, toolbars, status bars and context menus in the active theme.</summary>
    public sealed class ModernRenderer : ToolStripProfessionalRenderer
    {
        readonly Tokens _t;
        public ModernRenderer(Tokens t) : base(new ModernColors(t)) { _t = t; RoundedEdges = false; }

        Color Chrome => ModernTheme.Chrome(_t);
        Color Edge => ModernTheme.Blend(_t.BorderStrong, Chrome, 0.45f); // hairlines and separators on the bar

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            var ts = e.ToolStrip;
            using var pen = new Pen(ts is ToolStripDropDown ? _t.Border : Edge);
            if (ts is ToolStripDropDown)
                e.Graphics.DrawRectangle(pen, 0, 0, ts.Width - 1, ts.Height - 1);
            else if (ts is StatusStrip)
                e.Graphics.DrawLine(pen, 0, 0, ts.Width, 0);
            else
                e.Graphics.DrawLine(pen, 0, ts.Height - 1, ts.Width, ts.Height - 1); // hairline under every bar
        }

        // Soft rounded highlight for hover, pressed and checked, instead of Krypton's boxed gradients.
        void Highlight(Graphics g, ToolStripItem item, bool inDropDown)
        {
            bool pressed = item.Pressed;
            bool hot = item.Selected;
            bool on = item is ToolStripButton b && b.Checked;
            if (!pressed && !hot && !on) return;

            Color fill;
            if (inDropDown)
                fill = ModernTheme.Blend(_t.Accent, _t.Surface, pressed ? 0.26f : 0.12f);
            else if (pressed)
                fill = ModernTheme.Blend(_t.Accent, Chrome, 0.24f);
            else if (on)
                fill = hot ? ModernTheme.Blend(_t.Accent, _t.Surface, 0.10f) : _t.Surface; // active toggle is raised
            else
                fill = ModernTheme.Blend(_t.Surface, Chrome, 0.6f);                       // hover lifts toward white
            var r = inDropDown ? new Rectangle(4, 1, item.Width - 8, item.Height - 2) : new Rectangle(1, 2, item.Width - 3, item.Height - 4);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = ThemeApplier.RoundedRect(r, Math.Min(_t.Radius, 6));
            using var brush = new SolidBrush(fill);
            g.FillPath(brush, path);
            if (on)
            {
                // thin accent underline marks the active toggle
                using var pen = new Pen(_t.Accent, 2f);
                g.DrawLine(pen, r.Left + 8, r.Bottom - 1, r.Right - 8, r.Bottom - 1);
            }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e) => Highlight(e.Graphics, e.Item, false);
        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e) => Highlight(e.Graphics, e.Item, false);
        protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e) => Highlight(e.Graphics, e.Item, false);
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            => Highlight(e.Graphics, e.Item, e.Item.Owner is ToolStripDropDown);

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new Pen(e.Item.Owner is ToolStripDropDown ? _t.Border : Edge);
            var r = e.Item.ContentRectangle;
            if (e.Vertical)
            {
                int x = r.Left + r.Width / 2;
                e.Graphics.DrawLine(pen, x, r.Top + 4, x, r.Bottom - 4);
            }
            else
            {
                int y = r.Top + r.Height / 2;
                e.Graphics.DrawLine(pen, r.Left + 8, y, r.Right - 8, y);
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

        Color Chrome => ModernTheme.Chrome(_t);
        Color Hover => ModernTheme.Blend(_t.Accent, _t.Surface, 0.16f);
        Color Press => ModernTheme.Blend(_t.Accent, _t.Surface, 0.28f);

        public override Color ToolStripGradientBegin => Chrome;
        public override Color ToolStripGradientMiddle => Chrome;
        public override Color ToolStripGradientEnd => Chrome;
        public override Color MenuStripGradientBegin => Chrome;
        public override Color MenuStripGradientEnd => Chrome;
        public override Color ToolStripDropDownBackground => _t.Surface;
        public override Color ImageMarginGradientBegin => _t.Surface;
        public override Color ImageMarginGradientMiddle => _t.Surface;
        public override Color ImageMarginGradientEnd => _t.Surface;
        public override Color ToolStripBorder => _t.Border;
        public override Color StatusStripGradientBegin => Chrome;
        public override Color StatusStripGradientEnd => Chrome;
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

    /// <summary>Windows 11 lets an app colour its title bar. Older Windows ignores the request.</summary>
    static class TitleBar
    {
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20, DWMWA_BORDER_COLOR = 34, DWMWA_CAPTION_COLOR = 35, DWMWA_TEXT_COLOR = 36;

        static int Bgr(Color c) => c.R | (c.G << 8) | (c.B << 16);

        public static void Apply(Form f, Tokens t)
        {
            void Set()
            {
                try
                {
                    int caption = Bgr(t.Surface), text = Bgr(t.Text), border = Bgr(t.Border), dark = ModernTheme.IsDark(t) ? 1 : 0;
                    DwmSetWindowAttribute(f.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, 4);
                    DwmSetWindowAttribute(f.Handle, DWMWA_CAPTION_COLOR, ref caption, 4);
                    DwmSetWindowAttribute(f.Handle, DWMWA_TEXT_COLOR, ref text, 4);
                    DwmSetWindowAttribute(f.Handle, DWMWA_BORDER_COLOR, ref border, 4);
                }
                catch { /* not Windows 11 */ }
            }
            if (f.IsHandleCreated) Set(); else f.HandleCreated += (_, _) => Set();
        }
    }

}
