using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace PasswordManager
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            VersionText.Text =
                $"版本 {GetVersion()}";

            AppConfig config =
                AppConfig.Load();

            VaultPathText.Text =
                string.IsNullOrWhiteSpace(config.VaultPath)
                    ? "未设置"
                    : config.VaultPath;

            ConfigPathText.Text =
                System.IO.Path.Combine(
                    AppContext.BaseDirectory,
                    "config.json");
        }

        private string GetVersion()
        {
            return System.Reflection.Assembly
                .GetExecutingAssembly()
                .GetName()
                .Version?
                .ToString(3)
                ?? "未知版本";
        }

        private async void CheckUpdateButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string currentVersion =
                GetVersion();

            CheckUpdateButton.IsEnabled = false;
            CheckUpdateButton.Content = "检查中...";

            try
            {
                UpdateInfo? updateInfo =
                    await UpdateChecker.CheckAsync(
                        currentVersion);

                if (updateInfo == null)
                {
                    MessageBox.Show(
                        this,
                        "检查更新失败，请稍后重试。",
                        "检查更新",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                if (!updateInfo.HasUpdate)
                {
                    MessageBox.Show(
                        this,
                        $"当前版本：v{updateInfo.CurrentVersion}\n\n" +
                        "当前已经是最新版本。",
                        "检查更新",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                MessageBoxResult result =
                    MessageBox.Show(
                        this,
                        $"发现新版本：v{updateInfo.LatestVersion}\n\n" +
                        $"当前版本：v{updateInfo.CurrentVersion}\n\n" +
                        "是否打开 GitHub 发布页面？",
                        "发现新版本",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = updateInfo.ReleaseUrl,
                            UseShellExecute = true
                        });
                }
            }
            catch
            {
                MessageBox.Show(
                    this,
                    "检查更新失败，请稍后重试。",
                    "检查更新",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            finally
            {
                CheckUpdateButton.IsEnabled = true;
                CheckUpdateButton.Content = "检查更新";
            }
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}