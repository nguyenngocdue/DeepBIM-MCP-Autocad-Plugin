using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace autocad_mcp_plugin.Commands
{
    /// <summary>
    /// Returns entities from model space of the current drawing.
    /// Equivalent of get_current_view_elements in the Revit plugin.
    ///
    /// Parameters:
    ///   limit  (int, optional, default 100) — max number of entities to return
    ///   layer  (string, optional)           — filter by layer name
    ///   type   (string, optional)           — filter by DXF type (e.g. "LINE", "CIRCLE")
    /// </summary>
    public class GetEntitiesCommand : DocumentContextCommandBase
    {
        public override string CommandName => "get_entities";
        protected override int TimeoutMs => 60000;

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            int limit      = parameters?["limit"]?.Value<int>()    ?? 100;
            string layer   = parameters?["layer"]?.ToString();
            string typeFilter = parameters?["type"]?.ToString()?.ToUpperInvariant();

            var db = doc.Database;
            var entities = new List<object>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                var modelSpace = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForRead);

                int count = 0;
                foreach (ObjectId id in modelSpace)
                {
                    if (count >= limit) break;

                    var entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity == null) continue;

                    if (!string.IsNullOrEmpty(layer) && entity.Layer != layer) continue;
                    if (!string.IsNullOrEmpty(typeFilter) && entity.GetRXClass().DxfName?.ToUpperInvariant() != typeFilter) continue;

                    entities.Add(new
                    {
                        objectId = id.ToString(),
                        type     = entity.GetType().Name,
                        dxfType  = entity.GetRXClass().DxfName,
                        layer    = entity.Layer,
                        color    = entity.Color.ColorNameForDisplay,
                        handle   = entity.Handle.ToString()
                    });
                    count++;
                }

                tr.Commit();
            }

            return new AIResult<List<object>>
            {
                Success  = true,
                Message  = $"Retrieved {entities.Count} entity(s) from model space.",
                Response = entities
            };
        }
    }
}
