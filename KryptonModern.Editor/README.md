# KryptonModern.Editor

A themeable WinForms code editor control. Syntax colouring, line numbers, minimap, breakpoint margin, brace matching, auto-close and auto-indent, completion list, find highlighting, squiggles, undo/redo. Plain WinForms, no third-party packages (it does not need Krypton). By **Fungusware**, MIT license.

It is the editor inside Krypton Studio (`KM_FORM=4` in the demo).

## Use it

```csharp
using KryptonModern.Editor;

var editor = new CodeEditor { Dock = DockStyle.Fill };
editor.Language = Languages.FromPath("Program.cs");   // or Languages.CSharp
editor.Theme = EditorTheme.Dark;                      // or EditorTheme.Light, or your own
editor.LoadText(File.ReadAllText(path));
Controls.Add(editor);

editor.Changed += (_, _) => Text = editor.Modified ? "Program.cs *" : "Program.cs";
editor.CaretMoved += (_, _) => status.Text = $"Ln {editor.Caret.Line + 1}, Col {editor.Caret.Column + 1}";
File.WriteAllText(path, editor.GetText()); editor.MarkSaved();
```

## What you can change

| Thing | How |
|---|---|
| Colours | `EditorTheme` is plain data. `EditorTheme.FromBase(background, foreground, accent, tokenColours, dark)` fills every derived colour. The demo's `EditorThemes.From(Tokens)` maps a KryptonModern theme to one. |
| A language | Implement `ILanguage`: tokenise one line given the state from the previous line (for block comments and multi-line strings). Built in: C#, JSON, XML, Markdown, plain text. |
| Completion | Set `CompletionProvider` to your own `ICompletionProvider`, or null to turn it off. The default offers keywords plus words in the file. |
| Diagnostics | `editor.Squiggles[line] = new Squiggle(Severity.Error, "message")` shows a wavy underline, minimap mark and tooltip. They clear on the next edit. |
| Breakpoints | `editor.Breakpoints` (click the left margin to toggle) and `BreakpointsChanged`. |
| Look | `EditorFontFamily`, `EditorFontSize`, `ShowMinimap`, `ShowLineNumbers`, `ShowIndentGuides`, `IndentSize`, `ReadOnly`. |
| Edits | `Replace`, `InsertText`, `BeginGroup`/`EndGroup` (one undo step), plus the usual Undo, Redo, Cut, Copy, Paste, Find, ReplaceAll, GoTo. |

## Why it stays fast

- The text is a list of lines. Every edit goes through one method and is stored as an edit, so undo does not copy the document.
- Lexing keeps one int of state per line and runs on a worker thread in chunks. The lines on screen are tokenised on demand when painted, so typing never waits on the rest of the file.
- Adjacent text of the same colour is drawn in one call. The minimap is a cached bitmap that is rebuilt only when the text, theme, size or scroll position changes.
- Measured in the demo on a 100,000-line, 9.9 MB C# file: open 0.3 s, jump to the end 30 ms, typing about 0.6 ms per character, a repaint about 12 ms.

## Limits

No word wrap, no code folding, no multiple carets, and single-line find only. Tabs are converted to spaces when a file is loaded. Breakpoint and squiggle line numbers are not shifted when lines are inserted above them.
