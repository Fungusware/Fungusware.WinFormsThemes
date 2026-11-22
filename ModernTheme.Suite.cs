// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using Krypton.Toolkit;

namespace KryptonModern;

/// <summary>Palette entries for the wider Krypton suite: Navigator / workspace / docking tabs, Ribbon, tool strips.</summary>
public static partial class ModernTheme
{
    static void BuildSuite(KryptonCustomPaletteBase p, Tokens t, Font font, Font semi)
    {
        var selBack = Blend(t.Accent, t.Surface, 0.18f);

        // ---- client areas: navigator pages, workspace cells, group boxes, tooltips ----
        foreach (dynamic cs in new dynamic[] { p.ControlStyles.ControlCommon, p.ControlStyles.ControlClient, p.ControlStyles.ControlAlternate,
                     p.ControlStyles.ControlGroupBox, p.ControlStyles.ControlCustom1, p.ControlStyles.ControlCustom2, p.ControlStyles.ControlCustom3 })
        {
            cs.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;
            cs.StateCommon.Back.Color1 = t.Canvas; cs.StateCommon.Back.Color2 = t.Canvas;
            cs.StateCommon.Border.Color1 = t.Border; cs.StateCommon.Border.Color2 = t.Border;
            cs.StateCommon.Border.Width = 1; cs.StateCommon.Border.Rounding = 0;
        }
        foreach (dynamic cs in new dynamic[] { p.ControlStyles.ControlToolTip, p.ControlStyles.ControlRibbon, p.ControlStyles.ControlRibbonAppMenu })
        {
            cs.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;
            cs.StateCommon.Back.Color1 = t.Surface; cs.StateCommon.Back.Color2 = t.Surface;
            cs.StateCommon.Border.Color1 = t.Border; cs.StateCommon.Border.Color2 = t.Border;
        }
        foreach (dynamic h in new dynamic[] { p.HeaderStyles.HeaderCommon, p.HeaderStyles.HeaderDockInactive, p.HeaderStyles.HeaderCustom1,
                     p.HeaderStyles.HeaderCustom2, p.HeaderStyles.HeaderCustom3 })
        {
            h.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;
            h.StateCommon.Back.Color1 = t.Surface; h.StateCommon.Back.Color2 = t.Surface;
            h.StateCommon.Border.Color1 = t.Border; h.StateCommon.Border.Color2 = t.Border;
            h.StateCommon.Border.DrawBorders = PaletteDrawBorders.Bottom;
            h.StateCommon.Border.Rounding = 0;
            h.StateCommon.Content.ShortText.Color1 = t.TextMuted; h.StateCommon.Content.ShortText.Font = font;
        }
        p.HeaderStyles.HeaderDockActive.StateCommon.Content.ShortText.Color1 = t.Text;
        foreach (dynamic sep in new dynamic[] { p.SeparatorStyles.SeparatorCommon, p.SeparatorStyles.SeparatorCustom1,
                     p.SeparatorStyles.SeparatorCustom2, p.SeparatorStyles.SeparatorCustom3 })
        {
            sep.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;
            sep.StateCommon.Back.Color1 = t.Border; sep.StateCommon.Back.Color2 = t.Border;
        }
        foreach (dynamic gr in new dynamic[] { p.GridStyles.GridList, p.GridStyles.GridSheet, p.GridStyles.GridCustom1, p.GridStyles.GridCustom2, p.GridStyles.GridCustom3 })
        {
            gr.StateCommon.Background.Color1 = t.Surface;
            gr.StateCommon.DataCell.Back.Color1 = t.Surface; gr.StateCommon.DataCell.Back.Color2 = t.Surface;
            gr.StateCommon.DataCell.Content.Color1 = t.Text;
            gr.StateCommon.HeaderColumn.Back.Color1 = t.Surface; gr.StateCommon.HeaderColumn.Back.Color2 = t.Surface;
            gr.StateCommon.HeaderColumn.Content.Color1 = t.TextMuted;
        }
        p.DragDrop.SolidBack = Blend(t.Accent, t.Canvas, 0.25f); p.DragDrop.SolidBorder = t.Accent;
        p.DragDrop.DropDockBack = t.Surface; p.DragDrop.DropDockBorder = t.Border;
        p.DragDrop.DropDockActive = t.Accent; p.DragDrop.DropDockInactive = t.BorderStrong;

        // ---- navigator / workspace / docking tab strips ------------------
        foreach (dynamic tab in new dynamic[] { p.TabStyles.TabCommon, p.TabStyles.TabHighProfile, p.TabStyles.TabStandardProfile,
                     p.TabStyles.TabLowProfile, p.TabStyles.TabDock, p.TabStyles.TabDockAutoHidden, p.TabStyles.TabOneNote,
                     p.TabStyles.TabCustom1, p.TabStyles.TabCustom2, p.TabStyles.TabCustom3 })
        {
            var c = tab.StateCommon;
            c.Back.ColorStyle = PaletteColorStyle.Solid;
            c.Border.Width = 1; c.Border.Rounding = t.Radius;
            c.Border.DrawBorders = PaletteDrawBorders.All;
            c.Border.GraphicsHint = PaletteGraphicsHint.AntiAlias;
            c.Content.ShortText.Font = font; c.Content.ShortText.Color1 = t.TextMuted;
            c.Content.Padding = new Padding(10, 4, 10, 4);
            Tab(tab.StateNormal, t.Canvas, t.Canvas, t.TextMuted);
            Tab(tab.StateTracking, t.SurfaceAlt, t.Border, t.Text);
            Tab(tab.StatePressed, t.SurfaceAlt, t.BorderStrong, t.Text);
            Tab(tab.StateSelected, t.Surface, t.Accent, t.Text);
            Tab(tab.StateDisabled, t.Canvas, t.Canvas, t.Disabled);
        }

        // ---- navigator bar buttons: check-button groups, stack, overflow, mini ----
        foreach (dynamic b in new dynamic[] { p.ButtonStyles.ButtonAlternate, p.ButtonStyles.ButtonCluster, p.ButtonStyles.ButtonGallery,
                     p.ButtonStyles.ButtonNavigatorStack, p.ButtonStyles.ButtonNavigatorOverflow, p.ButtonStyles.ButtonNavigatorMini,
                     p.ButtonStyles.ButtonBreadCrumb, p.ButtonStyles.ButtonCommand, p.ButtonStyles.ButtonButtonSpec, p.ButtonStyles.ButtonCommon })
        {
            Button(b, t.Surface, t.SurfaceAlt, t.Border, t.Border, t.Text, t, font);
            CheckedLook(b, t, selBack);
            b.StateCommon.Border.Color1 = t.Border; b.StateCommon.Border.Color2 = t.Border;
            b.OverrideFocus.Border.Color1 = t.Accent; b.OverrideFocus.Border.Color2 = t.Accent;
        }
        CheckedLook(p.ButtonStyles.ButtonStandalone, t, selBack);
        foreach (dynamic sb in new dynamic[] { p.ButtonStyles.ButtonNavigatorStack, p.ButtonStyles.ButtonNavigatorOverflow }) sb.StateCommon.Border.Rounding = 0;
        Button(p.ButtonStyles.ButtonLowProfile, t.Canvas, t.SurfaceAlt, t.Border, t.Canvas, t.Text, t, font);
        CheckedLook(p.ButtonStyles.ButtonLowProfile, t, selBack);

        // ---- docking splitters / dock headers already use Header + Separator styles ----

        // ---- ribbon ----------------------------------------------------
        var r = p.Ribbon;
        var g = r.RibbonGeneral;
        g.TabRowBackgroundSolidColor = t.Canvas;
        g.TabRowBackgroundGradientFirstColor = t.Canvas;
        g.TabRowBackgroundGradientRaftingDarkColor = t.Canvas;
        g.TabRowBackgroundGradientRaftingLightColor = t.Canvas;
        g.TabSeparatorColor = t.Border; g.TabSeparatorContextColor = t.Border;
        g.GroupSeparatorDark = t.Border; g.GroupSeparatorLight = t.Border;
        g.GroupDialogDark = t.TextMuted; g.GroupDialogLight = t.TextMuted;
        g.DropArrowDark = t.TextMuted; g.DropArrowLight = t.TextMuted;
        g.DisabledDark = t.Disabled; g.DisabledLight = t.Disabled;
        g.MinimizeBarDarkColor = t.TextMuted; g.MinimizeBarLightColor = t.TextMuted;
        g.QATButtonDarkColor = t.TextMuted; g.QATButtonLightColor = t.TextMuted;
        g.ContextTextColor = t.Text;
        g.TextFont = font; g.ContextTextFont = font;

        foreach (dynamic st in new dynamic[] { r.RibbonTab.StateCommon, r.RibbonTab.StateNormal, r.RibbonTab.StateTracking,
                     r.RibbonTab.StateContextTracking })
            RibbonDouble(st, t.Canvas, t.TextMuted);
        RibbonDouble(r.RibbonTab.StateTracking, t.SurfaceAlt, t.Text);
        RibbonDouble(r.RibbonTab.StateContextTracking, t.SurfaceAlt, t.Text);
        foreach (dynamic st in new dynamic[] { r.RibbonTab.StateCheckedNormal, r.RibbonTab.StateCheckedTracking,
                     r.RibbonTab.StateContextCheckedNormal, r.RibbonTab.StateContextCheckedTracking })
            RibbonDouble(st, t.Surface, t.Text);

        foreach (dynamic st in new dynamic[] { r.RibbonGroupArea.StateCommon, r.RibbonGroupArea.StateCheckedNormal,
                     r.RibbonGroupArea.StateContextCheckedNormal })
            RibbonBack(st, t.Surface);
        foreach (dynamic st in new dynamic[] { r.RibbonGroupArea.StateTracking, r.RibbonGroupArea.StateContextTracking,
                     r.RibbonGroupArea.StateContextPressed })
            RibbonBack(st, t.SurfaceAlt);

        foreach (dynamic st in new dynamic[] { r.RibbonGroupNormalBorder.StateCommon, r.RibbonGroupNormalBorder.StateNormal,
                     r.RibbonGroupNormalBorder.StateContextNormal, r.RibbonGroupCollapsedBorder.StateCommon,
                     r.RibbonGroupCollapsedBorder.StateNormal, r.RibbonGroupCollapsedBorder.StateContextNormal,
                     r.RibbonGroupCollapsedFrameBorder.StateCommon, r.RibbonGroupCollapsedFrameBorder.StateNormal,
                     r.RibbonGroupCollapsedFrameBorder.StateContextNormal })
            RibbonBack(st, t.Border);
        foreach (dynamic st in new dynamic[] { r.RibbonGroupNormalBorder.StateTracking, r.RibbonGroupNormalBorder.StateContextTracking,
                     r.RibbonGroupCollapsedBorder.StateTracking, r.RibbonGroupCollapsedBorder.StateContextTracking,
                     r.RibbonGroupCollapsedFrameBorder.StateTracking, r.RibbonGroupCollapsedFrameBorder.StateContextTracking })
            RibbonBack(st, t.BorderStrong);
        foreach (dynamic st in new dynamic[] { r.RibbonGroupCollapsedBack.StateCommon, r.RibbonGroupCollapsedBack.StateNormal,
                     r.RibbonGroupCollapsedBack.StateContextNormal, r.RibbonGroupCollapsedFrameBack.StateCommon,
                     r.RibbonGroupCollapsedFrameBack.StateNormal, r.RibbonGroupCollapsedFrameBack.StateContextNormal })
            RibbonBack(st, t.Surface);
        foreach (dynamic st in new dynamic[] { r.RibbonGroupCollapsedBack.StateTracking, r.RibbonGroupCollapsedBack.StateContextTracking,
                     r.RibbonGroupCollapsedFrameBack.StateTracking, r.RibbonGroupCollapsedFrameBack.StateContextTracking })
            RibbonBack(st, t.SurfaceAlt);
        foreach (dynamic st in new dynamic[] { r.RibbonGroupNormalTitle.StateCommon, r.RibbonGroupNormalTitle.StateNormal,
                     r.RibbonGroupNormalTitle.StateContextNormal, r.RibbonGroupNormalTitle.StateTracking,
                     r.RibbonGroupNormalTitle.StateContextTracking })
            RibbonDouble(st, t.Surface, t.TextMuted);
        foreach (dynamic st in new dynamic[] { r.RibbonGroupCollapsedText.StateCommon, r.RibbonGroupCollapsedText.StateNormal,
                     r.RibbonGroupCollapsedText.StateTracking, r.RibbonGroupCollapsedText.StateContextNormal,
                     r.RibbonGroupCollapsedText.StateContextTracking })
            st.TextColor = t.Text;

        foreach (dynamic txt in new dynamic[] { r.RibbonGroupButtonText, r.RibbonGroupCheckBoxText, r.RibbonGroupLabelText, r.RibbonGroupRadioButtonText })
        {
            txt.StateCommon.TextColor = t.Text; txt.StateNormal.TextColor = t.Text; txt.StateDisabled.TextColor = t.Disabled;
        }

        r.RibbonFileAppTab.RibbonFileAppTabTopColor = t.Accent;
        r.RibbonFileAppTab.RibbonFileAppTabBottomColor = t.Accent;
        r.RibbonFileAppTab.RibbonFileAppTabTextColor = t.OnAccent;
        RibbonBack(r.RibbonAppButton.StateCommon, t.Accent); RibbonBack(r.RibbonAppButton.StateNormal, t.Accent);
        RibbonBack(r.RibbonAppButton.StateTracking, t.AccentHover); RibbonBack(r.RibbonAppButton.StatePressed, t.AccentPressed);

        foreach (dynamic st in new dynamic[] { r.RibbonAppMenuOuter, r.RibbonAppMenuInner, r.RibbonAppMenuDocs,
                     r.RibbonQATFullbar, r.RibbonQATOverflow, r.RibbonQATMinibar.StateCommon, r.RibbonQATMinibar.StateActive,
                     r.RibbonQATMinibar.StateInactive, r.RibbonGalleryBack })
            RibbonBack(st, t.Surface);
        RibbonBack(r.RibbonQATFullbar, t.Canvas);
        RibbonBack(r.RibbonGalleryBorder, t.Border);
        r.RibbonAppMenuDocsTitle.TextColor = t.Text; r.RibbonAppMenuDocsEntry.TextColor = t.Text;

        Solid(p.PanelStyles.PanelRibbonInactive.StateCommon, t.Canvas);

        // ---- tool strips / status bar / menus (Krypton* strips use the palette) ----
        var m = p.ToolMenuStatus;
        m.StatusStrip.StatusStripGradientBegin = t.Surface; m.StatusStrip.StatusStripGradientEnd = t.Surface;
        m.StatusStrip.StatusStripText = t.Text; m.StatusStrip.StatusStripFont = font;
        m.ToolStrip.ToolStripText = t.Text;
        m.ToolStrip.ToolStripGradientBegin = t.Surface; m.ToolStrip.ToolStripGradientMiddle = t.Surface; m.ToolStrip.ToolStripGradientEnd = t.Surface;
        m.ToolStrip.ToolStripBorder = t.Border; m.ToolStrip.ToolStripDropDownBackground = t.Surface;
        m.ToolStrip.ToolStripPanelGradientBegin = t.Canvas; m.ToolStrip.ToolStripPanelGradientEnd = t.Canvas;
        m.ToolStrip.ToolStripContentPanelGradientBegin = t.Canvas; m.ToolStrip.ToolStripContentPanelGradientEnd = t.Canvas;
        m.MenuStrip.MenuStripText = t.Text;
        m.MenuStrip.MenuStripGradientBegin = t.Surface; m.MenuStrip.MenuStripGradientEnd = t.Surface;
        m.Separator.SeparatorDark = t.Border; m.Separator.SeparatorLight = t.Border;
        m.Grip.GripDark = t.BorderStrong; m.Grip.GripLight = t.Surface;
        m.Menu.MenuBorder = t.Border; m.Menu.MenuItemText = t.Text;
    }

    static void Tab(dynamic s, Color back, Color border, Color text)
    {
        s.Back.Color1 = back; s.Back.Color2 = back; s.Back.ColorStyle = PaletteColorStyle.Solid;
        s.Border.Color1 = border; s.Border.Color2 = border;
        s.Content.ShortText.Color1 = text; s.Content.ShortText.Color2 = text;
    }

    static void CheckedLook(dynamic style, Tokens t, Color selBack)
    {
        Fill(style.StateCheckedNormal, selBack, t.Accent);
        Fill(style.StateCheckedTracking, selBack, t.Accent);
        Fill(style.StateCheckedPressed, selBack, t.Accent);
        foreach (var s in new[] { style.StateCheckedNormal, style.StateCheckedTracking, style.StateCheckedPressed })
        { s.Content.ShortText.Color1 = t.Text; }
    }

    static void RibbonBack(dynamic b, Color c)
    {
        b.BackColor1 = c; b.BackColor2 = c; b.BackColor3 = c; b.BackColor4 = c; b.BackColor5 = c;
    }

    static void RibbonDouble(dynamic b, Color back, Color text)
    {
        RibbonBack(b, back); b.TextColor = text;
    }
}
