using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentGit.Services;

namespace FluentGit.Views;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        LoadAppVersion();
    }

    private void LoadAppVersion()
    {
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                AppVersionText.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
            }
        }
        catch
        {
            AppVersionText.Text = "v0.0.1";
        }
    }

    private async void CheckUpdateBtn_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateBtn.IsEnabled = false;
        CheckUpdateBtn.Content = Loc.GetString("AboutChecking.Text");

        try
        {
            var latestRelease = await UpdateService.CheckForUpdateAsync();

            if (latestRelease == null)
            {
                await ShowDialogAsync(Loc.GetString("CommonInfo"), Loc.GetString("AboutLatest.Text"));
                return;
            }

            string content = string.Format(Loc.GetString("AboutNewVersionMsg"), latestRelease.TagName, UpdateService.CurrentVersion);
            
            var dialog = new ContentDialog
            {
                Title = Loc.GetString("AboutNewVersionTitle"),
                Content = content,
                PrimaryButtonText = Loc.GetString("AboutGoToDownload.Content"),
                CloseButtonText = Loc.GetString("CommonCancel"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(latestRelease.HtmlUrl));
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error("AboutPage", $"检查更新异常: {ex.Message}");
            await ShowDialogAsync(Loc.GetString("CommonError"), Loc.GetString("AboutUpdateFailed.Text"));
        }
        finally
        {
            CheckUpdateBtn.IsEnabled = true;
            CheckUpdateBtn.Content = Loc.GetString("AboutCheckUpdate.Content");
        }
    }

    private async Task ShowDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = Loc.GetString("CommonOK"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}