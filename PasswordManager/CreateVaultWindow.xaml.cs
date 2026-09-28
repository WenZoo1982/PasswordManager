using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace PasswordManager
{
    public partial class CreateVaultWindow : Window
    {
        public CreateVaultWindow()
        {
            InitializeComponent();
        }

        private void ChooseFileButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "选择密码库保存位置",
                Filter = "密码库文件 (*.dat)|*.dat|所有文件 (*.*)|*.*",
                FileName = "storage.dat"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePathBox.Text = dialog.FileName;
            }
        }

        private void CreateButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FilePathBox.Text))
            {
                ErrorText.Text = "请选择密码库保存位置";
                return;
            }

            string filePath = FilePathBox.Text;

            try
            {
                if (File.Exists(filePath))
                {
                    MessageBoxResult result =
                        MessageBox.Show(
                            "这个文件已经存在。\n\n创建新的密码库会覆盖现有文件，是否继续？",
                            "确认创建",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    File.Delete(filePath);
                }

                string? directory =
                    Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (File.Create(filePath))
                {
                }

                CreatePasswordWindow passwordWindow =
                    new CreatePasswordWindow(filePath);

                passwordWindow.Show();

                Close();
            }
            catch
            {
                ErrorText.Text =
                    "密码库创建失败，请检查保存位置";
            }
        }

        private void OpenExistingButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "选择已有密码库",
                Filter = "密码库文件 (*.dat)|*.dat|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string filePath = dialog.FileName;

            try
            {
                Database database =
                    new Database(filePath);

                var passwordInfo =
                    database.GetPasswordInfo();

                if (passwordInfo == null)
                {
                    ErrorText.Text =
                        "这个文件不是有效的密码库，或者密码库尚未初始化。";

                    return;
                }

                AppConfig config =
                    AppConfig.Load();

                config.VaultPath = filePath;
                config.Save();

                LoginWindow loginWindow =
                    new LoginWindow();

                loginWindow.Show();

                Close();
            }
            catch
            {
                ErrorText.Text =
                    "无法读取这个密码库，请确认文件没有损坏。";
            }
        }
    }
}
