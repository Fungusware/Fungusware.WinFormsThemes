// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace KryptonModern.VsClone;

/// <summary>Icons drawn in code: Segoe Fluent glyphs tinted with the theme, plus little file-type badges.</summary>
public static class Ico
{
    static readonly string FontName = FontFamily.Families.Any(f => f.Name == "Segoe Fluent Icons") ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";

    public const char Save = '', Undo = '', Redo = '', Play = '', Stop = '', Back = '', Forward = '',
        Folder = '', FolderOpen = '', OpenFile = '', Page = '', NewFile = '', Search = '', Close = '', Settings = '',
        Refresh = '', Pin = '', Copy = '', Cut = '', Paste = '', Delete = '', Build = '', Error = '',
        Warning = '', Info = '', Package = '', Code = '', Chevron = '', Down = '', Up = '', Home = '',
        Collapse = '', Toolbox = '', Bulb = '', Terminal = '', Properties = '', List = '', Git = '', Bug = '',
        Help = '', Sync = '', Component = '', Ref = '', Replace = '', Hammer = '';

    public static Bitmap Glyph(char glyph, Color color, int size = 16, float scale = 0.82f)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using var f = new Font(FontName, size * scale, FontStyle.Regular, GraphicsUnit.Pixel);
        using var b = new SolidBrush(color);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(glyph.ToString(), f, b, new RectangleF(0, 0, size, size + 1), sf);
        return bmp;
    }

    public static Bitmap Badge(string text, Color fill, int size = 16)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using var p = CardPanel.Round(new RectangleF(0.5f, 1.5f, size - 1.5f, size - 3f), 3);
        using var b = new SolidBrush(fill);
        g.FillPath(b, p);
        using var f = new Font("Segoe UI", text.Length > 2 ? size * 0.34f : size * 0.42f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, f, Brushes.White, new RectangleF(0, 1, size, size - 1), sf);
        return bmp;
    }

    public static Bitmap Dot(Color c, int size = 16)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp); g.SmoothingMode = SmoothingMode.AntiAlias;
        using var b = new SolidBrush(c); g.FillEllipse(b, 3, 3, size - 6, size - 6);
        return bmp;
    }

    /// <summary>Key into the file image list for a path.</summary>
    public static string KeyFor(string path)
    {
        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".cs": case ".csx": return "cs";
            case ".csproj": case ".vbproj": case ".fsproj": return "proj";
            case ".sln": case ".slnx": return "sln";
            case ".json": case ".jsonc": return "json";
            case ".xml": case ".config": case ".props": case ".targets": case ".resx": case ".xaml": case ".manifest": case ".pubxml": case ".user": return "xml";
            case ".md": case ".markdown": return "md";
            case ".png": case ".jpg": case ".jpeg": case ".gif": case ".ico": case ".bmp": case ".svg": return "img";
            case ".html": case ".htm": return "html";
            case ".ps1": case ".bat": case ".cmd": case ".sh": return "script";
            case ".gitignore": case ".gitattributes": return "git";
            case ".dll": case ".exe": case ".pdb": return "bin";
            default: return Path.GetFileName(path).StartsWith(".git") ? "git" : "file";
        }
    }

    public static ImageList FileImages(Color glyphColor)
    {
        var il = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = new Size(16, 16) };
        void A(string k, Bitmap b) => il.Images.Add(k, b);
        A("cs", Badge("C#", Color.FromArgb(0x3C, 0x9A, 0x3C)));
        A("proj", Badge("C#", Color.FromArgb(0x92, 0x4B, 0xA8)));
        A("sln", Badge("VS", Color.FromArgb(0x6E, 0x4F, 0xC8)));
        A("json", Badge("{}", Color.FromArgb(0xD2, 0x9A, 0x1E)));
        A("xml", Badge("<>", Color.FromArgb(0x3B, 0x82, 0xD6)));
        A("md", Badge("M", Color.FromArgb(0x4F, 0x7D, 0xC0)));
        A("img", Badge("IMG", Color.FromArgb(0x1E, 0xA7, 0x9A)));
        A("html", Badge("H", Color.FromArgb(0xE3, 0x6B, 0x2C)));
        A("script", Badge(">_", Color.FromArgb(0x58, 0x63, 0x73)));
        A("git", Badge("G", Color.FromArgb(0xE0, 0x5A, 0x33)));
        A("bin", Badge("0x", Color.FromArgb(0x7A, 0x82, 0x92)));
        A("file", Glyph(Page, glyphColor));
        A("folder", Glyph(Folder, Color.FromArgb(0xDC, 0xB6, 0x7A)));
        A("folderopen", Glyph(FolderOpen, Color.FromArgb(0xDC, 0xB6, 0x7A)));
        A("pkg", Glyph(Package, Color.FromArgb(0xB0, 0x7B, 0xD8)));
        A("ref", Glyph(Ref, glyphColor));
        A("error", Glyph(Error, Color.FromArgb(0xE5, 0x4B, 0x4B)));
        A("warning", Glyph(Warning, Color.FromArgb(0xE0, 0xA8, 0x1E)));
        A("info", Glyph(Info, Color.FromArgb(0x3B, 0x82, 0xD6)));
        A("component", Glyph(Component, Color.FromArgb(0x3B, 0x82, 0xD6)));
        A("deps", Glyph(Ref, Color.FromArgb(0xB0, 0x7B, 0xD8)));
        A("solution", Badge("VS", Color.FromArgb(0x6E, 0x4F, 0xC8)));
        return il;
    }

    public static Icon AppIcon(Color accent)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using var p = CardPanel.Round(new RectangleF(1, 1, 30, 30), 7);
            using var b = new SolidBrush(accent); g.FillPath(b, p);
            using var f = new Font("Segoe UI", 17, FontStyle.Bold, GraphicsUnit.Pixel);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("K", f, Brushes.White, new RectangleF(0, 1, 32, 32), sf);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }
}
