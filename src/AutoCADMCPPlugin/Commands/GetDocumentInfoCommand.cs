using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;
using System.IO;

namespace autocad_mcp_plugin.Commands
{
    /// <summary>
    /// Returns basic information about the current AutoCAD document.
    /// Equivalent of get_current_view_info in the Revit plugin.
    /// </summary>
    public class GetDocumentInfoCommand : DocumentContextCommandBase
    {
        public override string CommandName => "get_document_info";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            var db = doc.Database;

            var info = new
            {
                fileName = Path.GetFileName(doc.Name),
                fullPath = doc.Name,
                units    = db.Insunits.ToString(),
                limMin   = new { x = db.Limmin.X, y = db.Limmin.Y },
                limMax   = new { x = db.Limmax.X, y = db.Limmax.Y },
                extMin   = new { x = db.Extmin.X, y = db.Extmin.Y, z = db.Extmin.Z },
                extMax   = new { x = db.Extmax.X, y = db.Extmax.Y, z = db.Extmax.Z }
            };

            return new AIResult<object>
            {
                Success  = true,
                Message  = "Document info retrieved.",
                Response = info
            };
        }
    }
}
