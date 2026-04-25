using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class InsertBlockCommand : DocumentContextCommandBase
    {
        public override string CommandName => "insert_block";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string blockName = parameters["blockName"]?.ToString() ?? throw new System.ArgumentException("'blockName' is required.");
            double x = parameters["x"]?.Value<double>() ?? throw new System.ArgumentException("'x' is required.");
            double y = parameters["y"]?.Value<double>() ?? throw new System.ArgumentException("'y' is required.");
            double z = parameters["z"]?.Value<double>() ?? 0.0;
            double scaleX = parameters["scaleX"]?.Value<double>() ?? 1.0;
            double scaleY = parameters["scaleY"]?.Value<double>() ?? 1.0;
            double scaleZ = parameters["scaleZ"]?.Value<double>() ?? 1.0;
            double rotation = parameters["rotation"]?.Value<double>() ?? 0.0;
            string layer = parameters["layer"]?.ToString();

            var db = doc.Database;
            string handle;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(blockName))
                    return new AIResult<string> { Success = false, Message = $"Block '{blockName}' not found in drawing." };

                var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                var modelSpace = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);

                var blkRef = new BlockReference(new Point3d(x, y, z), bt[blockName])
                {
                    ScaleFactors = new Scale3d(scaleX, scaleY, scaleZ),
                    Rotation = rotation * System.Math.PI / 180.0
                };
                if (!string.IsNullOrEmpty(layer)) blkRef.Layer = layer;

                modelSpace.AppendEntity(blkRef);
                tr.AddNewlyCreatedDBObject(blkRef, true);
                handle = blkRef.Handle.ToString();
                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = $"Block '{blockName}' inserted.", Response = handle };
        }
    }
}
