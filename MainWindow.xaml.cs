using FluentGit.Services;
using FluentGit.Views;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media; // 【补上了这个关键命名空间】
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FluentGit;

public sealed partial class MainWindow : Window
{
    private const string TAG = "MainWindow";
    private CancellationTokenSource? _globalInfoBarCts;
    private bool _isGlobalInfoBarAnimating = false;

    public MainWindow()
    {
        InitializeComponent();
        InitializeBackdrop();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string iconPath = Path.Combine(appDir, "Assets", "AppIcon.ico");
        AppWindow.SetIcon(iconPath);

        ApplyThemeFromSettings();

        NavFrame.Navigated += OnNavFrameNavigated;
        NavFrame.Navigate(typeof(RepoPage));
    }

    public async void ShowInfoBar(string title, string message, InfoBarSeverity severity)
    {
        if (_isGlobalInfoBarAnimating)
        {
            _globalInfoBarCts?.Cancel();
            await Task.Delay(50);
        }

        _isGlobalInfoBarAnimating = true;
        _globalInfoBarCts = new CancellationTokenSource();
        var token = _globalInfoBarCts.Token;

        GlobalInfoBarSlideIn.Stop();
        GlobalInfoBarSlideOut.Stop();
        GlobalInfoBarTransform.Y = -80;
        GlobalInfoBarContainer.Opacity = 0;
        GlobalInfoBarContainer.Visibility = Visibility.Visible;

        GlobalInfoBar.Title = title;
        GlobalInfoBar.Message = message;
        GlobalInfoBar.Severity = severity;

        GlobalInfoBarSlideIn.Begin();

        try
        {
            await Task.Delay(3000, token);
            GlobalInfoBarSlideOut.Begin();
            await Task.Delay(200);
            GlobalInfoBarContainer.Visibility = Visibility.Collapsed;
        }
        catch (TaskCanceledException)
        {
            GlobalInfoBarContainer.Visibility = Visibility.Collapsed;
            GlobalInfoBarTransform.Y = -80;
            GlobalInfoBarContainer.Opacity = 0;
            GlobalInfoBarSlideIn.Stop();
            GlobalInfoBarSlideOut.Stop();
        }
        finally
        {
            _isGlobalInfoBarAnimating = false;
        }
    }

    private void OnNavFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs args)
    {
        AppTitleBar.IsBackButtonVisible = NavFrame.CanGoBack;

        string? tag = args.SourcePageType switch
        {
            var t when t == typeof(RepoPage)     => "repo",
            var t when t == typeof(FilePage)     => "file",
            var t when t == typeof(HistoryPage)  => "history",
            var t when t == typeof(ClonePage)    => "clone",
            var t when t == typeof(AboutPage)    => "about",
            var t when t == typeof(SettingsPage) => "settings",
            _ => null
        };

        if (tag != null)
            SelectNavItem(tag);
    }

    private void SelectNavItem(string tag)
    {
        if (NavView.MenuItems != null)
        {
            foreach (var obj in NavView.MenuItems)
            {
                if (obj is NavigationViewItem item && (item.Tag as string) == tag)
                {
                    NavView.SelectedItem = item;
                    return;
                }
            }
        }

        if (NavView.FooterMenuItems != null)
        {
            foreach (var obj in NavView.FooterMenuItems)
            {
                if (obj is NavigationViewItem item && (item.Tag as string) == tag)
                {
                    NavView.SelectedItem = item;
                    return;
                }
            }
        }
    }

    private void InitializeBackdrop()
    {
        var settings = SettingsService.Load();
        ApplyBackdrop(settings.BackdropType ?? "Default");
    }

    public void ApplyBackdrop(string backdropType)
    {
        try
        {
            switch (backdropType)
            {
                case "Mica":
                    SystemBackdrop = MicaController.IsSupported() ? new MicaBackdrop { Kind = MicaKind.Base } : null;
                    AppLogger.OK(TAG, $"Backdrop: Mica (supported={MicaController.IsSupported()})");
                    break;
                case "MicaAlt":
                    SystemBackdrop = MicaController.IsSupported() ? new MicaBackdrop { Kind = MicaKind.BaseAlt } : null;
                    AppLogger.OK(TAG, $"Backdrop: MicaAlt (supported={MicaController.IsSupported()})");
                    break;
                case "Acrylic":
                    SystemBackdrop = DesktopAcrylicController.IsSupported() ? new DesktopAcrylicBackdrop() : null;
                    AppLogger.OK(TAG, $"Backdrop: Acrylic (supported={DesktopAcrylicController.IsSupported()})");
                    break;
                case "None":
                    SystemBackdrop = null;
                    AppLogger.OK(TAG, "Backdrop: None (solid color)");
                    break;
                case "Default":
                default:
                    SystemBackdrop = MicaController.IsSupported() ? new MicaBackdrop() : null;
                    AppLogger.OK(TAG, $"Backdrop: Default (Mica={MicaController.IsSupported()})");
                    break;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error(TAG, $"ApplyBackdrop 异常: {ex.Message}");
            SystemBackdrop = null;
        }
    }

    private void ApplyThemeFromSettings()
    {
        try
        {
            var settings = SettingsService.Load();
            var theme = settings.AppTheme switch
            {
                "Dark"  => ElementTheme.Dark,
                "Light" => ElementTheme.Light,
                _       => ElementTheme.Default
            };

            if (Content is FrameworkElement root)
                root.RequestedTheme = theme;

            AppLogger.Info(TAG, $"主题已应用: {theme}");
        }
        catch (Exception ex)
        {
            AppLogger.Error(TAG, $"ApplyThemeFromSettings 异常: {ex.Message}");
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        if (NavFrame.CanGoBack)
            NavFrame.GoBack();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "repo":
                    NavFrame.Navigate(typeof(RepoPage));
                    break;
                case "file":
                    NavFrame.Navigate(typeof(FilePage));
                    break;
                case "history":
                    NavFrame.Navigate(typeof(HistoryPage));
                    break;
                case "clone":
                    NavFrame.Navigate(typeof(ClonePage));
                    break;
                case "about":
                    NavFrame.Navigate(typeof(AboutPage));
                    break;
                case "settings":
                    NavFrame.Navigate(typeof(SettingsPage));
                    break;
                default:
                    AppLogger.Warning(TAG, $"未知导航项: {item.Tag}");
                    break;
            }
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item &&
            (item.Tag as string) == "settings")
        {
            RotateSettingsIcon();
        }
    }

    private void RotateSettingsIcon()
    {
        try
        {
            var anim = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTarget(anim, SettingsIconRotation);
            Storyboard.SetTargetProperty(anim, "Angle");

            var sb = new Storyboard();
            sb.Children.Add(anim);
            sb.Completed += (_, _) => SettingsIconRotation.Angle = 0;

            sb.Begin();
        }
        catch (Exception ex)
        {
            AppLogger.Warning(TAG, $"齿轮旋转动画失败: {ex.Message}");
        }
    }
}