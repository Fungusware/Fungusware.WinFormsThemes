// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using System.Text.RegularExpressions;
using KryptonModern.Editor;

namespace KryptonModern.VsClone;

/// <summary>The adapter between KryptonModern themes and the standalone editor: surface/accent from the theme, token colours per theme family.</summary>
public static class EditorThemes
{
    static Color C(int rgb) => Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);

    public static EditorTheme From(Tokens t)
    {
        bool dark = (t.Surface.R * 0.299 + t.Surface.G * 0.587 + t.Surface.B * 0.114) < 128;
        // [Plain, Keyword, Control, Type, Method, String, Number, Comment, Preproc, Tag, Attribute, Heading, Punctuation]
        int[] p = t.Name switch
        {
            "Dracula" => new[] { 0xF8F8F2, 0xFF79C6, 0xFF79C6, 0x8BE9FD, 0x50FA7B, 0xF1FA8C, 0xBD93F9, 0x6272A4, 0xFF9AC1, 0xFF79C6, 0x50FA7B, 0xBD93F9, 0xB0B6D0 },
            "Nord" => new[] { 0xECEFF4, 0x81A1C1, 0xB48EAD, 0x8FBCBB, 0x88C0D0, 0xA3BE8C, 0xB48EAD, 0x616E88, 0x5E81AC, 0x81A1C1, 0x8FBCBB, 0x88C0D0, 0xAEB8CB },
            "Solarized Dark" => new[] { 0x93A1A1, 0x859900, 0xCB4B16, 0xB58900, 0x268BD2, 0x2AA198, 0xD33682, 0x586E75, 0xCB4B16, 0x268BD2, 0x93A1A1, 0xB58900, 0x657B83 },
            "Solarized Light" => new[] { 0x586E75, 0x859900, 0xCB4B16, 0xB58900, 0x268BD2, 0x2AA198, 0xD33682, 0x93A1A1, 0xCB4B16, 0x268BD2, 0x586E75, 0xB58900, 0x93A1A1 },
            "Neon Synthwave" => new[] { 0xF4ECFF, 0xFF7EDB, 0xFF3CAC, 0x36F9F6, 0xFEDE5D, 0xFF8B39, 0xF97E72, 0x848BBD, 0xB5A3DD, 0xFF7EDB, 0x72F1B8, 0xFEDE5D, 0xB5A3DD },
            "Material Dark" => new[] { 0xE6E0E9, 0xD0BCFF, 0xEFB8C8, 0x80CBC4, 0xFFCB6B, 0xC3E88D, 0xF78C6C, 0x7C7A85, 0xA8A2B0, 0xF07178, 0xFFCB6B, 0xD0BCFF, 0x938F99 },
            "Material Light" => new[] { 0x1D1B20, 0x6750A4, 0x984061, 0x00695C, 0x8F4C00, 0x2E7D32, 0xB3261E, 0x79747E, 0x79747E, 0x6750A4, 0x8F4C00, 0x6750A4, 0x49454F },
            _ when dark => new[] { 0xD4D4D4, 0x569CD6, 0xC586C0, 0x4EC9B0, 0xDCDCAA, 0xCE9178, 0xB5CEA8, 0x6A9955, 0x9B9B9B, 0x569CD6, 0x9CDCFE, 0x569CD6, 0x808080 },
            _ => new[] { 0x1E1E1E, 0x0000FF, 0x8F08C4, 0x267F99, 0x795E26, 0xA31515, 0x098658, 0x008000, 0x808080, 0x800000, 0xE50000, 0x0000FF, 0x6B6B6B },
        };
        var th = EditorTheme.FromBase(t.Surface, t.Text, t.Accent, p.Select(C).ToArray(), dark);
        th.GutterForeground = ModernTheme.Blend(t.TextMuted, t.Surface, 0.75f);
        th.IndentGuide = ModernTheme.Blend(t.BorderStrong, t.Surface, 0.35f);
        return th;
    }
}

/// <summary>Finds the namespace / type / member around the caret for the navigation bar (a regex heuristic, not a parser).</summary>
public static class CodeStructure
{
    static readonly Regex TypeRx = new(@"^\s*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|unsafe|file)\s+)*(?:class|struct|interface|enum|record(?:\s+struct)?)\s+(\w+)", RegexOptions.Compiled);
    static readonly Regex MemberRx = new(@"^\s+(?:(?:public|internal|private|protected|static|override|virtual|abstract|async|sealed|unsafe|extern|new)\s+)*[\w<>\[\],.?\s]+?\s+(\w+)\s*(?:<[^>]*>)?\s*\(", RegexOptions.Compiled);
    static readonly Regex NsRx = new(@"^\s*namespace\s+([\w.]+)", RegexOptions.Compiled);
    static readonly Regex NotMember = new(@"^\s+(?:if|for|foreach|while|switch|return|using|lock|catch|else|new|throw|await)\b", RegexOptions.Compiled);

    public static (string Ns, string Type, string Member) Where(CodeEditor ed)
    {
        string ns = "", type = "", member = "";
        for (int i = 0; i < Math.Min(ed.LineCount, 400); i++) { var m = NsRx.Match(ed.GetLine(i)); if (m.Success) { ns = m.Groups[1].Value; break; } }
        int typeLine = -1;
        for (int i = ed.Caret.Line; i >= Math.Max(0, ed.Caret.Line - 3000); i--) { var m = TypeRx.Match(ed.GetLine(i)); if (m.Success) { type = m.Groups[1].Value; typeLine = i; break; } }
        if (typeLine >= 0)
            for (int i = ed.Caret.Line; i > typeLine; i--)
            {
                string l = ed.GetLine(i);
                if (l.TrimStart().StartsWith("//") || l.Contains("=>") && !l.Contains('(')) continue;
                var m = MemberRx.Match(l);
                if (m.Success && !NotMember.IsMatch(l)) { member = m.Groups[1].Value + "()"; break; }
            }
        return (ns, type, member);
    }
}
