// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
namespace KryptonModern.Editor;

/// <summary>A zero-based line/column position in the document.</summary>
public readonly record struct Pos(int Line, int Column) : IComparable<Pos>
{
    public int CompareTo(Pos o) => Line != o.Line ? Line.CompareTo(o.Line) : Column.CompareTo(o.Column);
    public static bool operator <(Pos a, Pos b) => a.CompareTo(b) < 0;
    public static bool operator >(Pos a, Pos b) => a.CompareTo(b) > 0;
    public static bool operator <=(Pos a, Pos b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Pos a, Pos b) => a.CompareTo(b) >= 0;
}

/// <summary>What a run of text is, used to pick its colour.</summary>
public enum TokenKind : byte { Plain, Keyword, Control, Type, Method, String, Number, Comment, Preproc, Tag, Attribute, Heading, Punctuation }

/// <summary>A coloured run inside one line. Anything not covered by a token is drawn as <see cref="TokenKind.Plain"/>.</summary>
public readonly record struct Token(int Start, int Length, TokenKind Kind);

public enum Severity { Error, Warning, Info }

/// <summary>A wavy underline under a line, with a tooltip message.</summary>
public sealed record Squiggle(Severity Severity, string Message);

/// <summary>
/// Tokenises one line at a time. The int state carries anything that spans lines (block comments, multi-line strings):
/// it is the value returned for the previous line, and 0 for the first line. Implementations must be thread-safe and stateless.
/// </summary>
public interface ILanguage
{
    string Name { get; }
    /// <summary>Append the line's tokens (ordered, non-overlapping) to <paramref name="output"/> and return the state for the next line.</summary>
    int Tokenize(string line, int state, List<Token> output);
    /// <summary>Line comment marker used by Toggle Comment, or null.</summary>
    string LineComment { get; }
    /// <summary>Words offered by the default completion provider.</summary>
    IReadOnlyList<string> Keywords { get; }
    /// <summary>Whether typing ( [ { " ' inserts the closing character.</summary>
    bool AutoCloseBrackets { get; }
    /// <summary>True when pressing Enter after this text should indent one level.</summary>
    bool ShouldIndent(string textBeforeCaret) => textBeforeCaret.TrimEnd().EndsWith('{');
    /// <summary>True when Enter between these two texts should open a blank indented line between them (for example between { and }).</summary>
    bool ShouldSplit(string before, string after) => ShouldIndent(before) && after.TrimStart().StartsWith('}');
}

public sealed record CompletionItem(string Text, TokenKind Kind = TokenKind.Plain);

/// <summary>Supplies the completion list. Called on the UI thread on each typed identifier character, so keep it quick.</summary>
public interface ICompletionProvider
{
    IReadOnlyList<CompletionItem> GetItems(CodeEditor editor, string prefix, bool forced);
}

/// <summary>Colours for the editor. Plain data: build one by hand or adapt your application's theme.</summary>
public sealed class EditorTheme
{
    public Color Background, Foreground, GutterForeground, GutterActiveForeground, CurrentLine, Selection, SelectionInactive,
        IndentGuide, BraceMatch, BraceMatchBorder, FindMatch, Caret, Error, Warning, Info, PopupBackground, PopupBorder, PopupSelection, PopupText, PopupMatch;
    /// <summary>One colour per <see cref="TokenKind"/>, indexed by the enum value.</summary>
    public Color[] Tokens = new Color[Enum.GetValues<TokenKind>().Length];

    public static EditorTheme Dark { get; } = Make(true);
    public static EditorTheme Light { get; } = Make(false);

    static Color C(int rgb) => Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
    public static Color Mix(Color a, Color b, float amountOfA) =>
        Color.FromArgb((int)(a.R * amountOfA + b.R * (1 - amountOfA)), (int)(a.G * amountOfA + b.G * (1 - amountOfA)), (int)(a.B * amountOfA + b.B * (1 - amountOfA)));

    /// <summary>Fills every derived colour from a few base colours, so a theme is just background, foreground, accent and the token palette.</summary>
    public static EditorTheme FromBase(Color background, Color foreground, Color accent, Color[] tokens, bool dark)
    {
        var t = new EditorTheme
        {
            Background = background, Foreground = foreground, Caret = foreground,
            GutterForeground = Mix(foreground, background, 0.45f), GutterActiveForeground = foreground,
            CurrentLine = Mix(accent, background, 0.08f), Selection = Mix(accent, background, 0.34f), SelectionInactive = Mix(accent, background, 0.16f),
            IndentGuide = Mix(foreground, background, 0.14f), BraceMatch = Mix(accent, background, 0.28f), BraceMatchBorder = accent,
            FindMatch = dark ? Color.FromArgb(0x6B, 0x55, 0x1F) : Color.FromArgb(0xF8, 0xD8, 0x7A),
            Error = dark ? C(0xF14C4C) : C(0xE51400), Warning = dark ? C(0xCCA700) : C(0x2E8B00), Info = dark ? C(0x75BEFF) : C(0x1A85FF),
            PopupBackground = background, PopupBorder = Mix(foreground, background, 0.35f), PopupSelection = Mix(accent, background, 0.34f),
            PopupText = foreground, PopupMatch = accent,
        };
        for (int i = 0; i < t.Tokens.Length; i++) t.Tokens[i] = i < tokens.Length ? tokens[i] : foreground;
        t.Tokens[(int)TokenKind.Plain] = foreground;
        return t;
    }

    static EditorTheme Make(bool dark) => FromBase(
        dark ? C(0x1E1E1E) : Color.White, dark ? C(0xD4D4D4) : C(0x1E1E1E), dark ? C(0x4A9CFF) : C(0x2563EB),
        (dark
            ? new[] { 0xD4D4D4, 0x569CD6, 0xC586C0, 0x4EC9B0, 0xDCDCAA, 0xCE9178, 0xB5CEA8, 0x6A9955, 0x9B9B9B, 0x569CD6, 0x9CDCFE, 0x569CD6, 0x808080 }
            : new[] { 0x1E1E1E, 0x0000FF, 0x8F08C4, 0x267F99, 0x795E26, 0xA31515, 0x098658, 0x008000, 0x808080, 0x800000, 0xE50000, 0x0000FF, 0x6B6B6B }).Select(C).ToArray(),
        dark);
}
