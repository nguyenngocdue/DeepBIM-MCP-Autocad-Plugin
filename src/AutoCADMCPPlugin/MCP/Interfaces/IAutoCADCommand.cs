using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.MCP.Interfaces
{
    /// <summary>
    /// AutoCAD equivalent of IRevitCommand from RevitMCPSDK.
    /// Implement this interface for each MCP tool.
    /// </summary>
    public interface IAutoCADCommand
    {
        string CommandName { get; }
        object Execute(JObject parameters, string requestId);
    }
}
