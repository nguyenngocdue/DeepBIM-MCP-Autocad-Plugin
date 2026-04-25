using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class CreateTextCommand : DocumentContextCommandBase
    {
        public override string CommandName => "create_text";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string text   = parameters["text"]?.ToString() ?? throw new System.ArgumentException("'text' is required.");
            double x      = parameters["x"]?.Value<double>() ?? throw new System.ArgumentException("'x' is required.");
            double y      = parameters["y"]?.Value<double>() ?? throw new System.ArgumentException("'y' is required.");
            double z      = parameters["z"]?.Value<double>() ?? 0.0;
            double height = parameters["height"]?.Value<double>() ?? 2.5;
            double rotation = parameters["rotation"]?.Value<double>() ?? 0.0;
            string layer  = parameters["layer"]?.ToString();
            string style  = parameters["style"]?.ToString() ?? "Standard";

            var db = doc.Database;
            string handle;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                var modelSpace = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);

                var dbText = new DBText
                {
                    Position = new Point3d(x, y, z),
                    TextString = text,
                    Height = height,
                    Rotation = rotation * System.Math.PI / 180.0
                };

                var tt = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                if (tt.Has(style)) dbText.TextStyleId = tt[style];
                if (!string.IsNullOrEmpty(layer)) dbText.Layer = layer;

                modelSpace.AppendEntity(dbText);
                tr.AddNewlyCreatedDBObject(dbText, true);
                handle = dbText.Handle.ToString();
                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = "Text created.", Response = handle };
        }
    }
}
