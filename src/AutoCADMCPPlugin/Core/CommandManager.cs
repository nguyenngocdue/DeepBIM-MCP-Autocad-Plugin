using autocad_mcp_plugin.Configuration;
using autocad_mcp_plugin.MCP.Interfaces;
using autocad_mcp_plugin.Utils;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace autocad_mcp_plugin.Core
{
    /// <summary>
    /// Loads IAutoCADCommand implementations from configured assemblies.
    /// Mirrors CommandManager from Revit plugin.
    /// </summary>
    public class CommandManager
    {
        private readonly ICommandRegistry _commandRegistry;
        private readonly ILogger _logger;
        private readonly ConfigurationManager _configManager;

        public CommandManager(
            ICommandRegistry commandRegistry,
            ILogger logger,
            ConfigurationManager configManager)
        {
            _commandRegistry = commandRegistry;
            _logger = logger;
            _configManager = configManager;
        }

        public void LoadCommands()
        {
            _logger.Info("Start loading commands...");

            if (_configManager.Config?.Commands == null || _configManager.Config.Commands.Count == 0)
            {
                _logger.Warning("No commands configured.");
                return;
            }

            foreach (var commandConfig in _configManager.Config.Commands)
            {
                try
                {
                    if (!commandConfig.Enabled)
                    {
                        _logger.Info("Skipping disabled command: {0}", commandConfig.CommandName);
                        continue;
                    }

                    LoadCommandFromAssembly(commandConfig);
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to load command {0}: {1}", commandConfig.CommandName, ex.Message);
                }
            }

            var registered = _commandRegistry.GetRegisteredCommands().ToList();
            _logger.Info("Command loading complete. Registered: {0}",
                registered.Count > 0 ? string.Join(", ", registered) : "(none)");
        }

        private void LoadCommandFromAssembly(CommandConfig config)
        {
            string assemblyPath = config.AssemblyPath;
            if (!Path.IsPathRooted(assemblyPath))
            {
                string baseDir = PathManager.GetCommandsDirectoryPath();
                assemblyPath = Path.Combine(baseDir, assemblyPath);
            }

            if (!File.Exists(assemblyPath))
            {
                _logger.Error("Command assembly not found: {0}", assemblyPath);
                return;
            }

            Assembly assembly = Assembly.LoadFrom(assemblyPath);

            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IAutoCADCommand).IsAssignableFrom(type) &&
                    !type.IsInterface &&
                    !type.IsAbstract)
                {
                    try
                    {
                        var command = (IAutoCADCommand)Activator.CreateInstance(type);
                        _commandRegistry.RegisterCommand(command);
                        _logger.Info("Registered command: {0} ({1})", command.CommandName, type.FullName);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Failed to instantiate {0}: {1}", type.FullName, ex.Message);
                    }
                }
            }
        }
    }
}
