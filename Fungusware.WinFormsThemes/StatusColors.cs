using System.Drawing;

namespace Fungusware.WinFormsThemes
{
    /// <summary>
    /// Colours for job outcomes (success, warning, error, fatal) that stay readable on the active theme.
    /// Light themes keep the traditional colours; dark themes get lighter tints of the same hues.
    /// </summary>
    public static class StatusColors
    {
        public static bool IsDark => ModernTheme.IsDark(ThemeApplier.Current);

        public static Color Success => IsDark ? Color.FromArgb(0x5F, 0xD3, 0x8A) : Color.DarkGreen;
        public static Color Warning => IsDark ? Color.FromArgb(0xFF, 0xB4, 0x54) : Color.Orange;
        public static Color Error => IsDark ? Color.FromArgb(0xFF, 0x7A, 0x8A) : Color.Crimson;
        public static Color Fatal => IsDark ? Color.FromArgb(0xFF, 0x5C, 0x5C) : Color.Red;

        /// <summary>Row background for a job that is running right now.</summary>
        public static Color RunningRow => IsDark ? ModernTheme.Blend(ThemeApplier.Current.Accent, ThemeApplier.Current.Surface, 0.18f) : Color.AliceBlue;

        // Chip colours: a tinted background with a strong foreground of the same hue.
        public static (Color Back, Color Fore) Chip(string kind)
        {
            if (IsDark)
                return kind switch
                {
                    "info" => (Color.FromArgb(0x1D, 0x30, 0x4D), Color.FromArgb(0x8C, 0xB8, 0xFF)),
                    "success" => (Color.FromArgb(0x1B, 0x3A, 0x2A), Color.FromArgb(0x6F, 0xE0, 0x9A)),
                    "warning" => (Color.FromArgb(0x45, 0x34, 0x16), Color.FromArgb(0xFF, 0xC2, 0x70)),
                    "error" => (Color.FromArgb(0x4A, 0x1F, 0x24), Color.FromArgb(0xFF, 0x8F, 0x9B)),
                    _ => (Color.FromArgb(0x2A, 0x2F, 0x3A), Color.FromArgb(0xB5, 0xBD, 0xCC)),
                };
            return kind switch
            {
                "info" => (Color.FromArgb(225, 239, 255), Color.FromArgb(16, 76, 140)),
                "success" => (Color.FromArgb(227, 244, 234), Color.FromArgb(20, 110, 58)),
                "warning" => (Color.FromArgb(255, 243, 224), Color.FromArgb(140, 79, 0)),
                "error" => (Color.FromArgb(254, 233, 232), Color.FromArgb(163, 26, 25)),
                _ => (Color.FromArgb(238, 240, 244), Color.FromArgb(87, 96, 110)),
            };
        }
    }
}
