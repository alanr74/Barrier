using System;
using Avalonia.Media;

namespace Ava.Services
{
    public interface ILoggingService
    {
        void Log(string message);
        void LogWithColor(string message, Color color);
        Action<string, Color>? LogWithColorAction { get; set; }
    }
}
