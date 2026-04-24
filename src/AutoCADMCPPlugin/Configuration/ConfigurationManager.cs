using autocad_mcp_plugin.MCP.Interfaces;
using autocad_mcp_plugin.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace autocad_mcp_plugin.Configuration
{
    public class ConfigurationManager
    {
        private readonly ILogger _logger;
        private readonly string _configPath;

        public FrameworkConfig Config { get; private set; }

        public ConfigurationManager(ILogger logger)
        {
            _logger = logger;
            _configPath = PathManager.GetCommandRegistryFilePath();
        }

        public void LoadConfiguration()
        {
            try
            {
                Config = new FrameworkConfig { Commands = new List<CommandConfig>() };

                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    var loaded = JsonConvert.DeserializeObject<FrameworkConfig>(json);
                    if (loaded?.Commands != null && loaded.Commands.Count > 0)
                    {
                        Config = loaded;
                        _logger.Info("Configuration loaded: {0} ({1} commands)", _configPath, Config.Commands.Count);
                        return;
                    }
                }

                _logger.Warning("No configuration file at {0}, using built-in commands.", _configPath);
                LoadBuiltInCommands();
            }
            catch (Exception ex)
            {
                Config = new FrameworkConfig { Commands = new List<CommandConfig>() };
                _logger.Error("Failed to load configuration: {0}", ex.Message);
            }
        }

        private void LoadBuiltInCommands()
        {
            // Fallback: load from command.json next to plugin DLL
            string commandsDir = PathManager.GetCommandsDirectoryPath();
            string path = Path.Combine(commandsDir, "command.json");

            if (!File.Exists(path))
            {
                _logger.Warning("No command.json at {0}", path);
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(json);
                var arr = data?["commands"] as Newtonsoft.Json.Linq.JArray;
                if (arr == null) return;

                foreach (var token in arr)
                {
                    var cmd = token as Newtonsoft.Json.Linq.JObject;
                    if (cmd == null) continue;
                    string name = cmd["commandName"]?.ToString();
                    string assemblyPath = cmd["assemblyPath"]?.ToString();
                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(assemblyPath)) continue;

                    bool enabled = cmd["enabled"]?.ToObject<bool>() ?? true;
                    Config.Commands.Add(new CommandConfig
                    {
                        CommandName = name,
                        AssemblyPath = assemblyPath,
                        Enabled = enabled,
                        Description = cmd["description"]?.ToString() ?? ""
                    });
                }

                _logger.Info("Loaded {0} commands from command.json", Config.Commands.Count);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load command.json: {0}", ex.Message);
            }
        }
    }
}
