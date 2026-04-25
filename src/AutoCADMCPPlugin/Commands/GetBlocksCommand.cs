using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace autocad_mcp_plugin.Commands
{
    public class GetBlocksCommand : DocumentContextCommandBase
    {
        public override string CommandName => "get_blocks";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            var db = doc.Database;
            var blocks = new List<object>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId id in bt)
                {
                    var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (btr.IsLayout || btr.IsAnonymous) continue;

                    int count = 0;
                    foreach (ObjectId _ in btr) count++;

                    blocks.Add(new
                    {
                        name          = btr.Name,
                        entityCount   = count,
                        hasAttributes = btr.HasAttributeDefinitions,
                        origin        = new { x = btr.Origin.X, y = btr.Origin.Y, z = btr.Origin.Z }
                    });
                }
                tr.Commit();
            }

            return new AIResult<List<object>> { Success = true, Message = $"Retrieved {blocks.Count} block(s).", Response = blocks };
        }
    }
}
