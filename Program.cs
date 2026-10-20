using CefSharp;
using CefSharp.WinForms;
using System.Windows.Forms;

namespace RedXBrowser;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var settings = new CefSettings
        {
            CachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RedXBrowser", "Cache")
        };

        if (!Cef.Initialize(settings))
        {
            MessageBox.Show("RedXBrowser could not start its bundled Chromium engine.",
                "RedXBrowser", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new BrowserForm());
        Cef.Shutdown();
    }
}
