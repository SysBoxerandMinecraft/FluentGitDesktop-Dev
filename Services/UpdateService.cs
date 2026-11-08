using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentGit.Services;

namespace FluentGit.Services
{
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;
    }

    public static class UpdateService
    {
        // 获取程序中硬编码的版本号
        public static string CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.1";

        // 用户的仓库 API 地址
        private const string ApiUrl = "https://api.github.com/repos/SysBoxerandMinecraft/FluentGitDesktop-Dev/releases";

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        static UpdateService()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FluentGit-UpdateChecker");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
        }

        /// <summary>
        /// 检查是否有新版本。返回 null 表示无更新或检查失败。
        /// </summary>
        public static async Task<GitHubRelease?> CheckForUpdateAsync()
        {
            try
            {
                var releases = await _httpClient.GetFromJsonAsync<GitHubRelease[]>(ApiUrl);
                if (releases == null || releases.Length == 0) return null;

                GitHubRelease? latestRelease = null;
                Version? latestVersion = null;
                Version? currentVersion = Version.Parse(CurrentVersion);

                foreach (var release in releases)
                {
                    if (string.IsNullOrEmpty(release.TagName)) continue;

                    // 去除 tag 里的 'v'，比如 v0.0.2 -> 0.0.2
                    var versionString = release.TagName.TrimStart('v', 'V');
                    if (!Version.TryParse(versionString, out var releaseVersion)) continue;

                    if (releaseVersion > currentVersion)
                    {
                        if (latestVersion == null || releaseVersion > latestVersion)
                        {
                            latestVersion = releaseVersion;
                            latestRelease = release;
                        }
                    }
                }

                return latestRelease;
            }
            catch (Exception ex)
            {
                AppLogger.Error("UpdateService", $"检查更新失败: {ex.Message}");
                return null; 
            }
        }
    }
}