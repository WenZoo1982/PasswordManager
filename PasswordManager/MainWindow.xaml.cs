using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PasswordManager
{
    public partial class MainWindow : Window
    {
        private List<PasswordEntry> _allEntries =
            new List<PasswordEntry>();

        public MainWindow()
        {
            InitializeComponent();

            Title = $"本地密码管理器 v{GetVersion()}";

            LoadPasswordEntries();
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

        private async void Window_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                string currentVersion =
                    GetVersion();

                UpdateInfo? updateInfo =
                    await UpdateChecker.CheckAsync(
                        currentVersion);

                if (updateInfo == null)
                {
                    return;
                }

                if (!updateInfo.HasUpdate)
                {
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
                // 自动检查更新失败时不影响程序正常使用。
            }
        }

        private void LoadPasswordEntries()
        {
            AppConfig config = AppConfig.Load();

            if (string.IsNullOrWhiteSpace(config.VaultPath))
            {
                return;
            }

            VaultPathText.Text =
                $"密码库：{config.VaultPath}";

            ConfigPathText.Text =
                $"配置文件：{System.IO.Path.Combine(
                    System.AppContext.BaseDirectory,
                    "config.json")}";

            if (!File.Exists(config.VaultPath))
            {
                return;
            }

            Database database =
                new Database(config.VaultPath);

            _allEntries =
                database.GetPasswordEntries()
                    .OrderBy(
                        entry => entry.Title,
                        System.StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            PasswordList.ItemsSource = _allEntries;
        }

        private void SearchBox_TextChanged(
            object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            string keyword =
                SearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                PasswordList.ItemsSource = _allEntries;
                return;
            }

            var filteredEntries =
                _allEntries
                    .Where(entry =>
                        entry.Title.Contains(
                            keyword,
                            System.StringComparison.OrdinalIgnoreCase)
                        ||
                        entry.Username.Contains(
                            keyword,
                            System.StringComparison.OrdinalIgnoreCase))
                    .ToList();

            PasswordList.ItemsSource = filteredEntries;
        }

        private void SearchBox_GotFocus(
            object sender,
            RoutedEventArgs e)
        {
            SearchPlaceholder.Visibility =
                Visibility.Collapsed;
        }

        private void SearchBox_LostFocus(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchPlaceholder.Visibility =
                    Visibility.Visible;
            }
        }

        private void AddPasswordButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AddPasswordWindow addPasswordWindow =
                new AddPasswordWindow();

            if (addPasswordWindow.ShowDialog() == true)
            {
                LoadPasswordEntries();
            }
        }

        private void PasswordList_SelectionChanged(
            object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PasswordList.SelectedItem is not PasswordEntry entry)
            {
                return;
            }

            PasswordDetailWindow detailWindow =
                new PasswordDetailWindow(entry);

            if (detailWindow.ShowDialog() == true)
            {
                LoadPasswordEntries();
            }

            PasswordList.SelectedItem = null;
        }

        private void LockButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            VaultSession.EncryptionKey = null;

            LoginWindow loginWindow =
                new LoginWindow();

            loginWindow.Show();

            Close();
        }

        private void ChangePasswordButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ChangePasswordWindow window =
                new ChangePasswordWindow();

            window.ShowDialog();
        }

        private void AboutButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AboutWindow window =
                new AboutWindow();

            window.ShowDialog();
        }

        private void MigrateVaultButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MigrateVaultWindow window =
                new MigrateVaultWindow();

            if (window.ShowDialog() == true)
            {
                LoadPasswordEntries();
            }
        }
    }
}