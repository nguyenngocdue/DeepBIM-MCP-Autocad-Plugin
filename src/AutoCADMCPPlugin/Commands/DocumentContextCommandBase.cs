using Autodesk.AutoCAD.ApplicationServices;
using autocad_mcp_plugin.Core;
using autocad_mcp_plugin.MCP.Interfaces;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    /// <summary>
    /// Base class for AutoCAD MCP commands that need document context.
    /// Equivalent to ExternalEventCommandBase from RevitMCPSDK.
    ///
    /// Usage: override ExecuteInDocumentContext(Document doc, JObject parameters)
    /// The base class marshals execution to AutoCAD main thread via DocumentContextQueue.
    /// </summary>
    public abstract class DocumentContextCommandBase : IAutoCADCommand
    {
        public abstract string CommandName { get; }

        /// <summary>Default timeout in milliseconds for waiting on document context.</summary>
        protected virtual int TimeoutMs => 30000;

        public object Execute(JObject parameters, string requestId)
        {
            return DocumentContextQueue.Instance.Enqueue(
                doc => ExecuteInDocumentContext(doc, parameters),
                TimeoutMs);
        }

        /// <summary>
        /// Implement the actual AutoCAD work here.
        /// This method is called on the AutoCAD main thread with a document lock.
        /// </summary>
        protected abstract object ExecuteInDocumentContext(Document doc, JObject parameters);
    }
}
