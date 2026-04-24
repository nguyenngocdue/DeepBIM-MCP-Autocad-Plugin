using Autodesk.AutoCAD.ApplicationServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    /// <summary>
    /// Simple test command — shows an alert dialog in AutoCAD.
    /// Extends DocumentContextCommandBase to marshal onto the AutoCAD main thread.
    /// </summary>
    public class SayHelloCommand : DocumentContextCommandBase
    {
        public override string CommandName => "say_hello";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string name = parameters?["name"]?.ToString() ?? "World";
            string message = $"Hello, {name}! AutoCAD MCP Plugin is running.";

            Application.ShowAlertDialog(message);

            return new AIResult<string>
            {
                Success = true,
                Message = "Hello command executed.",
                Response = message
            };
        }
    }
}
