using System;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia;
using Avalonia.Styling;

namespace Ava.Services
{
    public class LoggingService : ILoggingService
    {
        public Action<string>? LogAction { get; set; }
        public Action<string, Color>? LogWithColorAction { get; set; }
        public Action? ScrollAction { get; set; }

    private Color AdjustColorForTheme(Color color)
    {
        if (Application.Current?.RequestedThemeVariant == ThemeVariant.Dark)
        {
            if (color == Colors.White) return Colors.LightGray;
            if (color == Colors.Green) return Colors.LightGreen;
            if (color == Colors.Red) return Colors.LightCoral;
            if (color == Colors.Orange) return Colors.Yellow;
            // Add more as needed
        }
        else
        {
            // Light mode: use dark colors for readability
            if (color == Colors.White) return Colors.Black;
            if (color == Colors.Green) return Colors.DarkGreen;
            if (color == Colors.Red) return Colors.DarkRed;
            if (color == Colors.Orange) return Colors.DarkOrange;
            // Add more as needed
        }
        return color;
    }

        public void Log(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var adjustedColor = AdjustColorForTheme(Colors.White);
                LogWithColorAction?.Invoke($"{DateTime.Now}: {message}", adjustedColor);
                ScrollAction?.Invoke();
            });
        }

        public void LogWithColor(string message, Color color)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var adjustedColor = AdjustColorForTheme(color);
                LogWithColorAction?.Invoke($"{DateTime.Now}: {message}", adjustedColor);
                ScrollAction?.Invoke();
            });
        }
    }
}
