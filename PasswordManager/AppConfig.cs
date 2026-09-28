using System;
using System.IO;
using System.Text.Json;

namespace PasswordManager
{
    public class AppConfig
    {
        public string? VaultPath { get; set; }

        private static string ConfigFile =>
            Path.Combine(
                AppContext.BaseDirectory,
                "config.json");

        public static AppConfig Load()
        {
            if (!File.Exists(ConfigFile))
            {
                return new AppConfig();
            }

            try
            {
                string json = File.ReadAllText(ConfigFile);

                return JsonSerializer.Deserialize<AppConfig>(json)
                       ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(
                this,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(ConfigFile, json);
        }
    }
}