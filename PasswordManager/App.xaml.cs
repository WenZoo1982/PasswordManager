using System.IO;
using System.Windows;

namespace PasswordManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppConfig config = AppConfig.Load();

            if (!string.IsNullOrWhiteSpace(config.VaultPath) &&
                File.Exists(config.VaultPath))
            {
                LoginWindow loginWindow =
                    new LoginWindow();

                loginWindow.Show();
            }
            else
            {
                CreateVaultWindow createVaultWindow =
                    new CreateVaultWindow();

                createVaultWindow.Show();
            }
        }
    }
}

