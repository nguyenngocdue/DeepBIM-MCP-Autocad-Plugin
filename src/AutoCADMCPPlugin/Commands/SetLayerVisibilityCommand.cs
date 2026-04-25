using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class SetLayerVisibilityCommand : DocumentContextCommandBase
    {
        public override string CommandName => "set_layer_visibility";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string name = parameters["name"]?.ToString()
                ?? throw new System.ArgumentException("'name' is required.");
            bool visible = parameters["visible"]?.Value<bool>()
                ?? throw new System.ArgumentException("'visible' is required.");

            var db = doc.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(name))
                    return new AIResult<string> { Success = false, Message = $"Layer '{name}' does not exist.", Response = name };

                var layer = (LayerTableRecord)tr.GetObject(lt[name], OpenMode.ForWrite);
                layer.IsOff = !visible;
                tr.Commit();
            }

            string state = visible ? "ON" : "OFF";
            return new AIResult<string> { Success = true, Message = $"Layer '{name}' turned {state}.", Response = name };
        }
    }
}
