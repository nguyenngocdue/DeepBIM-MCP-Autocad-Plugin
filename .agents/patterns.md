# Key Patterns — autocad-addin

## Adding a New MCP Command (3 steps)

### Step 1 — C# Command class

Create `autocad-addin/src/AutoCADMCPPlugin/Commands/MyCommand.cs`:

```csharp
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using autocad_mcp_plugin.Models;
using Newtonsoft.Json.Linq;

namespace autocad_mcp_plugin.Commands
{
    public class MyCommand : DocumentContextCommandBase
    {
        public override string CommandName => "my_command";

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            // 'doc' is the active Document, already locked.
            // db = doc.Database for database access.
            var db = doc.Database;

            // Parse parameters
            string param = parameters?["myParam"]?.ToString() ?? "default";

            // Do AutoCAD work inside a Transaction
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // ... AutoCAD API calls ...
                tr.Commit();
            }

            return new AIResult<object>
            {
                Success  = true,
                Message  = "Done.",
                Response = new { result = param }
            };
        }
    }
}
```

That's it — the command is **auto-discovered** on next NETLOAD. No config changes needed.

### Step 2 — TypeScript tool

Create `autocad-mcp-server/src/tools/my_command.ts`:

```typescript
import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withAutoCADConnection } from "../utils/ConnectionManager.js";

export function registerMyCommandTool(server: McpServer) {
  server.tool(
    "my_command",
    "Description shown to AI. Be specific about what it does and what params mean.",
    {
      myParam: z.string().describe("What this parameter controls."),
    },
    async (args) => {
      try {
        const response = await withAutoCADConnection(async (client) => {
          return await client.sendCommand("my_command", args);
        });
        return { content: [{ type: "text" as const, text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text" as const, text: `my_command failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
```

### Step 3 — Register in server

Add to `autocad-mcp-server/src/tools/register.ts`:

```typescript
import { registerMyCommandTool } from "./my_command.js";
// ...
export function registerTools(server: McpServer) {
  // ... existing ...
  registerMyCommandTool(server);
}
```

Then rebuild: `cd autocad-mcp-server && npm run build`

---

## DocumentContextQueue Pattern (Thread Bridge)

AutoCAD has no `ExternalEvent` API. The plugin uses `Application.Idle` + `ConcurrentQueue`:

```
Background thread (TCP socket)
  → command.Execute(params)
  → DocumentContextQueue.Instance.Enqueue(doc => { ... }, timeoutMs)
      → WorkItem enqueued, background thread BLOCKS on ManualResetEventSlim.Wait()

AutoCAD main thread (Application.Idle event fires)
  → OnIdle() drains ConcurrentQueue
  → doc.LockDocument()
  → item.Action(doc) ← executes the lambda
  → item.CompletedEvent.Set() ← unblocks background thread
```

`DocumentContextCommandBase` wraps this automatically — just override `ExecuteInDocumentContext`.

**Important:** `DocumentContextQueue.Initialize(logger)` must be called once on the AutoCAD main thread (done in `SocketService.Initialize()` which is called from `AutoCADMCPApplication.Initialize()`).

---

## JSON-RPC Flow (End to End)

```
VS Code / Claude sends tool call
  → autocad-mcp-server (Node.js, stdio)
  → ConnectionManager.withAutoCADConnection()
  → finds AutoCAD on port 8180-8199
  → AutoCADClientConnection.sendCommand("method", params)
  → sends: {"jsonrpc":"2.0","method":"say_hello","params":{...},"id":"xxx"}

AutoCAD TCP socket (SocketService, background thread)
  → CommandExecutor.Execute(request)
  → AutoCADCommandRegistry.TryGetCommand("say_hello")
  → command.Execute(params, requestId)
  → [if DocumentContextCommandBase] → DocumentContextQueue.Enqueue(doc => ...)
  → [blocks until AutoCAD main thread completes]
  → returns result object
  → sends: {"jsonrpc":"2.0","id":"xxx","result":{...}}
```

---

## AIResult<T> Response Shape

All commands return `AIResult<T>` serialized as JSON:

```json
{
  "success": true,
  "message": "Human-readable status",
  "response": { /* T — the actual data */ }
}
```

On error:
```json
{
  "success": false,
  "message": "Error description",
  "response": null
}
```

---

## Commands That Don't Need Document Context

If your command doesn't touch the AutoCAD database, implement `IAutoCADCommand` directly (no `DocumentContextCommandBase`):

```csharp
public class SayHelloCommand : IAutoCADCommand
{
    public string CommandName => "say_hello";

    public object Execute(JObject parameters, string requestId)
    {
        string msg = parameters?["message"]?.ToString() ?? "Hello MCP!";
        return new AIResult<object>
        {
            Success  = true,
            Message  = "Hello from AutoCAD!",
            Response = new { greeting = msg, timestamp = DateTime.Now }
        };
    }
}
```

---

## Working with AutoCAD Transactions

Always use `db.TransactionManager.StartTransaction()`. Entities must be added with `tr.AddNewlyCreatedDBObject()`:

```csharp
using (var tr = db.TransactionManager.StartTransaction())
{
    // Get model space for write
    var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
    var modelSpace = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);

    // Create entity
    var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
    modelSpace.AppendEntity(line);
    tr.AddNewlyCreatedDBObject(line, true);  // required!

    tr.Commit();  // required — no commit = rollback
}
```

To read entities from model space:
```csharp
using (var tr = db.TransactionManager.StartTransaction())
{
    var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
    var modelSpace = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForRead);

    foreach (ObjectId id in modelSpace)
    {
        var entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
        if (entity == null) continue;
        // use entity...
    }
    tr.Commit();
}
```

---

## Common Namespaces

```csharp
using Autodesk.AutoCAD.ApplicationServices;  // Application, Document
using Autodesk.AutoCAD.DatabaseServices;     // Database, Transaction, Entity, Line, BlockTable, etc.
using Autodesk.AutoCAD.Geometry;             // Point3d, Vector3d
using Autodesk.AutoCAD.EditorInput;          // Editor (for WriteMessage)
using Autodesk.AutoCAD.Runtime;              // CommandMethod, ExtensionApplication
```
