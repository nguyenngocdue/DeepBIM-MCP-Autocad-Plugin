using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class CopyEntityCommand : DocumentContextCommandBase
    {
        public override string CommandName => "copy_entity";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string handleStr = parameters["handle"]?.ToString() ?? throw new System.ArgumentException("'handle' is required.");
            double dx = parameters["dx"]?.Value<double>() ?? throw new System.ArgumentException("'dx' is required.");
            double dy = parameters["dy"]?.Value<double>() ?? throw new System.ArgumentException("'dy' is required.");
            double dz = parameters["dz"]?.Value<double>() ?? 0.0;

            var db = doc.Database;
            string newHandle;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var handle = new Handle(System.Convert.ToInt64(handleStr, 16));
                if (!db.TryGetObjectId(handle, out var objId))
                    return new AIResult<string> { Success = false, Message = $"Entity with handle '{handleStr}' not found.", Response = handleStr };

                var entity = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                var copy = entity.Clone() as Entity;

                var matrix = Matrix3d.Displacement(new Vector3d(dx, dy, dz));
                copy.TransformBy(matrix);

                var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                var modelSpace = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);
                modelSpace.AppendEntity(copy);
                tr.AddNewlyCreatedDBObject(copy, true);
                newHandle = copy.Handle.ToString();
                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = "Entity copied.", Response = newHandle };
        }
    }
}
