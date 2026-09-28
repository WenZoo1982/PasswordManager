using System.Windows;

namespace PasswordManager
{
    public partial class CreatePasswordWindow : Window
    {
        private readonly string _vaultPath;

        public CreatePasswordWindow(string vaultPath)
        {
            InitializeComponent();

            _vaultPath = vaultPath;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "请输入主密码",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                PasswordBox.Focus();
                return;
            }

            if (password.Length < 8)
            {
                MessageBox.Show(
                    "主密码至少需要 8 个字符",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                PasswordBox.Focus();
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show(
                    "两次输入的主密码不一致",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                ConfirmPasswordBox.Clear();
                ConfirmPasswordBox.Focus();
                return;
            }

            try
            {
                Database database = new Database(_vaultPath);

                database.Initialize();

                byte[] salt = PasswordHasher.CreateSalt();

                byte[] passwordHash =
                    PasswordHasher.HashPassword(password, salt);

                database.SavePasswordHash(
                    passwordHash,
                    salt);

                byte[]? encryptionSalt =
                    database.GetEncryptionSalt();

                if (encryptionSalt == null)
                {
                    MessageBox.Show(
                        "无法读取加密 Salt",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                VaultSession.EncryptionKey =
                    PasswordKeyDerivation.DeriveKey(
                        password,
                        encryptionSalt);

                AppConfig config = new AppConfig
                {
                    VaultPath = _vaultPath
                };

                config.Save();

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                Close();
            }
            catch
            {
                MessageBox.Show(
                    "密码库初始化失败",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}