// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using BrightIdeasSoftware;

namespace Fungusware.WinFormsThemes
{
    /// <summary>
    /// Carries the theme to ObjectListView. Call <see cref="Register"/> once at start-up: every ObjectListView is then styled,
    /// restyled on a theme switch, and has its rows rebuilt so row colours chosen in a formatter follow the new theme.
    /// </summary>
    public static class ObjectListViewTheme
    {
        static bool _registered;
        static readonly ConditionalWeakTable<Control, object> WidthHooked = new();

        public static void Register()
        {
            if (_registered) return;
            _registered = true;
            ThemeApplier.RegisterStyler<ObjectListView>((olv, t) =>
            {
                StyleObjectListView(olv, t);
                ThemeApplier.ScrollBarTheme(olv, t, "DarkMode_ItemsView");
            });
            ThemeApplier.RegisterRefresh<ObjectListView>(olv =>
            {
                var rows = olv.Objects?.Cast<object>().ToList();
                if (rows != null && rows.Count > 0) olv.RefreshObjects(rows);
            });
        }

        // ---- header, selection and empty-list colours ----------------
        public static void StyleList(ObjectListView olv) => StyleObjectListView(olv, ThemeApplier.Current);

        static void StyleObjectListView(ObjectListView olv, Tokens t)
        {
            bool recolour = olv.BackColor != t.Surface || olv.ForeColor != t.Text; // rows made earlier keep their old colours
            olv.BackColor = t.Surface; olv.ForeColor = t.Text; olv.BorderStyle = BorderStyle.None;
            olv.HeaderUsesThemes = false;
            var h = new HeaderFormatStyle();
            h.SetFont(new Font(olv.Font, FontStyle.Bold)); // kept for the life of the list
            h.SetBackColor(t.SurfaceAlt);
            h.SetForeColor(t.Text);
            h.Normal.FrameColor = t.Border; h.Hot.FrameColor = t.Border; h.Pressed.FrameColor = t.Border;
            h.Normal.FrameWidth = 1; h.Hot.FrameWidth = 1; h.Pressed.FrameWidth = 1;
            h.Hot.BackColor = ModernTheme.Blend(t.Accent, t.SurfaceAlt, 0.10f);
            h.Pressed.BackColor = ModernTheme.Blend(t.Accent, t.SurfaceAlt, 0.20f);
            olv.HeaderFormatStyle = h;
            int headerHeight = Math.Max(olv.HeaderMinimumHeight, (int)Math.Round(olv.Font.GetHeight() * 1.0f) + 14); // comfortable padding above and below the header text
            olv.HeaderMinimumHeight = headerHeight;
            if (olv.HeaderMaximumHeight >= 0 && olv.HeaderMaximumHeight < headerHeight) olv.HeaderMaximumHeight = headerHeight; // a cap set in the designer would otherwise squash the header
            var sel = ModernTheme.Blend(t.Accent, t.Surface, 0.18f);
            olv.SelectedBackColor = sel; olv.SelectedForeColor = t.Text;
            olv.UnfocusedSelectedBackColor = ModernTheme.Blend(t.Accent, t.Surface, 0.10f);
            olv.UnfocusedSelectedForeColor = t.Text;
            olv.AlternateRowBackColor = ModernTheme.Blend(t.Canvas, t.Surface, 0.45f);
            olv.GridLines = false;

            // The last column takes the free space so the header bar runs the full width.
            if (olv.AllColumns.Count > 0 && !olv.AllColumns.Any(c => c.FillsFreeSpace))
            {
                var last = olv.AllColumns.LastOrDefault(c => c.IsVisible);
                if (last != null) last.FillsFreeSpace = true;
                // OLV only re-spreads free space on a resize event, so nudge once after layout.
                if (olv.IsHandleCreated && !ThemeApplier.Tagged(olv)) olv.BeginInvoke(new Action(() => { int w = olv.Width; olv.Width = w + 1; olv.Width = w; }));
            }

            // Auto-fitting columns to the content of an empty list leaves header stubs ("A...", "T..."): keep each header readable.
            using (var hf = new Font(olv.Font, FontStyle.Bold))
                foreach (var c in olv.AllColumns)
                    if (c.FillsFreeSpace == false && !string.IsNullOrEmpty(c.Text))
                        c.MinimumWidth = Math.Max(c.MinimumWidth, TextRenderer.MeasureText(c.Text, hf).Width + 20);

            // A list built inside a hidden tab can be laid out while tiny, which squeezes the fixed columns to "A..." stubs.
            // Remember the designed widths and put them back the first time the list has a real size.
            if (!WidthHooked.TryGetValue(olv, out _) && olv.AllColumns.Any(c => c.FillsFreeSpace))
            {
                WidthHooked.Add(olv, new object());
                var designed = olv.AllColumns.Where(c => c.IsVisible && !c.FillsFreeSpace && c.Width > 0).ToDictionary(c => c, c => c.Width);
                bool restored = false;
                void Restore()
                {
                    if (restored || olv.ClientSize.Width < 300) return;
                    restored = true;
                    foreach (var kv in designed) if (kv.Key.Width < kv.Value) kv.Key.Width = kv.Value;
                }
                olv.SizeChanged += (_, _) => Restore();
                olv.VisibleChanged += (_, _) => { if (olv.Visible) olv.BeginInvoke((Action)Restore); };
            }

            if (olv.EmptyListMsgOverlay is TextOverlay msg)
            {
                msg.BackColor = t.SurfaceAlt; msg.BorderColor = t.Border; msg.TextColor = t.TextMuted;
                msg.Font = new Font("Segoe UI", 9.5f); msg.CornerRounding = 8f;
            }
            if (recolour && olv.GetItemCount() > 0) olv.RefreshObjects(olv.Objects.Cast<object>().ToList());
            olv.Invalidate();
        }
    }
}
