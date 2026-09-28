using System.Collections.Generic;
using System.Security.Cryptography;
using System.Windows;

namespace PasswordManager
{
    public partial class ChangePasswordWindow : Window
    {
        public ChangePasswordWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void ChangeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string currentPassword =
                CurrentPasswordBox.Password;

            string newPassword =
                NewPasswordBox.Password;

            string confirmPassword =
                ConfirmPasswordBox.Password;

            // 检查当前密码
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                ErrorText.Text = "请输入当前主密码。";
                CurrentPasswordBox.Focus();
                return;
            }

            // 检查新密码
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ErrorText.Text = "请输入新主密码。";
                NewPasswordBox.Focus();
                return;
            }

            if (newPassword.Length < 8)
            {
                ErrorText.Text =
                    "新主密码至少需要 8 个字符。";

                NewPasswordBox.Focus();
                return;
            }

            // 检查确认密码
            if (newPassword != confirmPassword)
            {
                ErrorText.Text =
                    "两次输入的新主密码不一致。";

                ConfirmPasswordBox.Clear();
                ConfirmPasswordBox.Focus();
                return;
            }

            // 不允许新旧密码相同
            if (currentPassword == newPassword)
            {
                ErrorText.Text =
                    "新主密码不能与当前主密码相同。";

                NewPasswordBox.Clear();
                ConfirmPasswordBox.Clear();
                NewPasswordBox.Focus();
                return;
            }

            // 必须处于已解锁状态
            if (VaultSession.EncryptionKey == null)
            {
                ErrorText.Text =
                    "密码库尚未解锁。";

                return;
            }

            try
            {
                AppConfig config =
                    AppConfig.Load();

                if (string.IsNullOrWhiteSpace(
                        config.VaultPath))
                {
                    ErrorText.Text =
                        "找不到密码库。";

                    return;
                }

                Database database =
                    new Database(config.VaultPath);

                // 读取当前主密码信息
                var passwordInfo =
                    database.GetPasswordInfo();

                if (passwordInfo == null)
                {
                    ErrorText.Text =
                        "密码库尚未初始化。";

                    return;
                }

                // 验证当前主密码
                bool valid =
                    PasswordHasher.VerifyPassword(
                        currentPassword,
                        passwordInfo.Value.PasswordSalt,
                        passwordInfo.Value.PasswordHash);

                if (!valid)
                {
                    ErrorText.Text =
                        "当前主密码错误。";

                    CurrentPasswordBox.Clear();
                    CurrentPasswordBox.Focus();
                    return;
                }

                // 读取旧的 Encryption Salt
                byte[]? oldEncryptionSalt =
                    database.GetEncryptionSalt();

                if (oldEncryptionSalt == null)
                {
                    ErrorText.Text =
                        "无法读取密码库加密 Salt。";

                    return;
                }

                // 根据旧主密码得到旧加密密钥
                byte[] oldEncryptionKey =
                    PasswordKeyDerivation.DeriveKey(
                        currentPassword,
                        oldEncryptionSalt);

                // 读取所有密码记录
                List<PasswordEntry> entries =
                    database.GetPasswordEntries();

                // 创建新的密码 Salt
                byte[] newPasswordSalt =
                    PasswordHasher.CreateSalt();

                // 创建新的主密码 Hash
                byte[] newPasswordHash =
                    PasswordHasher.HashPassword(
                        newPassword,
                        newPasswordSalt);

                // 创建新的 Encryption Salt
                byte[] newEncryptionSalt =
                    PasswordHasher.CreateSalt();

                // 根据新主密码得到新的加密密钥
                byte[] newEncryptionKey =
                    PasswordKeyDerivation.DeriveKey(
                        newPassword,
                        newEncryptionSalt);

                // 使用新密钥重新加密所有记录
                List<PasswordEntry> reencryptedEntries =
                    ReencryptEntries(
                        entries,
                        oldEncryptionKey,
                        newEncryptionKey);

                // 一次性更新整个密码库
                database.ChangeVaultSecurity(
                    newPasswordHash,
                    newPasswordSalt,
                    newEncryptionSalt,
                    reencryptedEntries);

                // 更新当前会话密钥
                VaultSession.EncryptionKey =
                    newEncryptionKey;

                MessageBox.Show(
                    "主密码修改成功。",
                    "修改成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
            }
            catch (CryptographicException)
            {
                ErrorText.Text =
                    "密码库数据解密失败，主密码没有修改。";
            }
            catch
            {
                ErrorText.Text =
                    "修改主密码失败，密码库没有修改。";
            }
        }

        private static List<PasswordEntry> ReencryptEntries(
            List<PasswordEntry> entries,
            byte[] oldEncryptionKey,
            byte[] newEncryptionKey)
        {
            List<PasswordEntry> result =
                new List<PasswordEntry>();

            foreach (PasswordEntry entry in entries)
            {
                // 解密旧密码
                byte[] passwordBytes =
                    AesEncryption.Decrypt(
                        entry.Password,
                        oldEncryptionKey,
                        entry.PasswordNonce,
                        entry.PasswordTag);

                // 使用新密钥重新加密密码
                byte[] encryptedPassword =
                    AesEncryption.Encrypt(
                        passwordBytes,
                        newEncryptionKey,
                        out byte[] passwordNonce,
                        out byte[] passwordTag);

                byte[]? encryptedNotes = null;
                byte[]? notesNonce = null;
                byte[]? notesTag = null;

                // 如果存在备注，也重新加密
                if (entry.Notes != null &&
                    entry.NotesNonce != null &&
                    entry.NotesTag != null)
                {
                    byte[] notesBytes =
                        AesEncryption.Decrypt(
                            entry.Notes,
                            oldEncryptionKey,
                            entry.NotesNonce,
                            entry.NotesTag);

                    encryptedNotes =
                        AesEncryption.Encrypt(
                            notesBytes,
                            newEncryptionKey,
                            out byte[] tempNotesNonce,
                            out byte[] tempNotesTag);

                    notesNonce = tempNotesNonce;
                    notesTag = tempNotesTag;
                }

                result.Add(
                    new PasswordEntry
                    {
                        Id = entry.Id,
                        Title = entry.Title,
                        Url = entry.Url,
                        Username = entry.Username,

                        Password = encryptedPassword,
                        PasswordNonce = passwordNonce,
                        PasswordTag = passwordTag,

                        Notes = encryptedNotes,
                        NotesNonce = notesNonce,
                        NotesTag = notesTag
                    });
            }

            return result;
        }
    }
}