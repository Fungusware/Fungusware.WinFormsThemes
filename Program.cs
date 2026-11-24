// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
namespace Fungusware.WinFormsThemes.Demo;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        ObjectListViewTheme.Register(); // the ObjectListView adapter: lists follow the theme
        KryptonTheme.Register(); // the Krypton adapter: Krypton controls follow the Fungusware.WinFormsThemes theme
        // KM_FORM=1|2|3 opens a window directly (handy for scripted runs); otherwise show the launcher.
        Form f = Environment.GetEnvironmentVariable("KM_FORM") switch { "1" => new DemoForm(), "2" => new ControlsForm(), "3" => new GalleryForm(), "4" => new Fungusware.WinFormsThemes.Demo.VsClone.VsCloneForm(), "5" => new ListForm(), _ => new LauncherForm() };
        Application.Run(f);
    }
}
