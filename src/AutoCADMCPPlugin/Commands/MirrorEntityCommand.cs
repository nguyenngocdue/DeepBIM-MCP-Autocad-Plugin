using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class MirrorEntityCommand : DocumentContextCommandBase
    {
        public override string CommandName => "mirror_entity";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string handleStr  = parameters["handle"]?.ToString()  ?? throw new System.ArgumentException("'handle' is required.");
            double mx1 = parameters["mirrorX1"]?.Value<double>() ?? throw new System.ArgumentException("'mirrorX1' is required.");
            double my1 = parameters["mirrorY1"]?.Value<double>() ?? throw new System.ArgumentException("'mirrorY1' is required.");
            double mx2 = parameters["mirrorX2"]?.Value<double>() ?? throw new System.ArgumentException("'mirrorX2' is required.");
            double my2 = parameters["mirrorY2"]?.Value<double>() ?? throw new System.ArgumentException("'mirrorY2' is required.");
            bool deleteSource = parameters["deleteSource"]?.Value<bool>() ?? false;

            var db = doc.Database;
            string newHandle;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var handle = new Handle(System.Convert.ToInt64(handleStr, 16));
                if (!db.TryGetObjectId(handle, out var objId))
                    return new AIResult<string> { Success = false, Message = $"Entity with handle '{handleStr}' not found.", Response = handleStr };

                var entity = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                var mirrorLine = new Line3d(new Point3d(mx1, my1, 0), new Point3d(mx2, my2, 0));
                var matrix = Matrix3d.Mirroring(mirrorLine);

                var mirrored = entity.Clone() as Entity;
                mirrored.TransformBy(matrix);

                var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                var modelSpace = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);
                modelSpace.AppendEntity(mirrored);
                tr.AddNewlyCreatedDBObject(mirrored, true);
                newHandle = mirrored.Handle.ToString();

                if (deleteSource)
                {
                    entity.UpgradeOpen();
                    entity.Erase();
                }

                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = "Entity mirrored.", Response = newHandle };
        }
    }
}
