using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class MeasureAreaCommand : DocumentContextCommandBase
    {
        public override string CommandName => "measure_area";

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
                double area = 0;
                double perimeter = 0;

                if (entity is Curve curve)
                {
                    try { area = curve.Area; } catch { }
                    try { perimeter = curve.GetDistanceAtParameter(curve.EndParam); } catch { }
                }
                else if (entity is Circle c)
                {
                    area = System.Math.PI * c.Radius * c.Radius;
                    perimeter = 2 * System.Math.PI * c.Radius;
                }

                result = new { area, perimeter, handle = handleStr };
                tr.Commit();
            }

            return new AIResult<object> { Success = true, Message = "Area measured.", Response = result };
        }
    }
}
