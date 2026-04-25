using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class SetCurrentLayerCommand : DocumentContextCommandBase
    {
        public override string CommandName => "set_current_layer";

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

                db.Clayer = lt[name];
                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = $"Current layer set to '{name}'.", Response = name };
        }
    }
}
