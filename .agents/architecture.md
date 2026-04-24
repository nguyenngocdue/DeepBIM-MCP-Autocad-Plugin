# Architecture — autocad-addin

## Repository Layout

```
E:\C# Tool Revit\revit-mcp\
├── autocad-addin/                          ← THIS REPO (mcp-addin/autocad-addin/)
│   ├── AGENT.md
│   ├── command.json                        ← Command manifest (unused at runtime — commands auto-discovered)
│   ├── autocad-addin.sln
│   └── src/
│       └── AutoCADMCPPlugin/               ← Single C# project (net48, x64)
│           ├── AutoCADMCPPlugin.csproj
│           ├── AutoCAD/
│           │   ├── AutoCADMCPApplication.cs  ← IExtensionApplication entry point
│           │   └── MCPCommands.cs            ← [CommandMethod] MCPSTART / MCPSTOP / MCPSTATUS
│           ├── Core/
│           │   ├── SocketService.cs          ← TCP listener port 8180, JSON-RPC dispatcher
│           │   ├── CommandExecutor.cs        ← Routes request.method → IAutoCADCommand
│           │   ├── AutoCADCommandRegistry.cs ← Dictionary<string, IAutoCADCommand>
│           │   ├── CommandManager.cs         ← Loads commands from external DLLs (optional)
│           │   └── DocumentContextQueue.cs   ← Thread bridge: background → AutoCAD main thread
│           ├── Commands/
│           │   ├── DocumentContextCommandBase.cs  ← Abstract base for all doc-context commands
│           │   ├── SayHelloCommand.cs             ← say_hello
│           │   ├── GetDocumentInfoCommand.cs      ← get_document_info
│           │   ├── GetLayersCommand.cs             ← get_layers
│           │   ├── GetEntitiesCommand.cs           ← get_entities
│           │   ├── CreateLineCommand.cs            ← create_line
│           │   ├── WriteMessageCommand.cs          ← write_message
│           │   └── SendCodeToAutoCADCommand.cs     ← send_code_to_autocad
│           ├── Configuration/
│           │   ├── ConfigurationManager.cs   ← Loads commandRegistry.json or command.json
│           │   ├── FrameworkConfig.cs         ← Root config model
│           │   ├── CommandConfig.cs           ← Per-command config: name, assemblyPath, enabled
│           │   └── ServiceSettings.cs         ← Port (8180), LogLevel
│           ├── MCP/
│           │   ├── Interfaces/
│           │   │   ├── IAutoCADCommand.cs     ← Execute(JObject params, string requestId) : object
│           │   │   ├── ICommandRegistry.cs
│           │   │   └── ILogger.cs
│           │   └── Models/
│           │       └── JsonRPCModels.cs       ← Request, SuccessResponse, ErrorResponse, ErrorCodes
│           ├── Models/
│           │   └── AIResult.cs               ← AIResult<T> { bool Success; string Message; T Response }
│           └── Utils/
│               ├── Logger.cs                 ← Writes to %APPDATA%\DeepBim-MCP-ACAD\logs\
│               └── PathManager.cs            ← AppData folder, port file, logs dir

└── autocad-mcp-server/                     ← TypeScript MCP Server (see server.md)
    ├── package.json                        ← @modelcontextprotocol/sdk@1.27.1, zod
    ├── tsconfig.json                       ← module: Node16
    └── src/
        ├── index.ts                        ← McpServer + StdioServerTransport entry
        ├── utils/
        │   ├── SocketClient.ts             ← AutoCADClientConnection (TCP JSON-RPC)
        │   └── ConnectionManager.ts        ← Port scan 8180-8199, withAutoCADConnection<T>()
        └── tools/
            ├── register.ts                 ← Static imports, calls all registerXxxTool()
            ├── say_hello.ts
            ├── get_document_info.ts
            ├── get_layers.ts
            ├── get_entities.ts
            ├── create_line.ts
            ├── write_message.ts
            └── send_code_to_autocad.ts
```

## C# Project Details

- **Target**: `net48` (.NET Framework 4.8) — required by AutoCAD 2024
- **Platform**: `x64`
- **AutoCAD DLL references** (Private=false, not copied to output):
  - `C:\Program Files\Autodesk\AutoCAD 2024\acdbmgd.dll` — Database API
  - `C:\Program Files\Autodesk\AutoCAD 2024\acmgd.dll` — Application API
  - `C:\Program Files\Autodesk\AutoCAD 2024\accoremgd.dll` — Core API
- **NuGet**: `Newtonsoft.Json 13.0.3`, `Microsoft.CSharp 4.7.0`

## Runtime Paths

| What | Path |
|------|------|
| AppData folder | `%APPDATA%\DeepBim-MCP-ACAD\` |
| Log files | `%APPDATA%\DeepBim-MCP-ACAD\logs\acad-mcp_YYYYMMDD.log` |
| Startup log | `%APPDATA%\DeepBim-MCP-ACAD\mcp-server-startup.log` |
| Port file | `%APPDATA%\DeepBim-MCP-ACAD\mcp-port.txt` |
| TCP port range | 8180–8199 (default 8180) |

## Command Auto-Discovery

Commands are **not** loaded from `command.json` at runtime. `SocketService.Initialize()` calls
`RegisterCommandsFromCurrentAssembly()` which scans `Assembly.GetExecutingAssembly()` for all
non-abstract types implementing `IAutoCADCommand` and registers them automatically.

This means: **adding a new command = add one `.cs` file implementing `IAutoCADCommand`** — no config changes needed.
