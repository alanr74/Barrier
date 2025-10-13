using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using System.Globalization;
using System.IO;
using Ava.ViewModels;
using SkiaSharp;
using Microsoft.Extensions.Configuration;
using System;

namespace Ava;

public partial class App : Application
{
    private TrayIcon? _trayIcon;
    private WindowIcon? _greenIcon;
    private WindowIcon? _amberIcon;
    private WindowIcon? _redIcon;
    private AppConfig? _appConfig;

    private WindowIcon CreateTrayIcon(Color color)
    {
        using (var bitmap = new SKBitmap(16, 16))
        {
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);
                using (var paint = new SKPaint())
                {
                    paint.Color = new SKColor(color.R, color.G, color.B, color.A);
                    paint.TextSize = 12;
                    paint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
                    paint.TextAlign = SKTextAlign.Center;
                    var text = "P";
                    var x = 8f;
                    var y = 8f + 6f; // approximate baseline
                    canvas.DrawText(text, x, y, paint);
                }
            }
            using (var stream = new MemoryStream())
            {
                bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
                stream.Position = 0;
                return new WindowIcon(stream);
            }
        }
    }

    private void UpdateTrayIcon()
    {
        if (_trayIcon == null) return;
        var vm = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.DataContext as MainWindowViewModel;
        if (vm == null) return;
        var mainWindow = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;

        switch (vm.OverallStatus)
        {
            case Status.Green:
                _trayIcon.Icon = _greenIcon;
                mainWindow.Icon = _greenIcon;
                break;
            case Status.Amber:
                _trayIcon.Icon = _amberIcon;
                mainWindow.Icon = _amberIcon;
                break;
            case Status.Red:
                _trayIcon.Icon = _redIcon;
                mainWindow.Icon = _redIcon;
                break;
        }
    }

    public void SetTheme(string screenMode)
    {
        ThemeVariant themeVariant;
        switch (screenMode.ToLower())
        {
            case "dark":
                themeVariant = ThemeVariant.Dark;
                break;
            case "light":
                themeVariant = ThemeVariant.Light;
                break;
            case "system":
            default:
                themeVariant = ThemeVariant.Default;
                break;
        }
        RequestedThemeVariant = themeVariant;
        // Force style update
        Application.Current.Styles.Clear();
        Application.Current.Styles.Add(new FluentTheme());
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Read config
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                _appConfig = config.Get<Ava.AppConfig>() ?? new AppConfig();

                // Set initial theme
                SetTheme(_appConfig.ScreenMode);

                var mainWindow = new MainWindow();
                desktop.MainWindow = mainWindow;

                // Create tray icon
                _trayIcon = new TrayIcon
                {
                    Icon = null, // Use default icon
                    ToolTipText = "Barrier Control System",
                    IsVisible = true
                };

                var showMenuItem = new NativeMenuItem("Show");
                showMenuItem.Click += (s, e) =>
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                };

                var settingsMenuItem = new NativeMenuItem("Settings");
                settingsMenuItem.Click += (s, e) =>
                {
                    var settingsWindow = new SettingsWindow(_appConfig);
                    settingsWindow.Show();
                };

                var exitMenuItem = new NativeMenuItem("Exit");
                exitMenuItem.Click += (s, e) =>
                {
                    // Clean shutdown: dispose tray icon and shutdown
                    _trayIcon?.Dispose();
                    desktop.Shutdown();
                };

                _trayIcon.Clicked += (s, e) =>
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                };

                _trayIcon.Menu = new NativeMenu
                {
                    Items = { showMenuItem, settingsMenuItem, exitMenuItem }
                };

                // Create icons
                _greenIcon = CreateTrayIcon(Colors.Green);
                _amberIcon = CreateTrayIcon(Colors.Orange);
                _redIcon = CreateTrayIcon(Colors.Red);

                // Set initial icon
                _trayIcon.Icon = _amberIcon;
                mainWindow.Icon = _amberIcon;

                // Subscribe to status changes
                var vm = mainWindow.DataContext as MainWindowViewModel;
                if (vm != null)
                {
                    vm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == "OverallStatus")
                        {
                            UpdateTrayIcon();
                        }
                    };
                }

                // Start minimized to tray if not set to start open
                if (!_appConfig.StartOpenOnLaunch)
                {
                    mainWindow.WindowState = WindowState.Minimized;
                    mainWindow.ShowInTaskbar = false;
                    mainWindow.Hide();
                }
            }
        }
}
