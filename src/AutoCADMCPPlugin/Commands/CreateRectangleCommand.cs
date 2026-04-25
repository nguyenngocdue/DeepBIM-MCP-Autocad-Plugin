using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class CreateRectangleCommand : DocumentContextCommandBase
    {
        public override string CommandName => "create_rectangle";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            double x1 = parameters["x1"]?.Value<double>() ?? throw new System.ArgumentException("'x1' is required.");
            double y1 = parameters["y1"]?.Value<double>() ?? throw new System.ArgumentException("'y1' is required.");
            double x2 = parameters["x2"]?.Value<double>() ?? throw new System.ArgumentException("'x2' is required.");
            double y2 = parameters["y2"]?.Value<double>() ?? throw new System.ArgumentException("'y2' is required.");
            double z  = parameters["z"]?.Value<double>() ?? 0.0;
            string layer = parameters["layer"]?.ToString();

            var db = doc.Database;
            string handle;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                var modelSpace = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);

                var pline = new Polyline();
                pline.AddVertexAt(0, new Point2d(x1, y1), 0, 0, 0);
                pline.AddVertexAt(1, new Point2d(x2, y1), 0, 0, 0);
                pline.AddVertexAt(2, new Point2d(x2, y2), 0, 0, 0);
                pline.AddVertexAt(3, new Point2d(x1, y2), 0, 0, 0);
                pline.Closed = true;
                pline.Elevation = z;
                if (!string.IsNullOrEmpty(layer)) pline.Layer = layer;

                modelSpace.AppendEntity(pline);
                tr.AddNewlyCreatedDBObject(pline, true);
                handle = pline.Handle.ToString();
                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = "Rectangle created.", Response = handle };
        }
    }
}
