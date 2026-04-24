using autocad_mcp_plugin.MCP.Interfaces;
using System.Collections.Generic;

namespace autocad_mcp_plugin.Core
{
    /// <summary>
    /// AutoCAD equivalent of RevitCommandRegistry.
    /// </summary>
    public class AutoCADCommandRegistry : ICommandRegistry
    {
        private readonly Dictionary<string, IAutoCADCommand> _commands
            = new Dictionary<string, IAutoCADCommand>();

        public void RegisterCommand(IAutoCADCommand command)
        {
            _commands[command.CommandName] = command;
        }

        public bool TryGetCommand(string commandName, out IAutoCADCommand command)
        {
            return _commands.TryGetValue(commandName, out command);
        }

        public void ClearCommands()
        {
            _commands.Clear();
        }

        public IEnumerable<string> GetRegisteredCommands()
        {
            return _commands.Keys;
        }
    }
}
