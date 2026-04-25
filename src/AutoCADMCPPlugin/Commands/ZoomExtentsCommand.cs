using Autodesk.AutoCAD.ApplicationServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class ZoomExtentsCommand : DocumentContextCommandBase
    {
        public override string CommandName => "zoom_extents";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            doc.SendStringToExecute("_.ZOOM _E ", true, false, false);
            return new AIResult<string> { Success = true, Message = "Zoom extents applied.", Response = "ok" };
        }
    }
}
