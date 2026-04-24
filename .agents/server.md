# MCP Server — autocad-mcp-server

## Overview

The TypeScript/Node.js MCP server acts as a bridge between AI clients and the AutoCAD plugin.

```
AI Client (Claude / VS Code Copilot)
  │ MCP (stdio, JSON)
  ▼
autocad-mcp-server/        ← This project
  │ TCP JSON-RPC 2.0 (port 8180-8199)
  ▼
AutoCADMCPPlugin.dll       ← Running inside AutoCAD process
```

## Project Location

```
E:\C# Tool Revit\revit-mcp\autocad-mcp-server\
```

## Key Dependencies

| Package | Version | Notes |
|---------|---------|-------|
| `@modelcontextprotocol/sdk` | `1.27.1` | **PINNED** — 1.29.0 is missing `.d.ts` files |
| `zod` | `^3.23.8` | Schema validation for tool parameters |

**Important**: Do NOT upgrade `@modelcontextprotocol/sdk` above `1.27.1` — higher versions have broken type declarations.

## Build & Run

```bash
cd E:\C# Tool Revit\revit-mcp\autocad-mcp-server
npm install
npm run build        # compiles TypeScript → build/
npm start            # runs build/index.js
```

TypeScript config: `"module": "Node16"`, `"moduleResolution": "Node16"`.
All imports must use `.js` extension (e.g., `import { X } from "./foo.js"`).

## VS Code MCP Configuration

Location: `E:\C# Tool Revit\revit-mcp\mcp-addin\.vscode\mcp.json`

```json
{
  "servers": {
    "autocad": {
      "type": "stdio",
      "command": "node",
      "args": ["E:\\C# Tool Revit\\revit-mcp\\autocad-mcp-server\\build\\index.js"]
    }
  }
}
```

The server is a **stdio** server — VS Code manages the process lifecycle.

## Connection to AutoCAD

### Port Discovery

`ConnectionManager.withAutoCADConnection<T>()` scans ports 8180–8199 to find AutoCAD.
AutoCAD writes its active port to `%APPDATA%\DeepBim-MCP-ACAD\mcp-port.txt`.

Scan logic (simplified):
```typescript
for (let port = 8180; port <= 8199; port++) {
  try {
    const client = new AutoCADClientConnection(port);
    await client.connect();
    const result = await client.sendCommand("say_hello", {});
    if (result) return client;  // found it!
  } catch { continue; }
}
throw new Error("AutoCAD not found on ports 8180-8199");
```

### `withAutoCADConnection<T>(operation)`

```typescript
const response = await withAutoCADConnection(async (client) => {
  return await client.sendCommand("method_name", { param: "value" });
});
```

Handles: connect → run operation → disconnect. Throws on connection failure.

### `AutoCADClientConnection` (SocketClient.ts)

TCP JSON-RPC 2.0 client. Each call gets a unique `requestId` (UUID). Uses a callback map to correlate responses.

```typescript
const result = await client.sendCommand("get_layers", {});
// result is the parsed JSON-RPC "result" field (AIResult<T>)
```

## Entry Point (`src/index.ts`)

```typescript
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { registerTools } from "./tools/register.js";

const server = new McpServer({ name: "AutoCAD MCP Server", version: "1.0.0" });
registerTools(server);

const transport = new StdioServerTransport();
await server.connect(transport);
```

## Tool Registration (`src/tools/register.ts`)

Uses **static imports only** — do NOT use dynamic `import()` or `fs.readdirSync` + dynamic loading.
Reason: paths with `#` in directory names cause `ENAMETOOLONG` error on Windows.

```typescript
import { registerSayHelloTool }           from "./say_hello.js";
import { registerGetDocumentInfoTool }    from "./get_document_info.js";
import { registerGetLayersTool }          from "./get_layers.js";
import { registerGetEntitiesTool }        from "./get_entities.js";
import { registerCreateLineTool }         from "./create_line.js";
import { registerWriteMessageTool }       from "./write_message.js";
import { registerSendCodeToAutoCADTool }  from "./send_code_to_autocad.js";

export function registerTools(server: McpServer) {
  registerSayHelloTool(server);
  registerGetDocumentInfoTool(server);
  registerGetLayersTool(server);
  registerGetEntitiesTool(server);
  registerCreateLineTool(server);
  registerWriteMessageTool(server);
  registerSendCodeToAutoCADTool(server);
}
```

## Tool File Structure Template

```typescript
// src/tools/my_command.ts
import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withAutoCADConnection } from "../utils/ConnectionManager.js";

export function registerMyCommandTool(server: McpServer) {
  server.tool(
    "my_command",                           // must match C# CommandName
    "Description visible to AI agents.",    // important: be specific
    { myParam: z.string().describe("...") },
    async (args) => {
      try {
        const response = await withAutoCADConnection(async (client) => {
          return await client.sendCommand("my_command", args);
        });
        return { content: [{ type: "text" as const, text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text" as const, text: `Error: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
```

## Debugging

Server startup log (written before connecting transport):
```
%APPDATA%\DeepBim-MCP-ACAD\mcp-server-startup.log
```

AutoCAD plugin log:
```
%APPDATA%\DeepBim-MCP-ACAD\logs\acad-mcp_YYYYMMDD.log
```

If VS Code shows "Server disconnected" — check Node.js is installed and path in `mcp.json` is correct.

## Startup Log Write

`src/index.ts` writes a startup entry before connecting:
```typescript
const logPath = path.join(process.env.APPDATA!, "DeepBim-MCP-ACAD", "mcp-server-startup.log");
fs.appendFileSync(logPath, `[${new Date().toISOString()}] MCP Server starting...\n`);
```
This confirms the Node.js process is being invoked by VS Code.
