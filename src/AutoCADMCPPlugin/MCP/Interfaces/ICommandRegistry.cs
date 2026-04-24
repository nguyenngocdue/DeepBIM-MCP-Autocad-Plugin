using System.Collections.Generic;

namespace autocad_mcp_plugin.MCP.Interfaces
{
    public interface ICommandRegistry
    {
        void RegisterCommand(IAutoCADCommand command);
        bool TryGetCommand(string commandName, out IAutoCADCommand command);
        void ClearCommands();
        IEnumerable<string> GetRegisteredCommands();
    }
}
