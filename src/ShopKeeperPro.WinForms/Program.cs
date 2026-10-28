using System.Runtime.InteropServices;
using System.Text.Json;
using ShopKeeperPro.WinForms.UI;

namespace ShopKeeperPro.WinForms;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args is ["--self-test", var path])
        {
            try
            {
                using var form = new MainForm();
                var checks = form.VerifyInteraction();
                File.WriteAllText(path, JsonSerializer.Serialize(new
                {
                    passed = true,
                    architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    formTitle = form.Text,
                    width = form.Width,
                    height = form.Height,
                    checks
                }, new JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }
            catch (Exception ex)
            {
                File.WriteAllText(path, JsonSerializer.Serialize(new { passed = false, error = ex.ToString() }));
                return 1;
            }
        }

        // Multi-session Cashier Authentication Loop
        while (true)
        {
            using var login = new LoginDialog();
            var loginResult = login.ShowDialog();

            if (loginResult != DialogResult.OK || login.AuthenticatedUser == null)
            {
                // User clicked [ESC] EXIT or closed the login window -> terminate application cleanly
                break;
            }

            using var mainForm = new MainForm(login.AuthenticatedUser);
            Application.Run(mainForm);

            if (!mainForm.WantsSignOut)
            {
                // User closed the main window directly via [X] -> exit
                break;
            }
            // User signed out -> loop restarts and presents LoginDialog for next cashier
        }

        return 0;
    }
}