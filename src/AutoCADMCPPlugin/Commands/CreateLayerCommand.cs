using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class CreateLayerCommand : DocumentContextCommandBase
    {
        public override string CommandName => "create_layer";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string name = parameters["name"]?.ToString()
                ?? throw new System.ArgumentException("'name' is required.");

            var db = doc.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (lt.Has(name))
                    return new AIResult<string> { Success = false, Message = $"Layer '{name}' already exists.", Response = name };

                var layer = new LayerTableRecord { Name = name };

                int? colorIndex = parameters["color"]?.Value<int?>();
                if (colorIndex.HasValue)
                    layer.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)colorIndex.Value);

                string linetype = parameters["linetype"]?.ToString();
                if (!string.IsNullOrEmpty(linetype))
                {
                    var ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                    if (ltt.Has(linetype))
                        layer.LinetypeObjectId = ltt[linetype];
                }

                double? lw = parameters["lineweight"]?.Value<double?>();
                if (lw.HasValue)
                    layer.LineWeight = (LineWeight)(int)lw.Value;

                string description = parameters["description"]?.ToString();
                if (!string.IsNullOrEmpty(description))
                    layer.Description = description;

                lt.UpgradeOpen();
                lt.Add(layer);
                tr.AddNewlyCreatedDBObject(layer, true);
                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = $"Layer '{name}' created.", Response = name };
        }
    }
}
