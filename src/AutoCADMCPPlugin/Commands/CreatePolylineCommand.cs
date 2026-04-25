using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class CreatePolylineCommand : DocumentContextCommandBase
    {
        public override string CommandName => "create_polyline";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            var pointsArr = parameters["points"] as JArray
                ?? throw new System.ArgumentException("'points' array is required.");
            bool closed = parameters["closed"]?.Value<bool>() ?? false;
            string layer = parameters["layer"]?.ToString();

            var db = doc.Database;
            string handle;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                var modelSpace = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);

                var pline = new Polyline();
                for (int i = 0; i < pointsArr.Count; i++)
                {
                    var pt = pointsArr[i] as JObject;
                    double x = pt["x"]?.Value<double>() ?? 0;
                    double y = pt["y"]?.Value<double>() ?? 0;
                    pline.AddVertexAt(i, new Point2d(x, y), 0, 0, 0);
                }
                pline.Closed = closed;
                if (!string.IsNullOrEmpty(layer)) pline.Layer = layer;

                modelSpace.AppendEntity(pline);
                tr.AddNewlyCreatedDBObject(pline, true);
                handle = pline.Handle.ToString();
                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = $"Polyline created with {pointsArr.Count} vertices.", Response = handle };
        }
    }
}
