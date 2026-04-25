using Autodesk.AutoCAD.ApplicationServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class ExportDxfCommand : DocumentContextCommandBase
    {
        public override string CommandName => "export_dxf";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string outputPath = parameters["outputPath"]?.ToString()
                ?? throw new System.ArgumentException("'outputPath' is required.");

            // Escape the path for AutoCAD command line (wrap in quotes)
            string safePath = outputPath.Replace("\"", "\\\"");

            // DXFOUT <path> <precision>
            // Precision 16 = maximum precision
            doc.SendStringToExecute($"_.DXFOUT \"{safePath}\" 16 ", true, false, false);

            return new AIResult<string> { Success = true, Message = $"DXF export initiated to '{outputPath}'.", Response = outputPath };
        }
    }
}

