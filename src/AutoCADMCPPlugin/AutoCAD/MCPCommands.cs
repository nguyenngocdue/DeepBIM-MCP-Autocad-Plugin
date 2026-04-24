using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using autocad_mcp_plugin.Core;

namespace autocad_mcp_plugin.AutoCAD
{
    /// <summary>
    /// AutoCAD commands for controlling the MCP server.
    /// Run in AutoCAD command line: MCPSTART, MCPSTOP, MCPSTATUS
    /// </summary>
    public class MCPCommands
    {
        /// <summary>Start or verify the MCP server connection.</summary>
        [CommandMethod("MCPSTART")]
        public void StartMcp()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                if (SocketService.Instance.IsRunning)
                {
                    ed.WriteMessage($"\n[DeepBim-MCP] Server already running on port {SocketService.Instance.Port}.\n");
                    return;
                }

                SocketService.Instance.Initialize();
                SocketService.Instance.Start();
                ed.WriteMessage($"\n[DeepBim-MCP] MCP Server started on port {SocketService.Instance.Port}.\n");
            }
            catch (global::System.Exception ex)
            {
                ed.WriteMessage($"\n[DeepBim-MCP] ERROR: {ex.Message}\n");
            }
        }

        /// <summary>Stop the MCP server.</summary>
        [CommandMethod("MCPSTOP")]
        public void StopMcp()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                if (!SocketService.Instance.IsRunning)
                {
                    ed.WriteMessage("\n[DeepBim-MCP] Server is not running.\n");
                    return;
                }

                SocketService.Instance.Stop();
                ed.WriteMessage("\n[DeepBim-MCP] MCP Server stopped.\n");
            }
            catch (global::System.Exception ex)
            {
                ed.WriteMessage($"\n[DeepBim-MCP] ERROR: {ex.Message}\n");
            }
        }

        /// <summary>Show current MCP server status.</summary>
        [CommandMethod("MCPSTATUS")]
        public void McpStatus()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            string status = SocketService.Instance.IsRunning
                ? $"RUNNING on port {SocketService.Instance.Port}"
                : "STOPPED";

            ed.WriteMessage($"\n[DeepBim-MCP] Status: {status}\n");
        }
    }
}
