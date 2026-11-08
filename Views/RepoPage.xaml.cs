using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentGit.Services;

namespace FluentGit.Views;

public sealed partial class RepoPage : Page
{
    private const string TAG = "RepoPage";
    private CancellationTokenSource? _infoBarCts;
    private bool _isInfoBarAnimating = false;
    private bool _isBusy = false;

    public RepoPage()
    {
        InitializeComponent();
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CheckGitAvailability();
        LoadRecentRepos();
        var saved = AppState.CurrentRepoPath;
        if (!string.IsNullOrEmpty(saved) && Directory.Exists(saved))
        {
            RepoActionsPanel.Visibility = Visibility.Collapsed;
            RepoNameDisplay.Text = Path.GetFileName(saved);
            RepoNameDisplay.Visibility = Visibility.Visible;
        }
        UpdateCloneUI();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var saved = AppState.CurrentRepoPath;
        if (!string.IsNullOrEmpty(saved) && Directory.Exists(saved))
        {
            RepoActionsPanel.Visibility = Visibility.Collapsed;
            RepoNameDisplay.Text = Path.GetFileName(saved);
            RepoNameDisplay.Visibility = Visibility.Visible;
        }
        UpdateCloneUI();
        LoadRecentRepos();
    }

    private void OnNavigateToClone(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(ClonePage));

    private void CheckGitAvailability()
    {
        try
        {
            var settings = SettingsService.Load();
            string? gitPath = settings.GitPath;
            if (string.IsNullOrEmpty(gitPath))
            {
                gitPath = GitPathHelper.FindGitPath();
                if (!string.IsNullOrEmpty(gitPath) && GitPathHelper.ValidateGitPath(gitPath))
                {
                    settings.GitPath = gitPath;
                    SettingsService.Save(settings);
                }
                else gitPath = null;
            }

            bool gitFound = false;
            if (!string.IsNullOrEmpty(gitPath) && GitPathHelper.ValidateGitPath(gitPath))
            {
                try
                {
                    var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = gitPath;
                    process.StartInfo.Arguments = "--version";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode == 0 && output.Contains("git version")) gitFound = true;
                }
                catch { }
            }

            if (!gitFound)
            {
                GitWarningPanel.Visibility = Visibility.Visible;
                RepoActionsPanel.Visibility = Visibility.Collapsed;
                RepoOpsPanel.Visibility = Visibility.Collapsed;
                CommitPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                GitWarningPanel.Visibility = Visibility.Collapsed;
                RepoActionsPanel.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex) { AppLogger.Error(TAG, $"CheckGitAvailability 异常: {ex.Message}"); }
    }

    private void OnBrowseForInit(object sender, RoutedEventArgs e)
    {
        try
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            var folder = folderPicker.PickSingleFolderAsync().GetAwaiter().GetResult();
            if (folder != null) RepoPathTextBox.Text = folder.Path;
        }
        catch { }
    }

    private void LoadRecentRepos()
    {
        var settings = SettingsService.Load();
        if (settings.RecentRepos != null && settings.RecentRepos.Count > 0)
        {
            RecentReposListView.ItemsSource = null;
            RecentReposListView.ItemsSource = settings.RecentRepos;
            RecentReposPanel.Visibility = Visibility.Visible;
        }
        else RecentReposPanel.Visibility = Visibility.Collapsed;
    }

    private void OnRecentRepoClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is string repoPath && Directory.Exists(repoPath))
        {
            AppState.SetRepository(repoPath);
            SettingsService.AddRecentRepo(repoPath);
            RepoActionsPanel.Visibility = Visibility.Collapsed;
            RepoNameDisplay.Text = Path.GetFileName(repoPath);
            RepoNameDisplay.Visibility = Visibility.Visible;
            UpdateCloneUI();
            ShowInfoBar(Loc.GetString("CommonSuccess"), string.Format(Loc.GetString("RepoOpenSuccess"), Path.GetFileName(repoPath)), InfoBarSeverity.Success);
        }
    }

    private async Task ShowConflictDialogAsync(string repoPath, List<string> conflicts)
    {
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = Loc.GetString("ConflictDesc"), TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
        var listView = new ListView { ItemsSource = conflicts, MaxHeight = 200, SelectionMode = ListViewSelectionMode.None, BorderThickness = new Thickness(1), BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"], CornerRadius = new CornerRadius(4), Padding = new Thickness(4) };
        panel.Children.Add(listView);

        var dialog = new ContentDialog
        {
            Title = Loc.GetString("ConflictTitle"),
            Content = new ScrollViewer { Content = panel },
            PrimaryButtonText = Loc.GetString("ConflictOpenFolder"),
            CloseButtonText = Loc.GetString("ConflictOK"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{repoPath}\"", UseShellExecute = true }); }
            catch { }
        }
    }
}