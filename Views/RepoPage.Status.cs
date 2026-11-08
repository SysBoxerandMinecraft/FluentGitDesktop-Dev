using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentGit.Services;

namespace FluentGit.Views;

public sealed partial class RepoPage
{
    private async void RefreshChangeStatus()
    {
        var repoPath = AppState.CurrentRepoPath;
        if (string.IsNullOrEmpty(repoPath) || !Directory.Exists(repoPath))
        {
            ChangeStatusText.Text = Loc.GetString("RepoStatusUnopened");
            StatusListView.ItemsSource = null;
            return;
        }

        var settings = SettingsService.Load();
        string? gitPath = settings.GitPath;
        if (string.IsNullOrEmpty(gitPath) || !File.Exists(gitPath))
        {
            ChangeStatusText.Text = Loc.GetString("RepoErrGitPath");
            StatusListView.ItemsSource = null;
            return;
        }

        try
        {
            ChangeStatusText.Text = Loc.GetString("RepoStatusReading");

            var entries = await Task.Run(() => GitService.GetStatus(repoPath, gitPath));

            if (entries.Count == 0)
            {
                ChangeStatusText.Text = Loc.GetString("RepoClean");
                StatusListView.ItemsSource = null;
            }
            else
            {
                ChangeStatusText.Text = $"当前共有 {entries.Count} 个变更文件";
                StatusListView.ItemsSource = entries;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error(TAG, $"RefreshChangeStatus 异常: {ex.Message}");
            ChangeStatusText.Text = Loc.GetString("RepoStatusReadFailed");
        }
    }

    private void UpdateCloneUI()
    {
        var repoPath = AppState.CurrentRepoPath;
        bool isRepoOpen = !string.IsNullOrEmpty(repoPath) && Directory.Exists(repoPath);
        if (isRepoOpen)
        {
            RepoOpsPanel.Visibility = Visibility.Visible;
            CommitPanel.Visibility = Visibility.Visible;
            RefreshChangeStatus();
        }
        else
        {
            RepoOpsPanel.Visibility = Visibility.Collapsed;
            CommitPanel.Visibility = Visibility.Collapsed;
        }
    }
}