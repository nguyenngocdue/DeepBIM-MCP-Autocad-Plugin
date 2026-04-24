using System;
using System.IO;
using System.Reflection;

namespace autocad_mcp_plugin.Utils
{
    public static class PathManager
    {
        private static string _pluginDirectory;

        private const string AppDataFolder = "DeepBim-MCP-ACAD";

        public static void SetPluginDirectory(string dir)
        {
            _pluginDirectory = dir;
        }

        public static string GetPluginDirectoryPath()
        {
            if (_pluginDirectory != null)
                return _pluginDirectory;

            // Method 1: Assembly location
            string location = typeof(PathManager).Assembly.Location;
            if (!string.IsNullOrEmpty(location))
            {
                string dir = Path.GetDirectoryName(location);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    _pluginDirectory = dir;
                    return _pluginDirectory;
                }
            }

            // Method 2: Fallback to %APPDATA%\DeepBim-MCP-ACAD
            _pluginDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppDataFolder);
            if (!Directory.Exists(_pluginDirectory))
                Directory.CreateDirectory(_pluginDirectory);

            return _pluginDirectory;
        }

        public static string GetLogsDirectoryPath()
        {
            string dir = Path.Combine(GetPluginDirectoryPath(), "logs");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public static string GetCommandsDirectoryPath()
        {
            return Path.Combine(GetPluginDirectoryPath(), "Commands");
        }

        public static string GetCommandRegistryFilePath()
        {
            return Path.Combine(GetPluginDirectoryPath(), "Commands", "commandRegistry.json");
        }

        /// <summary>Path to the port file so MCP server can discover the TCP port.</summary>
        public static string GetPortFilePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppDataFolder, "mcp-port.txt");
        }
    }
}
