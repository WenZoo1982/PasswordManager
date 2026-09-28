using System;
using System.Text;
using System.Windows;

namespace PasswordManager
{
    public partial class AddPasswordWindow : Window
    {
        private bool _passwordVisible = false;

        public AddPasswordWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void ShowPasswordButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_passwordVisible)
            {
                PasswordBox.Password = VisiblePasswordBox.Text;

                PasswordBox.Visibility = Visibility.Visible;
                VisiblePasswordBox.Visibility = Visibility.Collapsed;

                ShowPasswordButton.Content = "显示";

                _passwordVisible = false;
            }
            else
            {
                VisiblePasswordBox.Text = PasswordBox.Password;

                PasswordBox.Visibility = Visibility.Collapsed;
                VisiblePasswordBox.Visibility = Visibility.Visible;

                ShowPasswordButton.Content = "隐藏";

                _passwordVisible = true;
            }
        }

        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string title = TitleBox.Text.Trim();
            string url = UrlBox.Text.Trim();
            string username = UsernameBox.Text.Trim();
            string password = _passwordVisible
                ? VisiblePasswordBox.Text
                : PasswordBox.Password;
            string notes = NotesBox.Text;

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(
                    "请输入网站名称或应用名称。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                TitleBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show(
                    "请输入用户名。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                UsernameBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show(
                    "请输入密码。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                PasswordBox.Focus();
                return;
            }

            if (VaultSession.EncryptionKey == null)
            {
                MessageBox.Show(
                    "密码库尚未解锁。",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            try
            {
                byte[] passwordBytes =
                    Encoding.UTF8.GetBytes(password);

                byte[] encryptedPassword =
                    AesEncryption.Encrypt(
                        passwordBytes,
                        VaultSession.EncryptionKey,
                        out byte[] passwordNonce,
                        out byte[] passwordTag);

                byte[]? encryptedNotes = null;
                byte[]? notesNonce = null;
                byte[]? notesTag = null;

                if (!string.IsNullOrEmpty(notes))
                {
                    byte[] notesBytes =
                        Encoding.UTF8.GetBytes(notes);

                    encryptedNotes =
                        AesEncryption.Encrypt(
                            notesBytes,
                            VaultSession.EncryptionKey,
                            out byte[] tempNonce,
                            out byte[] tempTag);

                    notesNonce = tempNonce;
                    notesTag = tempTag;
                }

                AppConfig config = AppConfig.Load();

                if (string.IsNullOrWhiteSpace(config.VaultPath))
                {
                    MessageBox.Show(
                        "找不到密码库。",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                Database database =
                    new Database(config.VaultPath);

                database.AddPasswordEntry(
                    title,
                    url,
                    username,
                    encryptedPassword,
                    passwordNonce,
                    passwordTag,
                    encryptedNotes,
                    notesNonce,
                    notesTag);

                MessageBox.Show(
                    "密码已保存。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
            }
            catch
            {
                MessageBox.Show(
                    "密码保存失败。",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}