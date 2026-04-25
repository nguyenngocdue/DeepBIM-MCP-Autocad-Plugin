using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class DeleteLayerCommand : DocumentContextCommandBase
    {
        public override string CommandName => "delete_layer";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string name = parameters["name"]?.ToString()
                ?? throw new System.ArgumentException("'name' is required.");

            var db = doc.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(name))
                    return new AIResult<string> { Success = false, Message = $"Layer '{name}' does not exist.", Response = name };

                var layerId = lt[name];
                if (layerId == db.Clayer)
                    return new AIResult<string> { Success = false, Message = $"Cannot delete the current layer '{name}'.", Response = name };

                var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForWrite);
                layer.Erase();
                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = $"Layer '{name}' deleted.", Response = name };
        }
    }
}
