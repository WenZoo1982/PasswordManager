using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace PasswordManager
{
    public partial class MigrateVaultWindow : Window
    {
        private readonly string _currentVaultPath;

        public MigrateVaultWindow()
        {
            InitializeComponent();

            AppConfig config =
                AppConfig.Load();

            _currentVaultPath =
                config.VaultPath ?? string.Empty;

            CurrentPathBox.Text =
                string.IsNullOrWhiteSpace(_currentVaultPath)
                    ? "未找到当前密码库"
                    : _currentVaultPath;
        }

        private void ChooseFileButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SaveFileDialog dialog =
                new SaveFileDialog
                {
                    Title = "选择新的密码库保存位置",
                    Filter =
                        "密码库文件 (*.dat)|*.dat|所有文件 (*.*)|*.*",
                    FileName = "storage.dat"
                };

            if (dialog.ShowDialog() == true)
            {
                NewPathBox.Text =
                    dialog.FileName;

                StatusText.Text = string.Empty;
            }
        }

        private void MigrateButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            StatusText.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(_currentVaultPath) ||
                !File.Exists(_currentVaultPath))
            {
                StatusText.Text =
                    "找不到当前密码库文件。";

                return;
            }

            if (string.IsNullOrWhiteSpace(NewPathBox.Text) ||
                NewPathBox.Text == "请选择路径")
            {
                StatusText.Text =
                    "请选择新的密码库保存位置。";

                return;
            }

            string newPath =
                NewPathBox.Text.Trim();

            try
            {
                string currentFullPath =
                    Path.GetFullPath(_currentVaultPath);

                string newFullPath =
                    Path.GetFullPath(newPath);

                if (string.Equals(
                    currentFullPath,
                    newFullPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    StatusText.Text =
                        "新的密码库位置不能与当前密码库相同。";

                    return;
                }

                if (File.Exists(newFullPath))
                {
                    MessageBoxResult result =
                        MessageBox.Show(
                            "目标位置已经存在文件。\n\n" +
                            "继续迁移将覆盖这个文件，是否继续？",
                            "确认覆盖",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                string? directory =
                    Path.GetDirectoryName(newFullPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(
                    currentFullPath,
                    newFullPath,
                    true);

                if (!File.Exists(newFullPath))
                {
                    StatusText.Text =
                        "新密码库文件创建失败。";

                    return;
                }

                Database database =
                    new Database(newFullPath);

                var passwordInfo =
                    database.GetPasswordInfo();

                if (passwordInfo == null)
                {
                    File.Delete(newFullPath);

                    StatusText.Text =
                        "迁移失败：新密码库验证失败。";

                    return;
                }

                byte[]? encryptionSalt =
                    database.GetEncryptionSalt();

                if (encryptionSalt == null)
                {
                    File.Delete(newFullPath);

                    StatusText.Text =
                        "迁移失败：无法读取加密信息。";

                    return;
                }

                AppConfig config =
                    AppConfig.Load();

                config.VaultPath =
                    newFullPath;

                config.Save();

                MessageBox.Show(
                    "密码库迁移成功。\n\n" +
                    "新的密码库位置：\n" +
                    newFullPath +
                    "\n\n" +
                    "原密码库文件仍然保留。",
                    "迁移成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch
            {
                StatusText.Text =
                    "密码库迁移失败，请检查新的保存位置。";
            }
        }

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}