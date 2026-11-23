// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
namespace KryptonModern;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        // KM_FORM=1|2|3 opens a window directly (handy for scripted runs); otherwise show the launcher.
        Form f = Environment.GetEnvironmentVariable("KM_FORM") switch { "1" => new DemoForm(), "2" => new ControlsForm(), "3" => new GalleryForm(), "4" => new KryptonModern.VsClone.VsCloneForm(), _ => new LauncherForm() };
        Application.Run(f);
    }
}
