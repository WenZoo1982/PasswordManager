using System.Windows;

namespace PasswordManager
{
    public partial class LoginWindow : Window
    {
        private readonly string _vaultPath;

        public LoginWindow()
        {
            InitializeComponent();

            AppConfig config = AppConfig.Load();

            _vaultPath = config.VaultPath ?? string.Empty;
        }

        private void UnlockButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                ErrorText.Text = "请输入主密码";
                PasswordBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_vaultPath))
            {
                ErrorText.Text = "找不到密码库";
                return;
            }

            try
            {
                Database database = new Database(_vaultPath);

                var passwordInfo = database.GetPasswordInfo();

                if (passwordInfo == null)
                {
                    ErrorText.Text = "密码库尚未初始化";
                    return;
                }

                bool valid = PasswordHasher.VerifyPassword(
                    PasswordBox.Password,
                    passwordInfo.Value.PasswordSalt,
                    passwordInfo.Value.PasswordHash);

                if (!valid)
                {
                    ErrorText.Text = "主密码错误";
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                    return;
                }

                byte[]? encryptionSalt =
                    database.GetEncryptionSalt();

                if (encryptionSalt == null)
                {
                    ErrorText.Text = "无法读取加密 Salt";
                    return;
                }

                byte[] encryptionKey =
                    PasswordKeyDerivation.DeriveKey(
                        PasswordBox.Password,
                        encryptionSalt);

                VaultSession.EncryptionKey = encryptionKey;

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                Close();
            }
            catch
            {
                ErrorText.Text = "密码库读取失败";
            }
        }

        private void PasswordBox_KeyDown(
            object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                UnlockButton_Click(sender, e);
            }
        }
    }
}