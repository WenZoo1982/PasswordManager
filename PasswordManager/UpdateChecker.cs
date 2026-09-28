using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PasswordManager
{
    public class UpdateInfo
    {
        public bool HasUpdate { get; set; }

        public string CurrentVersion { get; set; } = string.Empty;

        public string LatestVersion { get; set; } = string.Empty;

        public string ReleaseUrl { get; set; } = string.Empty;
    }

    public static class UpdateChecker
    {
        private const string ApiUrl =
            "https://api.github.com/repos/WenZoo1982/PasswordManager/releases/latest";

        public static async Task<UpdateInfo?> CheckAsync(
            string currentVersion)
        {
            try
            {
                using HttpClient client = new HttpClient();

                client.Timeout =
                    TimeSpan.FromSeconds(5);

                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "PasswordManager");

                HttpResponseMessage response =
                    await client.GetAsync(ApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string json =
                    await response.Content.ReadAsStringAsync();

                using JsonDocument document =
                    JsonDocument.Parse(json);

                JsonElement root =
                    document.RootElement;

                string tagName =
                    root.GetProperty("tag_name").GetString()
                    ?? string.Empty;

                string htmlUrl =
                    root.GetProperty("html_url").GetString()
                    ?? string.Empty;

                string latestVersion =
                    tagName.TrimStart('v', 'V');

                if (!Version.TryParse(
                        currentVersion,
                        out Version? current))
                {
                    return null;
                }

                if (!Version.TryParse(
                        latestVersion,
                        out Version? latest))
                {
                    return null;
                }

                return new UpdateInfo
                {
                    HasUpdate = latest > current,
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    ReleaseUrl = htmlUrl
                };
            }
            catch
            {
                return null;
            }
        }
    }
}