using Autodesk.AutoCAD.ApplicationServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class ExportPdfCommand : DocumentContextCommandBase
    {
        public override string CommandName => "export_pdf";
        protected override int TimeoutMs => 60000;

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string outputPath = parameters["outputPath"]?.ToString()
                ?? throw new System.ArgumentException("'outputPath' is required.");
            string layoutName = parameters["layout"]?.ToString();

            // Switch layout if specified
            if (!string.IsNullOrEmpty(layoutName))
                doc.SendStringToExecute($"_.LAYOUT _S \"{layoutName}\" ", true, false, false);

            // Use PLOT command with DWG To PDF.pc3, extents, fit-to-paper
            // Format: PLOT <detailed-plot-yes/no> <layout-name> <device> <media> <plot-style> <plot-area> <corner1> <corner2> <fit-yn> <centered-yn> <orientation> <upside-down> <file-path> <save-changes>
            string safePath = outputPath.Replace("\"", "");
            doc.SendStringToExecute(
                $"_.PLOT _Y \"\" \"DWG To PDF.pc3\" \"ISO A4\" \"\" _E _F _C _P _N \"{safePath}\" _Y ",
                true, false, false);

            return new AIResult<string> { Success = true, Message = $"PDF export initiated to '{outputPath}'.", Response = outputPath };
        }
    }
}
