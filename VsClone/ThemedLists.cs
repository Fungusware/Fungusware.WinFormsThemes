// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using System.Drawing.Drawing2D;
using System.Text.Json;

namespace KryptonModern.VsClone;

/// <summary>TreeView that paints rows, chevrons, icons and selection from the active theme tokens.</summary>
public sealed class ThemedTree : TreeView
{
    TreeNode _hot;
    public ThemedTree()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        DrawMode = TreeViewDrawMode.OwnerDrawAll; ShowLines = false; ShowRootLines = false; ShowPlusMinus = false;
        FullRowSelect = true; HideSelection = false; ItemHeight = 24; Indent = 16; BorderStyle = BorderStyle.None;
        Font = new Font("Segoe UI", 9.5f);
    }

    protected override void OnDrawNode(DrawTreeNodeEventArgs e)
    {
        var node = e.Node; if (node == null || node.Bounds.Height == 0) return;
        var t = ThemeApplier.Current; var g = e.Graphics;
        int y = node.Bounds.Y, h = ItemHeight;
        bool sel = (e.State & TreeNodeStates.Selected) != 0;
        var row = new Rectangle(0, y, Width, h);
        Color back = sel ? ModernTheme.Blend(t.Accent, t.Surface, Focused ? 0.28f : 0.16f) : node == _hot ? t.SurfaceAlt : t.Surface;
        using (var b = new SolidBrush(back)) g.FillRectangle(b, row);
        int x = 6 + node.Level * Indent;
        if (node.Nodes.Count > 0)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(t.TextMuted, 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            int cx = x + 5, cy = y + h / 2;
            if (node.IsExpanded) g.DrawLines(pen, new[] { new Point(cx - 3, cy - 2), new Point(cx, cy + 2), new Point(cx + 3, cy - 2) });
            else g.DrawLines(pen, new[] { new Point(cx - 2, cy - 4), new Point(cx + 2, cy), new Point(cx - 2, cy + 4) });
            g.SmoothingMode = SmoothingMode.None;
        }
        x += 16;
        if (ImageList != null)
        {
            string key = node.IsExpanded && node.ImageKey == "folder" ? "folderopen" : node.ImageKey;
            if (key.Length > 0 && ImageList.Images.ContainsKey(key)) g.DrawImage(ImageList.Images[key], x, y + (h - 16) / 2, 16, 16);
            x += 22;
        }
        var f = node.NodeFont ?? Font;
        TextRenderer.DrawText(g, node.Text, f, new Rectangle(x, y, Math.Max(10, Width - x), h), node.ForeColor.IsEmpty || node.ForeColor == Color.Empty ? t.Text : node.ForeColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var n = GetNodeAt(1, e.Y);
        if (n != _hot) { _hot = n; Invalidate(); }
    }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = null; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        var n = GetNodeAt(1, e.Y);
        if (n != null && e.Button == MouseButtons.Left && n.Nodes.Count > 0 && e.X < 6 + n.Level * Indent + 16) n.Toggle();
    }
}

/// <summary>Details list with theme-aware rows, severity colouring and optional icons in the first column.</summary>
public sealed class ThemedList : ListView
{
    public Func<ListViewItem, Color?> RowColor;
    public ThemedList()
    {
        Tag = "themed"; // tells ThemeApplier not to attach its own draw handlers
        View = View.Details; FullRowSelect = true; HideSelection = false; OwnerDraw = true; BorderStyle = BorderStyle.None;
        DoubleBuffered = true; Font = new Font("Segoe UI", 9.5f);
        var small = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        SmallImageList = small;
        SizeChanged += (_, _) => Fit();
    }

    protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
    {
        var t = ThemeApplier.Current;
        using var back = new SolidBrush(t.Surface); e.Graphics.FillRectangle(back, e.Bounds);
        using var pen = new Pen(t.Border); e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top + 4, e.Bounds.Right - 1, e.Bounds.Bottom - 5);
        TextRenderer.DrawText(e.Graphics, e.Header.Text, Font, Rectangle.Inflate(e.Bounds, -6, 0), t.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
    protected override void OnDrawItem(DrawListViewItemEventArgs e) { }
    protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
    {
        var t = ThemeApplier.Current; var g = e.Graphics;
        bool sel = e.Item.Selected;
        using (var back = new SolidBrush(sel ? ModernTheme.Blend(t.Accent, t.Surface, 0.22f) : t.Surface)) g.FillRectangle(back, e.Bounds);
        int x = e.Bounds.X + 6;
        if (e.ColumnIndex == 0 && e.Item.ImageKey is { Length: > 0 } k && SmallImageList.Images.ContainsKey(k))
        { g.DrawImage(SmallImageList.Images[k], x, e.Bounds.Y + (e.Bounds.Height - 16) / 2, 16, 16); x += 22; }
        var col = RowColor?.Invoke(e.Item) ?? t.Text;
        TextRenderer.DrawText(g, e.SubItem.Text, Font, new Rectangle(x, e.Bounds.Y, Math.Max(4, e.Bounds.Right - x), e.Bounds.Height), col,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
    }
    public void Fit() { if (Columns.Count > 0) Columns[Columns.Count - 1].Width = -2; }
}

/// <summary>User settings kept in %APPDATA%.</summary>
public sealed class StudioSettings
{
    public string Theme { get; set; } = "Fluent Dark";
    public float FontSize { get; set; } = 10.5f;
    public bool Minimap { get; set; } = true;
    public bool LineNumbers { get; set; } = true;
    public List<string> Recent { get; set; } = new();

    static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KryptonModern", "studio.json");
    public static StudioSettings Load()
    {
        try { if (File.Exists(FilePath)) return JsonSerializer.Deserialize<StudioSettings>(File.ReadAllText(FilePath)) ?? new(); } catch { }
        return new();
    }
    public void Save()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true })); } catch { }
    }
    public void AddRecent(string p)
    {
        Recent.RemoveAll(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase)); Recent.Insert(0, p);
        if (Recent.Count > 8) Recent.RemoveRange(8, Recent.Count - 8);
    }
}

/// <summary>What the Properties window shows for a file or folder.</summary>
public sealed class FileProps
{
    readonly string _p;
    public FileProps(string p) { _p = p; }
    [System.ComponentModel.Category("Misc"), System.ComponentModel.Description("The name of the file or folder.")]
    public string Name => Path.GetFileName(_p);
    [System.ComponentModel.Category("Misc"), System.ComponentModel.Description("The full path on disk.")]
    public string FullPath => _p;
    [System.ComponentModel.Category("Misc")]
    public string Type => Directory.Exists(_p) ? "Folder" : (Path.GetExtension(_p).TrimStart('.').ToUpperInvariant() + " file").Trim();
    [System.ComponentModel.Category("Misc")]
    public string Size => File.Exists(_p) ? new FileInfo(_p).Length.ToString("N0") + " bytes" : "";
    [System.ComponentModel.Category("Misc")]
    public DateTime Modified => File.Exists(_p) ? File.GetLastWriteTime(_p) : Directory.GetLastWriteTime(_p);
    [System.ComponentModel.Category("Advanced"), System.ComponentModel.Description("How the build treats this file.")]
    public string BuildAction => Path.GetExtension(_p).Equals(".cs", StringComparison.OrdinalIgnoreCase) ? "C# compiler" : "None";
    [System.ComponentModel.Category("Advanced")]
    public string CopyToOutputDirectory => "Do not copy";
}
