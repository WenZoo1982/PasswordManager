using System;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace PasswordManager
{
    public partial class PasswordDetailWindow : Window
    {
        private readonly PasswordEntry _entry;

        private bool _passwordVisible = false;

        public PasswordDetailWindow(PasswordEntry entry)
        {
            InitializeComponent();

            _entry = entry;

            LoadEntry();
        }

        private void LoadEntry()
        {
            if (VaultSession.EncryptionKey == null)
            {
                MessageBox.Show(
                    "密码库尚未解锁。",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Close();
                return;
            }

            try
            {
                TitleBox.Text = _entry.Title;
                UrlBox.Text = _entry.Url;
                UsernameBox.Text = _entry.Username;

                byte[] passwordBytes =
                    AesEncryption.Decrypt(
                        _entry.Password,
                        VaultSession.EncryptionKey,
                        _entry.PasswordNonce,
                        _entry.PasswordTag);

                string password =
                    Encoding.UTF8.GetString(passwordBytes);

                PasswordBox.Password = password;
                VisiblePasswordBox.Text = password;

                if (_entry.Notes != null &&
                    _entry.NotesNonce != null &&
                    _entry.NotesTag != null)
                {
                    byte[] notesBytes =
                        AesEncryption.Decrypt(
                            _entry.Notes,
                            VaultSession.EncryptionKey,
                            _entry.NotesNonce,
                            _entry.NotesTag);

                    NotesBox.Text =
                        Encoding.UTF8.GetString(notesBytes);
                }
            }
            catch
            {
                MessageBox.Show(
                    "密码信息读取失败。",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Close();
            }
        }

        private void EditButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            TitleText.Text = "编辑密码";

            TitleBox.IsReadOnly = false;
            UrlBox.IsReadOnly = false;
            UsernameBox.IsReadOnly = false;
            NotesBox.IsReadOnly = false;

            PasswordBox.IsHitTestVisible = true;

            OpenUrlButton.IsEnabled = false;
            CopyUsernameButton.IsEnabled = false;
            CopyPasswordButton.IsEnabled = false;
            ShowPasswordButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;

            EditButton.Content = "保存";

            EditButton.Click -= EditButton_Click;
            EditButton.Click += SaveEditButton_Click;

            TitleBox.Focus();
        }

        private void SaveEditButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string title = TitleBox.Text.Trim();
            string url = UrlBox.Text.Trim();
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;
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

                database.UpdatePasswordEntry(
                    _entry.Id,
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

        private void DeleteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBoxResult result =
                MessageBox.Show(
                    $"确定要删除“{_entry.Title}”吗？\n\n删除后无法恢复。",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
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

            try
            {
                Database database =
                    new Database(config.VaultPath);

                database.DeletePasswordEntry(_entry.Id);

                MessageBox.Show(
                    "密码已删除。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
            }
            catch
            {
                MessageBox.Show(
                    "密码删除失败。",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenUrlButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string url = UrlBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show(
                    "没有填写网址。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            try
            {
                if (!url.StartsWith(
                        "http://",
                        StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith(
                        "https://",
                        StringComparison.OrdinalIgnoreCase))
                {
                    url = "https://" + url;
                }

                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
            }
            catch
            {
                MessageBox.Show(
                    "无法打开网址。",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CopyUsernameButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Clipboard.SetText(UsernameBox.Text);

            MessageBox.Show(
                "用户名已复制。",
                "提示",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CopyPasswordButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string password;

            if (_passwordVisible)
            {
                password = VisiblePasswordBox.Text;
            }
            else
            {
                password = PasswordBox.Password;
            }

            Clipboard.SetText(password);

            MessageBox.Show(
                "密码已复制。",
                "提示",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ShowPasswordButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_passwordVisible)
            {
                PasswordBox.Password =
                    VisiblePasswordBox.Text;

                PasswordBox.Visibility =
                    Visibility.Visible;

                VisiblePasswordBox.Visibility =
                    Visibility.Collapsed;

                ShowPasswordButton.Content = "显示";

                _passwordVisible = false;
            }
            else
            {
                VisiblePasswordBox.Text =
                    PasswordBox.Password;

                PasswordBox.Visibility =
                    Visibility.Collapsed;

                VisiblePasswordBox.Visibility =
                    Visibility.Visible;

                ShowPasswordButton.Content = "隐藏";

                _passwordVisible = true;
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