using Autodesk.AutoCAD.ApplicationServices;
using autocad_mcp_plugin.MCP.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace autocad_mcp_plugin.Core
{
    /// <summary>
    /// AutoCAD equivalent of Revit's ExternalEventManager.
    ///
    /// AutoCAD does not have a built-in ExternalEvent mechanism.
    /// This class uses Application.Idle + a ConcurrentQueue to marshal
    /// work items from background threads (socket listener) onto the
    /// AutoCAD document context (main thread).
    ///
    /// Usage from background thread:
    ///   var result = DocumentContextQueue.Instance.Enqueue(doc => { ... return value; }, timeout);
    /// </summary>
    public class DocumentContextQueue
    {
        private static DocumentContextQueue _instance;
        public static DocumentContextQueue Instance =>
            _instance ?? (_instance = new DocumentContextQueue());

        private readonly ConcurrentQueue<WorkItem> _queue = new ConcurrentQueue<WorkItem>();
        private bool _hooked;
        private ILogger _logger;

        private DocumentContextQueue() { }

        public void Initialize(ILogger logger)
        {
            _logger = logger;
            if (!_hooked)
            {
                Application.Idle += OnIdle;
                _hooked = true;
                _logger?.Info("DocumentContextQueue initialized (Idle hook registered).");
            }
        }

        public void Shutdown()
        {
            if (_hooked)
            {
                Application.Idle -= OnIdle;
                _hooked = false;
            }
        }

        /// <summary>
        /// Enqueue a function to be executed on the AutoCAD main thread.
        /// Blocks the calling (background) thread until execution completes or times out.
        /// </summary>
        /// <param name="action">Function receiving the active Document; returns a result object.</param>
        /// <param name="timeoutMs">Timeout in milliseconds.</param>
        public object Enqueue(Func<Document, object> action, int timeoutMs = 30000)
        {
            var item = new WorkItem(action);
            _queue.Enqueue(item);
            bool completed = item.CompletedEvent.Wait(timeoutMs);
            if (!completed)
                throw new TimeoutException("AutoCAD document context action timed out.");
            if (item.Exception != null)
                throw new System.Exception($"AutoCAD action failed: {item.Exception.Message}", item.Exception);
            return item.Result;
        }

        private void OnIdle(object sender, EventArgs e)
        {
            // Drain the queue on AutoCAD main thread
            while (_queue.TryDequeue(out var item))
            {
                try
                {
                    var doc = Application.DocumentManager.MdiActiveDocument;
                    if (doc == null)
                    {
                        item.Exception = new InvalidOperationException("No active AutoCAD document.");
                        item.CompletedEvent.Set();
                        continue;
                    }

                    using (doc.LockDocument())
                    {
                        item.Result = item.Action(doc);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error("DocumentContextQueue action error: {0}", ex.Message);
                    item.Exception = ex;
                }
                finally
                {
                    item.CompletedEvent.Set();
                }
            }
        }

        private class WorkItem
        {
            public Func<Document, object> Action { get; }
            public object Result { get; set; }
            public System.Exception Exception { get; set; }
            public ManualResetEventSlim CompletedEvent { get; } = new ManualResetEventSlim(false);

            public WorkItem(Func<Document, object> action)
            {
                Action = action;
            }
        }
    }
}
