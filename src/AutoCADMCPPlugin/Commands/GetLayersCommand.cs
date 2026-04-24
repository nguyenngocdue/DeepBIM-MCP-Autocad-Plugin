using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace autocad_mcp_plugin.Commands
{
    /// <summary>
    /// Returns all layers in the current drawing.
    /// No direct Revit equivalent (Revit uses categories instead of layers).
    /// </summary>
    public class GetLayersCommand : DocumentContextCommandBase
    {
        public override string CommandName => "get_layers";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            var db = doc.Database;
            var layers = new List<object>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId layerId in layerTable)
                {
                    var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
                    layers.Add(new
                    {
                        name        = layer.Name,
                        isOff       = layer.IsOff,
                        isFrozen    = layer.IsFrozen,
                        isLocked    = layer.IsLocked,
                        color       = layer.Color.ColorNameForDisplay,
                        lineWeight  = layer.LineWeight.ToString(),
                        description = layer.Description
                    });
                }
                tr.Commit();
            }

            return new AIResult<List<object>>
            {
                Success  = true,
                Message  = $"Retrieved {layers.Count} layer(s).",
                Response = layers
            };
        }
    }
}
