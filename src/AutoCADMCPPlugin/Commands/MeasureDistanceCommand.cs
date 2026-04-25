using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class MeasureDistanceCommand : DocumentContextCommandBase
    {
        public override string CommandName => "measure_distance";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            double x1 = parameters["x1"]?.Value<double>() ?? throw new System.ArgumentException("'x1' is required.");
            double y1 = parameters["y1"]?.Value<double>() ?? throw new System.ArgumentException("'y1' is required.");
            double z1 = parameters["z1"]?.Value<double>() ?? 0.0;
            double x2 = parameters["x2"]?.Value<double>() ?? throw new System.ArgumentException("'x2' is required.");
            double y2 = parameters["y2"]?.Value<double>() ?? throw new System.ArgumentException("'y2' is required.");
            double z2 = parameters["z2"]?.Value<double>() ?? 0.0;

            var p1 = new Point3d(x1, y1, z1);
            var p2 = new Point3d(x2, y2, z2);
            double distance = p1.DistanceTo(p2);

            var result = new { distance, point1 = new { x = x1, y = y1, z = z1 }, point2 = new { x = x2, y = y2, z = z2 } };
            return new AIResult<object> { Success = true, Message = $"Distance: {distance:F4}", Response = result };
        }
    }
}
