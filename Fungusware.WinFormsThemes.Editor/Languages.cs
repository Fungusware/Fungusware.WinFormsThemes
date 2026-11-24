// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
namespace Fungusware.WinFormsThemes.Editor;

/// <summary>Built-in languages and a lookup by file name.</summary>
public static class Languages
{
    public static ILanguage Plain => PlainLanguage.Instance;
    public static ILanguage CSharp => CSharpLanguage.Instance;
    public static ILanguage Json => JsonLanguage.Instance;
    public static ILanguage Xml => XmlLanguage.Instance;
    public static ILanguage Markdown => MarkdownLanguage.Instance;

    public static ILanguage FromPath(string path)
    {
        switch (Path.GetExtension(path ?? "").ToLowerInvariant())
        {
            case ".cs": case ".csx": return CSharp;
            case ".json": case ".jsonc": return Json;
            case ".csproj": case ".vbproj": case ".fsproj": case ".slnx": case ".xml": case ".xaml": case ".props": case ".targets": case ".config": case ".resx":
            case ".html": case ".htm": case ".svg": case ".pubxml": case ".manifest": case ".user": return Xml;
            case ".md": case ".markdown": return Markdown;
            default: return Plain;
        }
    }
}

public sealed class PlainLanguage : ILanguage
{
    public static readonly PlainLanguage Instance = new();
    public string Name => "Plain text";
    public int Tokenize(string line, int state, List<Token> output) => 0;
    public string LineComment => null;
    public IReadOnlyList<string> Keywords => Array.Empty<string>();
    public bool AutoCloseBrackets => false;
}

public sealed class CSharpLanguage : ILanguage
{
    public string Name => "C#";
    public string LineComment => "//";
    public bool AutoCloseBrackets => true;

    static readonly string[] Kws =
    {
        "abstract","as","base","bool","byte","char","checked","class","const","decimal","delegate","double","enum","event","explicit","extern",
        "false","fixed","float","implicit","in","int","interface","internal","is","lock","long","namespace","new","null","object","operator","out",
        "override","params","private","protected","public","readonly","ref","sbyte","sealed","short","sizeof","stackalloc","static","string","struct",
        "this","true","typeof","uint","ulong","unchecked","unsafe","ushort","using","virtual","void","volatile","var","dynamic","async","await",
        "record","init","required","get","set","value","partial","where","yield","nameof","global","with","when"
    };
    static readonly string[] Ctrl = { "if", "else", "switch", "case", "default", "for", "foreach", "while", "do", "break", "continue", "return", "goto", "throw", "try", "catch", "finally" };
    static readonly HashSet<string> KwSet = new(Kws), CtlSet = new(Ctrl);
    public static readonly CSharpLanguage Instance = new();
    public IReadOnlyList<string> Keywords { get; } = Kws.Concat(Ctrl).ToArray();

    // state: 0 normal, 1 inside /* */, 2 inside a multi-line verbatim string
    public int Tokenize(string s, int state, List<Token> o)
    {
        int n = s.Length, i = 0;
        if (state == 1)
        {
            int e = s.IndexOf("*/", StringComparison.Ordinal);
            if (e < 0) { o.Add(new(0, n, TokenKind.Comment)); return 1; }
            o.Add(new(0, e + 2, TokenKind.Comment)); i = e + 2;
        }
        else if (state == 2)
        {
            int k = 0; bool closed = false;
            while (k < n) { if (s[k] == '"') { if (k + 1 < n && s[k + 1] == '"') { k += 2; continue; } k++; closed = true; break; } k++; }
            o.Add(new(0, k, TokenKind.String));
            if (!closed) return 2;
            i = k;
        }
        string prevWord = null;
        while (i < n)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            char nx = i + 1 < n ? s[i + 1] : '\0';
            if (c == '/' && nx == '/') { o.Add(new(i, n - i, TokenKind.Comment)); return 0; }
            if (c == '/' && nx == '*')
            {
                int e = s.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (e < 0) { o.Add(new(i, n - i, TokenKind.Comment)); return 1; }
                o.Add(new(i, e + 2 - i, TokenKind.Comment)); i = e + 2; continue;
            }
            if (c == '#' && o.Count == 0) { o.Add(new(i, n - i, TokenKind.Preproc)); return 0; }
            if (c == '"' || c == '$' || c == '@')
            {
                int j = i; bool verbatim = false;
                while (j < n && (s[j] == '$' || s[j] == '@')) { if (s[j] == '@') verbatim = true; j++; }
                if (j < n && s[j] == '"')
                {
                    int k = j + 1; bool closed = false;
                    if (verbatim)
                        while (k < n) { if (s[k] == '"') { if (k + 1 < n && s[k + 1] == '"') { k += 2; continue; } k++; closed = true; break; } k++; }
                    else
                        while (k < n) { if (s[k] == '\\') { k += 2; continue; } if (s[k] == '"') { k++; closed = true; break; } k++; }
                    k = Math.Min(k, n);
                    o.Add(new(i, k - i, TokenKind.String)); i = k; prevWord = null;
                    if (verbatim && !closed) return 2;
                    continue;
                }
                if (c == '@' && i + 1 < n && (char.IsLetter(nx) || nx == '_')) { i++; continue; }
                i++; continue;
            }
            if (c == '\'')
            {
                int k = i + 1;
                while (k < n) { if (s[k] == '\\') { k += 2; continue; } if (s[k] == '\'') { k++; break; } k++; }
                k = Math.Min(k, n);
                o.Add(new(i, k - i, TokenKind.String)); i = k; prevWord = null; continue;
            }
            if (char.IsDigit(c) || (c == '.' && char.IsDigit(nx)))
            {
                int j = i + 1;
                while (j < n && (char.IsLetterOrDigit(s[j]) || s[j] == '_' || (s[j] == '.' && j + 1 < n && char.IsDigit(s[j + 1])))) j++;
                o.Add(new(i, j - i, TokenKind.Number)); i = j; prevWord = null; continue;
            }
            if (char.IsLetter(c) || c == '_')
            {
                int j = i + 1;
                while (j < n && (char.IsLetterOrDigit(s[j]) || s[j] == '_')) j++;
                string w = s.Substring(i, j - i);
                int b = i - 1; while (b >= 0 && s[b] == ' ') b--;
                bool afterDot = b >= 0 && s[b] == '.';
                int a = j; while (a < n && s[a] == ' ') a++;
                bool call = a < n && s[a] == '(';
                TokenKind k;
                if (!afterDot && CtlSet.Contains(w)) k = TokenKind.Control;
                else if (!afterDot && KwSet.Contains(w)) k = TokenKind.Keyword;
                else if (call && prevWord != "new") k = TokenKind.Method;
                else if (afterDot) k = TokenKind.Plain;
                else if (char.IsUpper(w[0]) && !AllCaps(w)) k = TokenKind.Type;
                else k = TokenKind.Plain;
                if (k != TokenKind.Plain) o.Add(new(i, j - i, k));
                prevWord = w; i = j; continue;
            }
            prevWord = null; i++;
        }
        return 0;
    }

    static bool AllCaps(string w)
    {
        if (w.Length < 2) return false;
        foreach (char ch in w) if (!(char.IsUpper(ch) || char.IsDigit(ch) || ch == '_')) return false;
        return true;
    }
}

public sealed class JsonLanguage : ILanguage
{
    public static readonly JsonLanguage Instance = new();
    public string Name => "JSON";
    public string LineComment => "//";
    public bool AutoCloseBrackets => true;
    public IReadOnlyList<string> Keywords { get; } = new[] { "true", "false", "null" };
    public bool ShouldIndent(string before) { var t = before.TrimEnd(); return t.EndsWith('{') || t.EndsWith('['); }
    public bool ShouldSplit(string before, string after) { var a = after.TrimStart(); return ShouldIndent(before) && (a.StartsWith('}') || a.StartsWith(']')); }

    public int Tokenize(string s, int state, List<Token> o)
    {
        int n = s.Length, i = 0;
        while (i < n)
        {
            char c = s[i];
            if (c == '/' && i + 1 < n && s[i + 1] == '/') { o.Add(new(i, n - i, TokenKind.Comment)); break; }
            if (c == '"')
            {
                int k = i + 1;
                while (k < n) { if (s[k] == '\\') { k += 2; continue; } if (s[k] == '"') { k++; break; } k++; }
                k = Math.Min(k, n);
                int a = k; while (a < n && s[a] == ' ') a++;
                o.Add(new(i, k - i, a < n && s[a] == ':' ? TokenKind.Attribute : TokenKind.String)); i = k; continue;
            }
            if (char.IsDigit(c) || (c == '-' && i + 1 < n && char.IsDigit(s[i + 1])))
            {
                int j = i + 1; while (j < n && (char.IsLetterOrDigit(s[j]) || s[j] == '.' || s[j] == '-' || s[j] == '+')) j++;
                o.Add(new(i, j - i, TokenKind.Number)); i = j; continue;
            }
            if (char.IsLetter(c))
            {
                int j = i + 1; while (j < n && char.IsLetter(s[j])) j++;
                string w = s.Substring(i, j - i);
                if (w is "true" or "false" or "null") o.Add(new(i, j - i, TokenKind.Keyword));
                i = j; continue;
            }
            i++;
        }
        return 0;
    }
}

public sealed class XmlLanguage : ILanguage
{
    public static readonly XmlLanguage Instance = new();
    public string Name => "XML";
    public string LineComment => null;
    public bool AutoCloseBrackets => false;
    public IReadOnlyList<string> Keywords => Array.Empty<string>();
    public bool ShouldIndent(string before) { var t = before.TrimEnd(); return t.EndsWith('>') && !t.EndsWith("/>") && !t.EndsWith("-->") && !t.Contains("</") && !t.TrimStart().StartsWith("<?") && !t.TrimStart().StartsWith("<!"); }
    public bool ShouldSplit(string before, string after) => ShouldIndent(before) && after.TrimStart().StartsWith("</");

    // state: 0 text, 1 inside <!-- -->, 2 inside a tag
    public int Tokenize(string s, int state, List<Token> o)
    {
        int n = s.Length, i = 0;
        while (i < n)
        {
            if (state == 1)
            {
                int e = s.IndexOf("-->", i, StringComparison.Ordinal);
                if (e < 0) { o.Add(new(i, n - i, TokenKind.Comment)); return 1; }
                o.Add(new(i, e + 3 - i, TokenKind.Comment)); i = e + 3; state = 0; continue;
            }
            if (state == 0)
            {
                int lt = s.IndexOf('<', i);
                if (lt < 0) return 0;
                if (string.CompareOrdinal(s, lt, "<!--", 0, 4) == 0) { state = 1; i = lt; continue; }
                int j = lt + 1;
                if (j < n && (s[j] == '/' || s[j] == '?' || s[j] == '!')) j++;
                o.Add(new(lt, j - lt, TokenKind.Punctuation));
                int k = j; while (k < n && (char.IsLetterOrDigit(s[k]) || s[k] is ':' or '.' or '-' or '_')) k++;
                if (k > j) o.Add(new(j, k - j, TokenKind.Tag));
                i = k; state = 2; continue;
            }
            char c = s[i];
            if (c == '>') { o.Add(new(i, 1, TokenKind.Punctuation)); i++; state = 0; continue; }
            if (c == '/' || c == '?') { o.Add(new(i, 1, TokenKind.Punctuation)); i++; continue; }
            if (c == '"' || c == '\'')
            {
                int e = s.IndexOf(c, i + 1); int end = e < 0 ? n : e + 1;
                o.Add(new(i, end - i, TokenKind.String)); i = end; continue;
            }
            if (char.IsLetter(c))
            {
                int k = i + 1; while (k < n && (char.IsLetterOrDigit(s[k]) || s[k] is ':' or '.' or '-' or '_')) k++;
                o.Add(new(i, k - i, TokenKind.Attribute)); i = k; continue;
            }
            i++;
        }
        return state;
    }
}

public sealed class MarkdownLanguage : ILanguage
{
    public static readonly MarkdownLanguage Instance = new();
    public string Name => "Markdown";
    public string LineComment => null;
    public bool AutoCloseBrackets => false;
    public IReadOnlyList<string> Keywords => Array.Empty<string>();
    public bool ShouldIndent(string before) => false;

    // state: 0 text, 1 inside a ``` fence
    public int Tokenize(string s, int state, List<Token> o)
    {
        if (s.StartsWith("```")) { o.Add(new(0, s.Length, TokenKind.Preproc)); return state == 1 ? 0 : 1; }
        if (state == 1) { if (s.Length > 0) o.Add(new(0, s.Length, TokenKind.String)); return 1; }
        if (s.StartsWith('#')) { o.Add(new(0, s.Length, TokenKind.Heading)); return 0; }
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '`') { int e = s.IndexOf('`', i + 1); if (e > 0) { o.Add(new(i, e - i + 1, TokenKind.String)); i = e + 1; continue; } }
            i++;
        }
        return 0;
    }
}
