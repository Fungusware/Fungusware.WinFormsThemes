// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
namespace Fungusware.WinFormsThemes;

/// <summary>
/// A control family that follows the theme (Krypton, a third-party suite). The core theme drives it: it is told when the
/// theme is installed and when it changes, and it styles the controls it owns. Register with
/// <see cref="ThemeApplier.RegisterAdapter"/>.
/// </summary>
public abstract class ThemeAdapter
{
    /// <summary>The engine was installed with this theme (once, before the first form is themed).</summary>
    public virtual void Install(Tokens t) { }

    /// <summary>The theme was switched at run time, before the open forms are restyled.</summary>
    public virtual void ThemeChanged(Tokens t) { }

    /// <summary>True when this adapter styles <paramref name="c"/>; the engine's own styling is skipped for it.</summary>
    public virtual bool Owns(Control c) => false;

    /// <summary>Styles a control this adapter owns.</summary>
    public virtual void Style(Control c, Tokens t) { }

    /// <summary>Whether the controls inside an owned control are walked by the engine.</summary>
    public virtual bool WalkInto(Control c) => true;
}
