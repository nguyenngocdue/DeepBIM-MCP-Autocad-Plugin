using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class GetEntityPropertiesCommand : DocumentContextCommandBase
    {
        public override string CommandName => "get_entity_properties";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string handleStr = parameters["handle"]?.ToString() ?? throw new System.ArgumentException("'handle' is required.");

            var db = doc.Database;
            object props;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var handle = new Handle(System.Convert.ToInt64(handleStr, 16));
                if (!db.TryGetObjectId(handle, out var objId))
                    return new AIResult<object> { Success = false, Message = $"Entity with handle '{handleStr}' not found." };

                var entity = (Entity)tr.GetObject(objId, OpenMode.ForRead);

                double? length = null;
                double? area   = null;

                if (entity is Curve curve)
                {
                    try { length = curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam); } catch { }
                    try { area = curve.Area; } catch { }
                }
                else if (entity is Circle c)
                {
                    area = System.Math.PI * c.Radius * c.Radius;
                    length = 2 * System.Math.PI * c.Radius;
                }

                var extents = entity.GeometricExtents;
                props = new
                {
                    handle      = handleStr,
                    type        = entity.GetType().Name,
                    dxfType     = entity.GetRXClass().DxfName,
                    layer       = entity.Layer,
                    color       = entity.Color.ColorNameForDisplay,
                    linetype    = entity.Linetype,
                    lineweight  = entity.LineWeight.ToString(),
                    length,
                    area,
                    minX = extents.MinPoint.X,
                    minY = extents.MinPoint.Y,
                    minZ = extents.MinPoint.Z,
                    maxX = extents.MaxPoint.X,
                    maxY = extents.MaxPoint.Y,
                    maxZ = extents.MaxPoint.Z,
                };
                tr.Commit();
            }

            return new AIResult<object> { Success = true, Message = "Entity properties retrieved.", Response = props };
        }
    }
}
