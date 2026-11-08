using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic; // 新增
using System.Linq; // 新增

namespace FluentGit.Services;

public class AppSettings
{
    public string? GitPath { get; set; }
    public string? GitHash { get; set; }
    public string? AppTheme { get; set; }
    public bool FilterBuildArtifacts { get; set; } = true;
    public string? BackdropType { get; set; }
    public string? Language { get; set; } = "zh-CN"; 
    
    // 新增：最近打开的仓库列表
    public List<string> RecentRepos { get; set; } = new();
}

public static class SettingsService
{
    private const string TAG = "SettingsService";

    private static readonly string _settingsPath = Path.Combine(
        AppContext.BaseDirectory, "Setting", "settings.json"
    );

    static SettingsService()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        catch (Exception ex)
        {
            AppLogger.Error(TAG, $"创建设置目录失败: {ex.Message}");
        }
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                string json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    if (string.IsNullOrEmpty(settings.Language))
                        settings.Language = "zh-CN";
                    // 清理已被用户删除的无效路径
                    settings.RecentRepos = settings.RecentRepos.Where(Directory.Exists).ToList();
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error(TAG, $"读取设置失败: {ex.Message}");
        }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

        for (int i = 0; i < 3; i++)
        {
            try
            {
                var dir = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(_settingsPath, json);
                return; 
            }
            catch (Exception ex)
            {
                if (i == 2) AppLogger.Error(TAG, $"保存设置彻底失败: {ex.Message}");
                Thread.Sleep(100); 
            }
        }
    }

    // 新增：添加最近打开的仓库
    public static void AddRecentRepo(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
        
        var settings = Load();
        // 如果已存在，先移除
        settings.RecentRepos.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        // 插到最前面
        settings.RecentRepos.Insert(0, path);
        
        // 最多保留 8 个
        if (settings.RecentRepos.Count > 8)
            settings.RecentRepos = settings.RecentRepos.Take(8).ToList();
            
        Save(settings);
    }
}