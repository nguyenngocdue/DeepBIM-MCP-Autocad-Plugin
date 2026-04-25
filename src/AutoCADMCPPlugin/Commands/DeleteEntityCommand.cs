using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class DeleteEntityCommand : DocumentContextCommandBase
    {
        public override string CommandName => "delete_entity";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string handleStr = parameters["handle"]?.ToString() ?? throw new System.ArgumentException("'handle' is required.");

            var db = doc.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var handle = new Handle(System.Convert.ToInt64(handleStr, 16));
                if (!db.TryGetObjectId(handle, out var objId))
                    return new AIResult<string> { Success = false, Message = $"Entity with handle '{handleStr}' not found.", Response = handleStr };

                var entity = (Entity)tr.GetObject(objId, OpenMode.ForWrite);
                entity.Erase();
                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = "Entity deleted.", Response = handleStr };
        }
    }
}
