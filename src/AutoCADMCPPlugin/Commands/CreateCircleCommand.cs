using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class CreateCircleCommand : DocumentContextCommandBase
    {
        public override string CommandName => "create_circle";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            double cx = parameters["centerX"]?.Value<double>() ?? throw new System.ArgumentException("'centerX' is required.");
            double cy = parameters["centerY"]?.Value<double>() ?? throw new System.ArgumentException("'centerY' is required.");
            double cz = parameters["centerZ"]?.Value<double>() ?? 0.0;
            double radius = parameters["radius"]?.Value<double>() ?? throw new System.ArgumentException("'radius' is required.");
            string layer = parameters["layer"]?.ToString();

            var db = doc.Database;
            string handle;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                var modelSpace = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);

                var circle = new Circle(new Point3d(cx, cy, cz), Vector3d.ZAxis, radius);
                if (!string.IsNullOrEmpty(layer)) circle.Layer = layer;

                modelSpace.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);
                handle = circle.Handle.ToString();
                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = "Circle created.", Response = handle };
        }
    }
}
