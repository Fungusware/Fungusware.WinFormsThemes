// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
namespace KryptonModern;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Form f = Environment.GetEnvironmentVariable("KM_FORM") switch { "2" => new ControlsForm(), "3" => new GalleryForm(), _ => new DemoForm() };
        Application.Run(f);
    }
}
