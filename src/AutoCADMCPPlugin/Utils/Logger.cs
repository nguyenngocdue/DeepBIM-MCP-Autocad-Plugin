using autocad_mcp_plugin.MCP.Interfaces;
using System;
using System.IO;

namespace autocad_mcp_plugin.Utils
{
    public class Logger : ILogger
    {
        private readonly string _logFilePath;
        private readonly LogLevel _currentLogLevel = LogLevel.Info;

        public Logger()
        {
            string logDir = PathManager.GetLogsDirectoryPath();
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            _logFilePath = Path.Combine(logDir, $"acad-mcp_{DateTime.Now:yyyyMMdd}.log");
        }

        public void Log(LogLevel level, string message, params object[] args)
        {
            if (level < _currentLogLevel) return;

            string formatted = args.Length > 0 ? string.Format(message, args) : message;
            string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {formatted}";

            System.Diagnostics.Debug.WriteLine(entry);

            try
            {
                File.AppendAllText(_logFilePath, entry + Environment.NewLine);
            }
            catch { }
        }

        public void Debug(string message, params object[] args)   => Log(LogLevel.Debug, message, args);
        public void Info(string message, params object[] args)    => Log(LogLevel.Info, message, args);
        public void Warning(string message, params object[] args) => Log(LogLevel.Warning, message, args);
        public void Error(string message, params object[] args)   => Log(LogLevel.Error, message, args);
    }
}
