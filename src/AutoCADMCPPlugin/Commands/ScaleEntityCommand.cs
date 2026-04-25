using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class ScaleEntityCommand : DocumentContextCommandBase
    {
        public override string CommandName => "scale_entity";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string handleStr  = parameters["handle"]?.ToString()  ?? throw new System.ArgumentException("'handle' is required.");
            double bx         = parameters["baseX"]?.Value<double>() ?? throw new System.ArgumentException("'baseX' is required.");
            double by         = parameters["baseY"]?.Value<double>() ?? throw new System.ArgumentException("'baseY' is required.");
            double bz         = parameters["baseZ"]?.Value<double>() ?? 0.0;
            double scaleFactor = parameters["scaleFactor"]?.Value<double>() ?? throw new System.ArgumentException("'scaleFactor' is required.");

            var db = doc.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var handle = new Handle(System.Convert.ToInt64(handleStr, 16));
                if (!db.TryGetObjectId(handle, out var objId))
                    return new AIResult<string> { Success = false, Message = $"Entity with handle '{handleStr}' not found.", Response = handleStr };

                var entity = (Entity)tr.GetObject(objId, OpenMode.ForWrite);
                var matrix = Matrix3d.Scaling(scaleFactor, new Point3d(bx, by, bz));
                entity.TransformBy(matrix);
                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = $"Entity scaled by {scaleFactor}.", Response = handleStr };
        }
    }
}
