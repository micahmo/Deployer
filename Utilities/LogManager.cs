using System;
using System.Collections.Generic;
using System.Globalization;

namespace Utilities
{
    public static class LogManager
    {
        public static T RegisterLogger<T>(T logger) where T : ILogger
        {
            _loggers.Add(logger);
            return logger;
        }

        public static void LogBreak()
        {
            _loggers.ForEach(l => l.Log("<hr>"));
        }

        public static void Log(string message, string title = "", LogLevel level = LogLevel.Info)
        {
            string style = level switch
            {
                LogLevel.Info => "<body>",
                LogLevel.Warning => "<body style='color:yellow'>",
                LogLevel.Error => "<body style='color:red'>",
                _ => "<body>"
            };

            message = string.Join(Environment.NewLine,
                //"<hr>",
                style,
                $"<b>{DateTime.Now.ToString("hh:mm:ss.fff tt", CultureInfo.CurrentCulture)}{(string.IsNullOrEmpty(title) ? "" : $" - {title}")}</b>",
                message,
                "</body>").Replace(Environment.NewLine, "<br>");

            _loggers.ForEach(l => l.Log(message));
        }

        private static readonly List<ILogger> _loggers = new List<ILogger>();
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }
}
