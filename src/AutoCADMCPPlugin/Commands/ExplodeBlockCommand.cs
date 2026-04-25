using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace autocad_mcp_plugin.Commands
{
    public class ExplodeBlockCommand : DocumentContextCommandBase
    {
        public override string CommandName => "explode_block";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string handleStr = parameters["handle"]?.ToString() ?? throw new System.ArgumentException("'handle' is required.");

            var db = doc.Database;
            var newHandles = new List<string>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var handle = new Handle(System.Convert.ToInt64(handleStr, 16));
                if (!db.TryGetObjectId(handle, out var objId))
                    return new AIResult<object> { Success = false, Message = $"Entity with handle '{handleStr}' not found." };

                var blkRef = tr.GetObject(objId, OpenMode.ForWrite) as BlockReference;
                if (blkRef == null)
                    return new AIResult<object> { Success = false, Message = "Entity is not a block reference." };

                var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                var modelSpace = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);

                var exploded = new DBObjectCollection();
                blkRef.Explode(exploded);

                foreach (DBObject obj in exploded)
                {
                    var entity = obj as Entity;
                    if (entity == null) continue;
                    modelSpace.AppendEntity(entity);
                    tr.AddNewlyCreatedDBObject(entity, true);
                    newHandles.Add(entity.Handle.ToString());
                }

                blkRef.Erase();
                tr.Commit();
            }

            return new AIResult<object>
            {
                Success = true,
                Message = $"Block exploded into {newHandles.Count} entities.",
                Response = new { count = newHandles.Count, handles = newHandles }
            };
        }
    }
}
