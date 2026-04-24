using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using autocad_mcp_plugin.Core;
using autocad_mcp_plugin.Utils;
using System;
using System.IO;
using System.Reflection;

// Register this assembly as an AutoCAD extension application
[assembly: ExtensionApplication(typeof(autocad_mcp_plugin.AutoCAD.AutoCADMCPApplication))]

namespace autocad_mcp_plugin.AutoCAD
{
    /// <summary>
    /// AutoCAD entry point — equivalent of Revit's IExternalApplication.
    /// Loaded automatically when the DLL is loaded with NETLOAD.
    /// </summary>
    public class AutoCADMCPApplication : IExtensionApplication
    {
        public void Initialize()
        {
            try
            {
                // Resolve plugin directory from DLL location
                string location = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(location))
                {
                    string dir = Path.GetDirectoryName(location);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        PathManager.SetPluginDirectory(dir);
                }

                var logger = new Logger();
                logger.Info("AutoCAD MCP Plugin initializing...");

                SocketService.Instance.Initialize();

                // Auto-start MCP server on load
                SocketService.Instance.Start();

                // Create ribbon tab once the AutoCAD UI is ready
                // Use ComponentManager.ItemInitialized for reliable ribbon detection
                if (Autodesk.Windows.ComponentManager.Ribbon != null)
                {
                    MCPRibbon.CreateRibbon();
                }
                else
                {
                    Autodesk.Windows.ComponentManager.ItemInitialized += OnRibbonItemInitialized;
                }

                // Write status to AutoCAD command line
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor.WriteMessage(
                    $"\n[DeepBim-MCP] AutoCAD MCP Plugin loaded." +
                    $"\n  TCP  port : {SocketService.Instance.Port}" +
                    $"\n  HTTP port : {SocketService.Instance.HttpPort}\n");
            }
            catch (System.Exception ex)
            {
                try
                {
                    var doc = Application.DocumentManager.MdiActiveDocument;
                    doc?.Editor.WriteMessage($"\n[DeepBim-MCP] ERROR during initialization: {ex.Message}\n");
                }
                catch { }
            }
        }

        public void Terminate()
        {
            try
            {
                if (SocketService.Instance.IsRunning)
                    SocketService.Instance.Stop();
            }
            catch { }
        }

        private static void OnRibbonItemInitialized(object sender, Autodesk.Windows.RibbonItemEventArgs e)
        {
            if (Autodesk.Windows.ComponentManager.Ribbon == null) return;
            Autodesk.Windows.ComponentManager.ItemInitialized -= OnRibbonItemInitialized;
            MCPRibbon.CreateRibbon();
        }
    }
}
