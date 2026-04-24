using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace autocad_mcp_plugin.AutoCAD
{
    /// <summary>
    /// Creates a "DeepBim MCP" ribbon tab with Start / Stop / Status buttons.
    /// Called from AutoCADMCPApplication.Initialize().
    /// </summary>
    public static class MCPRibbon
    {
        private const string TabId    = "DeepBimMCP_Tab";
        private const string PanelId  = "DeepBimMCP_Panel";

        public static void CreateRibbon()
        {
            try
            {
                var ribCtrl = ComponentManager.Ribbon;
                if (ribCtrl == null) return;

                // Remove existing tab to avoid duplicates on reload
                var existingTab = ribCtrl.FindTab(TabId);
                if (existingTab != null)
                    ribCtrl.Tabs.Remove(existingTab);

                // ── Tab ──────────────────────────────────────────────────────
                var tab = new RibbonTab
                {
                    Title = "DeepBim MCP",
                    Id    = TabId,
                    IsActive = false
                };
                ribCtrl.Tabs.Add(tab);

                // ── Panel ─────────────────────────────────────────────────────
                var panelSource = new RibbonPanelSource
                {
                    Title = "AutoCAD MCP",
                    Id    = PanelId
                };
                var panel = new RibbonPanel { Source = panelSource };
                tab.Panels.Add(panel);

                // ── Row: Start + Stop buttons side by side ────────────────────
                var rowStart = new RibbonRowPanel();
                rowStart.Items.Add(MakeButton(
                    id:      "btn_mcpstart",
                    label:   "Start MCP",
                    tooltip: "Start the DeepBim MCP Server (MCPSTART)",
                    command: "MCPSTART",
                    color:   Colors.LimeGreen));

                rowStart.Items.Add(new RibbonRowBreak());

                rowStart.Items.Add(MakeButton(
                    id:      "btn_mcpstop",
                    label:   "Stop MCP",
                    tooltip: "Stop the DeepBim MCP Server (MCPSTOP)",
                    command: "MCPSTOP",
                    color:   Colors.OrangeRed));

                panelSource.Items.Add(rowStart);
                panelSource.Items.Add(new RibbonSeparator());

                // ── Status button ─────────────────────────────────────────────
                panelSource.Items.Add(MakeButton(
                    id:      "btn_mcpstatus",
                    label:   "MCP Status",
                    tooltip: "Show MCP Server status (MCPSTATUS)",
                    command: "MCPSTATUS",
                    color:   Colors.DodgerBlue,
                    size:    RibbonItemSize.Large));
            }
            catch (System.Exception ex)
            {
                try
                {
                    var doc = Application.DocumentManager.MdiActiveDocument;
                    doc?.Editor.WriteMessage($"\n[DeepBim-MCP] Ribbon init warning: {ex.Message}\n");
                }
                catch { }
            }
        }

        private static RibbonButton MakeButton(
            string id, string label, string tooltip,
            string command, Color color,
            RibbonItemSize size = RibbonItemSize.Standard)
        {
            return new RibbonButton
            {
                Id              = id,
                Name            = id,
                Text            = label,
                ToolTip         = new RibbonToolTip
                {
                    Title   = label,
                    Content = tooltip
                },
                CommandHandler  = new RibbonCommandHandler(command),
                LargeImage      = CreateColorIcon(color, 32),
                Image           = CreateColorIcon(color, 16),
                Size            = size,
                Orientation     = System.Windows.Controls.Orientation.Vertical,
                ShowText        = true
            };
        }

        /// <summary>Creates a solid-color square bitmap as ribbon icon.</summary>
        private static ImageSource CreateColorIcon(Color color, int size)
        {
            var bmp = new WriteableBitmap(size, size, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
            var pixels = new byte[size * size * 4];
            for (int i = 0; i < size * size; i++)
            {
                pixels[i * 4 + 0] = color.B;
                pixels[i * 4 + 1] = color.G;
                pixels[i * 4 + 2] = color.R;
                pixels[i * 4 + 3] = color.A;
            }
            bmp.WritePixels(new System.Windows.Int32Rect(0, 0, size, size), pixels, size * 4, 0);
            return bmp;
        }
    }

    /// <summary>Executes an AutoCAD command string when ribbon button is clicked.</summary>
    internal class RibbonCommandHandler : System.Windows.Input.ICommand
    {
        private readonly string _command;

        public RibbonCommandHandler(string command)
        {
            _command = command;
        }

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.SendStringToExecute(_command + " ", true, false, true);
        }
    }
}
