using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class CreateArcCommand : DocumentContextCommandBase
    {
        public override string CommandName => "create_arc";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            double cx = parameters["centerX"]?.Value<double>() ?? throw new System.ArgumentException("'centerX' is required.");
            double cy = parameters["centerY"]?.Value<double>() ?? throw new System.ArgumentException("'centerY' is required.");
            double cz = parameters["centerZ"]?.Value<double>() ?? 0.0;
            double radius = parameters["radius"]?.Value<double>() ?? throw new System.ArgumentException("'radius' is required.");
            double startDeg = parameters["startAngle"]?.Value<double>() ?? throw new System.ArgumentException("'startAngle' is required.");
            double endDeg   = parameters["endAngle"]?.Value<double>()   ?? throw new System.ArgumentException("'endAngle' is required.");
            string layer = parameters["layer"]?.ToString();

            double startRad = startDeg * System.Math.PI / 180.0;
            double endRad   = endDeg   * System.Math.PI / 180.0;

            var db = doc.Database;
            string handle;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                var modelSpace = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);

                var arc = new Arc(new Point3d(cx, cy, cz), radius, startRad, endRad);
                if (!string.IsNullOrEmpty(layer)) arc.Layer = layer;

                modelSpace.AppendEntity(arc);
                tr.AddNewlyCreatedDBObject(arc, true);
                handle = arc.Handle.ToString();
                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = "Arc created.", Response = handle };
        }
    }
}
