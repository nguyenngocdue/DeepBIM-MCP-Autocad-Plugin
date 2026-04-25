using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class GetBoundingBoxCommand : DocumentContextCommandBase
    {
        public override string CommandName => "get_bounding_box";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string handleStr = parameters["handle"]?.ToString() ?? throw new System.ArgumentException("'handle' is required.");

            var db = doc.Database;
            object result;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var handle = new Handle(System.Convert.ToInt64(handleStr, 16));
                if (!db.TryGetObjectId(handle, out var objId))
                    return new AIResult<object> { Success = false, Message = $"Entity with handle '{handleStr}' not found." };

                var entity = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                var ext = entity.GeometricExtents;

                result = new
                {
                    handle = handleStr,
                    min = new { x = ext.MinPoint.X, y = ext.MinPoint.Y, z = ext.MinPoint.Z },
                    max = new { x = ext.MaxPoint.X, y = ext.MaxPoint.Y, z = ext.MaxPoint.Z },
                    width  = ext.MaxPoint.X - ext.MinPoint.X,
                    height = ext.MaxPoint.Y - ext.MinPoint.Y,
                    depth  = ext.MaxPoint.Z - ext.MinPoint.Z,
                };
                tr.Commit();
            }

            return new AIResult<object> { Success = true, Message = "Bounding box retrieved.", Response = result };
        }
    }
}
