using System;
using Avalonia.Threading;

namespace Ava.Services
{
    public class LoggingService : ILoggingService
    {
        public Action<string>? LogAction { get; set; }
        public Action? ScrollAction { get; set; }

        public void Log(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                LogAction?.Invoke($"{DateTime.Now}: {message}\n");
                ScrollAction?.Invoke();
            });
        }
    }
}
