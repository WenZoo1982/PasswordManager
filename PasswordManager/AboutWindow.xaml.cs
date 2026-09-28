using System;
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

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}