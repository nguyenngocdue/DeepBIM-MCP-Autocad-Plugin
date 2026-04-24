using autocad_mcp_plugin.MCP.Interfaces;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    /// <summary>
    /// Simple test command — equivalent of say_hello in Revit plugin.
    /// Does not need document context so it does NOT extend DocumentContextCommandBase.
    /// </summary>
    public class SayHelloCommand : IAutoCADCommand
    {
        public string CommandName => "say_hello";

        public object Execute(JObject parameters, string requestId)
        {
            string name = parameters?["name"]?.ToString() ?? "World";
            return new AIResult<string>
            {
                Success = true,
                Message = "Hello command executed.",
                Response = $"Hello, {name}! AutoCAD MCP Plugin is running."
            };
        }
    }
}
