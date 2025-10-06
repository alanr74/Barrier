using System;
using Avalonia.Threading;
using Avalonia.Media;

namespace Ava.Services
{
    public class LoggingService : ILoggingService
    {
        public Action<string>? LogAction { get; set; }
        public Action<string, Color>? LogWithColorAction { get; set; }
        public Action? ScrollAction { get; set; }

        public void Log(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                LogWithColorAction?.Invoke($"{DateTime.Now}: {message}", Colors.White);
                ScrollAction?.Invoke();
            });
        }

        public void LogWithColor(string message, Color color)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                LogWithColorAction?.Invoke($"{DateTime.Now}: {message}", color);
                ScrollAction?.Invoke();
            });
        }
    }
}
