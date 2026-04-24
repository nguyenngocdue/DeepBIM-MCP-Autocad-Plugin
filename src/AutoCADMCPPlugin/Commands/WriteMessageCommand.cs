using Autodesk.AutoCAD.ApplicationServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    /// <summary>
    /// Writes a message to the AutoCAD command line / editor.
    /// Useful for debug feedback from MCP tools.
    ///
    /// Parameters:
    ///   message (string) — text to display
    /// </summary>
    public class WriteMessageCommand : DocumentContextCommandBase
    {
        public override string CommandName => "write_message";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string message = parameters?["message"]?.ToString() ?? "(no message)";
            doc.Editor.WriteMessage($"\n[DeepBim-MCP] {message}\n");

            return new AIResult<string>
            {
                Success  = true,
                Message  = "Message written to AutoCAD editor.",
                Response = message
            };
        }
    }
}
