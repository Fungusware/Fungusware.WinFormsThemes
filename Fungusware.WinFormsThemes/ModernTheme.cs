using System;
using System.Drawing;
using System.Windows.Forms;

namespace Fungusware.WinFormsThemes
{
    /// <summary>Colour tokens for one appearance.</summary>
    public sealed record Tokens(
        Color Canvas,       // window background
        Color Surface,      // inputs, lists, cards
        Color SurfaceAlt,   // hover / subtle fills
        Color Border,
        Color BorderStrong,
        Color Text,
        Color TextMuted,
        Color Accent,
        Color AccentHover,
        Color AccentPressed,
        Color OnAccent,
        Color Disabled,
        string Name = "Theme",
        float Radius = 6f,
        float CardRadius = 10f);

    /// <summary>
    /// A Krypton palette that imitates Windows 11 / modern web controls:
    /// flat fills, 1px hairlines, rounded corners, one accent colour.
    /// </summary>
    public static partial class ModernTheme
    {
        /// <summary>Tone for menu bars, toolbars and the status bar: a step darker than the form so they read as chrome.</summary>
        public static Color Chrome(Tokens t) => Blend(t.Border, t.BorderStrong, 0.88f);

        /// <summary>Windows 11 light.</summary>
        public static readonly Tokens FluentLight = new(
            Canvas: Color.FromArgb(0xF3, 0xF4, 0xF7),
            Surface: Color.White,
            SurfaceAlt: Color.FromArgb(0xEB, 0xED, 0xF2),
            Border: Color.FromArgb(0xDD, 0xE0, 0xE7),
            BorderStrong: Color.FromArgb(0x9A, 0xA1, 0xB0),
            Text: Color.FromArgb(0x1C, 0x21, 0x2B),
            TextMuted: Color.FromArgb(0x5B, 0x63, 0x73),
            Accent: Color.FromArgb(0x25, 0x63, 0xEB),
            AccentHover: Color.FromArgb(0x1D, 0x4E, 0xC7),
            AccentPressed: Color.FromArgb(0x17, 0x3F, 0xA3),
            OnAccent: Color.White,
            Disabled: Color.FromArgb(0xA8, 0xAE, 0xBA),
            Name: "Fluent Light");

        /// <summary>Windows 11 dark.</summary>
        public static readonly Tokens FluentDark = new(
            Canvas: Color.FromArgb(0x14, 0x17, 0x1D),
            Surface: Color.FromArgb(0x1E, 0x22, 0x2B),
            SurfaceAlt: Color.FromArgb(0x2A, 0x2F, 0x3A),
            Border: Color.FromArgb(0x32, 0x38, 0x45),
            BorderStrong: Color.FromArgb(0x6B, 0x74, 0x86),
            Text: Color.FromArgb(0xEE, 0xF0, 0xF4),
            TextMuted: Color.FromArgb(0x9A, 0xA3, 0xB5),
            Accent: Color.FromArgb(0x5B, 0x8D, 0xFF),
            AccentHover: Color.FromArgb(0x7A, 0xA3, 0xFF),
            AccentPressed: Color.FromArgb(0x45, 0x78, 0xEB),
            OnAccent: Color.FromArgb(0x0B, 0x13, 0x28),
            Disabled: Color.FromArgb(0x5A, 0x62, 0x73),
            Name: "Fluent Dark");

        static Color C(int rgb) => Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);

        static Tokens T(string name, float radius, float card, int canvas, int surface, int alt, int border, int strong,
                        int text, int muted, int accent, int hover, int pressed, int onAccent, int disabled) =>
            new(C(canvas), C(surface), C(alt), C(border), C(strong), C(text), C(muted),
                C(accent), C(hover), C(pressed), C(onAccent), C(disabled), name, radius, card);

        public static readonly Tokens MaterialLight = T("Material Light", 4, 12,
            0xF7F2FA, 0xFFFFFF, 0xECE6F0, 0xCAC4D0, 0x79747E, 0x1D1B20, 0x49454F,
            0x6750A4, 0x5B4698, 0x4F3D8A, 0xFFFFFF, 0xB0AAB5);

        public static readonly Tokens MaterialDark = T("Material Dark", 4, 12,
            0x141218, 0x211F26, 0x2B2930, 0x49454F, 0x938F99, 0xE6E0E9, 0xCAC4D0,
            0xD0BCFF, 0xE0D0FF, 0xB9A2F2, 0x381E72, 0x6E6A75);

        public static readonly Tokens SolarizedLight = T("Solarized Light", 2, 4,
            0xEEE8D5, 0xFDF6E3, 0xE6DFC8, 0xD6CFB6, 0x93A1A1, 0x586E75, 0x657B83,
            0x268BD2, 0x2E9BE5, 0x1F78B4, 0x002B36, 0xA3ABA6);

        public static readonly Tokens SolarizedDark = T("Solarized Dark", 2, 4,
            0x002B36, 0x073642, 0x0E4452, 0x1C5565, 0x586E75, 0x93A1A1, 0x7F9296,
            0x268BD2, 0x4AA3E8, 0x1F78B4, 0x002B36, 0x586E75);

        public static readonly Tokens Nord = T("Nord", 4, 8,
            0x2E3440, 0x3B4252, 0x434C5E, 0x4C566A, 0x7B88A1, 0xECEFF4, 0xAEB8CB,
            0x88C0D0, 0x9FD0DE, 0x81A1C1, 0x2E3440, 0x6B7790);

        public static readonly Tokens Dracula = T("Dracula", 6, 10,
            0x21222C, 0x282A36, 0x343746, 0x44475A, 0x6272A4, 0xF8F8F2, 0xB0B6D0,
            0xBD93F9, 0xCDA9FF, 0xA47BE8, 0x282A36, 0x6272A4);

        /// <summary>Every theme the app offers, in menu order.</summary>
        public static readonly Tokens NeonSynthwave = T("Neon Synthwave", 8, 14,
            0x0B0618, 0x150C2E, 0x20143F, 0x35235F, 0x7A5FBF, 0xF4ECFF, 0xB5A3DD,
            0xFF3CAC, 0xFF6AC1, 0xE0208F, 0x1A0524, 0x6A5A8E);

        // Approximation of DevExpress "Bezier": cool grey canvas, white surfaces, tight corners, steel-blue accent.
        public static readonly Tokens Bezier = T("Bezier", 2, 3,
            0xEDEFF2, 0xFFFFFF, 0xE1E5EA, 0xCDD3DA, 0x8A94A0, 0x2B3036, 0x5A636D,
            0x3872B8, 0x2F63A3, 0x265389, 0xFFFFFF, 0xAAB1B9);

        /// <summary>Short names for <see cref="FluentLight"/> and <see cref="FluentDark"/>.</summary>
        public static Tokens Light => FluentLight;
        public static Tokens Dark => FluentDark;

        /// <summary>Every built-in theme, in menu order. Saved theme names are looked up here.</summary>
        public static readonly Tokens[] All = { FluentLight, FluentDark, MaterialLight, MaterialDark, SolarizedLight, SolarizedDark, Nord, Dracula, NeonSynthwave, Bezier };

        /// <summary>True when the window background is dark (drives the title bar and scroll bar style).</summary>
        public static bool IsDark(Tokens t) => (t.Canvas.R * 0.299 + t.Canvas.G * 0.587 + t.Canvas.B * 0.114) < 128;

        public static Color Blend(Color a, Color b, float amountOfA) =>
            Color.FromArgb(
                (int)(a.R * amountOfA + b.R * (1 - amountOfA)),
                (int)(a.G * amountOfA + b.G * (1 - amountOfA)),
                (int)(a.B * amountOfA + b.B * (1 - amountOfA)));
    }
}
