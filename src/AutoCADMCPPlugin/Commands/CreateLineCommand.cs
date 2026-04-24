using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    /// <summary>
    /// Creates a line entity in the current drawing.
    /// Equivalent of create_line_based_element in the Revit plugin.
    ///
    /// Parameters:
    ///   startX, startY, startZ (double) — start point
    ///   endX,   endY,   endZ   (double) — end point
    ///   layer  (string, optional)       — target layer name
    /// </summary>
    public class CreateLineCommand : DocumentContextCommandBase
    {
        public override string CommandName => "create_line";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            double startX = parameters?["startX"]?.Value<double>() ?? 0;
            double startY = parameters?["startY"]?.Value<double>() ?? 0;
            double startZ = parameters?["startZ"]?.Value<double>() ?? 0;
            double endX   = parameters?["endX"]?.Value<double>()   ?? 1;
            double endY   = parameters?["endY"]?.Value<double>()   ?? 0;
            double endZ   = parameters?["endZ"]?.Value<double>()   ?? 0;
            string layer  = parameters?["layer"]?.ToString();

            var db = doc.Database;

            string objectIdStr;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var modelSpace = (BlockTableRecord)tr.GetObject(
                    SymbolUtilityServices.GetBlockModelSpaceId(db),
                    OpenMode.ForWrite);

                var line = new Line(
                    new Point3d(startX, startY, startZ),
                    new Point3d(endX, endY, endZ));

                if (!string.IsNullOrEmpty(layer))
                    line.Layer = layer;

                modelSpace.AppendEntity(line);
                tr.AddNewlyCreatedDBObject(line, true);

                objectIdStr = line.ObjectId.ToString();
                tr.Commit();
            }

            return new AIResult<object>
            {
                Success  = true,
                Message  = $"Line created from ({startX},{startY},{startZ}) to ({endX},{endY},{endZ}).",
                Response = new { objectId = objectIdStr, layer = layer ?? "0" }
            };
        }
    }
}
