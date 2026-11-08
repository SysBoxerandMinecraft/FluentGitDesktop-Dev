using System;
using System.Collections.Generic;

namespace FluentGit.Services;

public static partial class GitService
{
    public static List<GitStatusEntry> GetStatus(string repoPath, string gitExePath)
    {
        if (!IsGitRepository(repoPath)) return new List<GitStatusEntry>();

        try
        {
            var (code, output, error) = RunGit(gitExePath, new[] { "status", "--porcelain" }, repoPath);
            if (code != 0) return new List<GitStatusEntry>();

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var entries = new List<GitStatusEntry>();
            foreach (var line in lines)
            {
                if (line.Length < 4) continue;
                entries.Add(new GitStatusEntry
                {
                    X = line[0],
                    Y = line[1],
                    Path = line[3..].Trim('"') // 去除可能存在的引号
                });
            }
            return entries;
        }
        catch { return new List<GitStatusEntry>(); }
    }

    public static (int Modified, int Added, int Untracked) GetChangeSummary(string gitExePath, string repoPath)
    {
        var entries = GetStatus(repoPath, gitExePath);
        int modified = 0, added = 0, untracked = 0;

        foreach (var e in entries)
        {
            if (e.X == '?' || e.Y == '?') untracked++;
            else if (e.X == 'A') added++;
            else modified++;
        }
        return (modified, added, untracked);
    }

    // ========== 新增：暂存单个文件 ==========
    public static (bool Success, string Message) StageFile(string gitExePath, string repoPath, string filePath)
    {
        try
        {
            var (code, _, error) = RunGit(gitExePath, new[] { "add", filePath }, repoPath);
            return code == 0 ? (true, "暂存成功") : (false, error.Trim());
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    // ========== 新增：放弃单文件的修改 ==========
    public static (bool Success, string Message) DiscardChanges(string gitExePath, string repoPath, string filePath)
    {
        try
        {
            // 使用 restore 命令，兼容性更好
            var (code, _, error) = RunGit(gitExePath, new[] { "restore", filePath }, repoPath);
            return code == 0 ? (true, "已放弃修改") : (false, error.Trim());
        }
        catch (Exception ex) { return (false, ex.Message); }
    }
}