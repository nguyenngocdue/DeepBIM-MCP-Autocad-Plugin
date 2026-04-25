using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class ExportPdfCommand : DocumentContextCommandBase
    {
        public override string CommandName => "export_pdf";
        protected override int TimeoutMs => 60000;

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string outputPath = parameters["outputPath"]?.ToString()
                ?? throw new System.ArgumentException("'outputPath' is required.");
            string layoutName = parameters["layout"]?.ToString();

            var db = doc.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // Find layout
                var layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                ObjectId layoutId = ObjectId.Null;

                if (!string.IsNullOrEmpty(layoutName))
                {
                    if (!layoutDict.Contains(layoutName))
                        return new AIResult<string> { Success = false, Message = $"Layout '{layoutName}' not found." };
                    layoutId = layoutDict.GetAt(layoutName);
                }
                else
                {
                    // Use current layout
                    layoutId = db.CurrentSpaceId;
                }

                var ps = PlotSettingsValidator.Current;
                var plotSettings = new PlotSettings(true);
                ps.RefreshLists(plotSettings);
                ps.SetPlotConfigurationName(plotSettings, "DWG To PDF.pc3", null);
                ps.SetPlotWindowArea(plotSettings, new Autodesk.AutoCAD.Geometry.Point2d(0, 0), new Autodesk.AutoCAD.Geometry.Point2d(0, 0));
                ps.SetPlotType(plotSettings, PlotType.Extents);
                ps.SetUseStandardScale(plotSettings, true);
                ps.SetStdScaleType(plotSettings, StdScaleType.ScaleToFit);
                ps.SetPlotCentered(plotSettings, true);

                var pi = new PlotInfo();
                pi.Layout = layoutId;
                pi.OverrideSettings = plotSettings;

                var piv = new PlotInfoValidator { MediaMatchingPolicy = MatchingPolicy.MatchEnabled };
                piv.Validate(pi);

                using (var pe = PlotEngine.OpenPlotEngine())
                using (var prog = pe.BeginDocument(pi, doc.Name, null, 1, true, outputPath))
                {
                    var ppi = new PlotPageInfo();
                    prog.BeginPage(ppi, pi, true, null);
                    prog.BeginGenerateGraphics(null);
                    prog.EndGenerateGraphics(null);
                    prog.EndPage(null);
                    prog.EndDocument(null);
                }

                tr.Commit();
            }

            return new AIResult<string> { Success = true, Message = $"PDF exported to '{outputPath}'.", Response = outputPath };
        }
    }
}
