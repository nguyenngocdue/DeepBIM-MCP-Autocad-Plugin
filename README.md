# AutoCAD MCP Plugin

AutoCAD .NET plugin that exposes AutoCAD functionality via the MCP (Model Context Protocol) over TCP/HTTP, allowing AI assistants (Claude, etc.) to interact with AutoCAD.

Ported from [revit-mcp-plugin](../revit-mcp-plugin) — see [PORTING_NOTES.md](./PORTING_NOTES.md) for mapping details.

---

## Architecture

```
AutoCADMCPPlugin/
  AutoCAD/
    AutoCADMCPApplication.cs   ← IExtensionApplication entry point (Initialize/Terminate)
    MCPCommands.cs             ← [CommandMethod] MCPSTART / MCPSTOP / MCPSTATUS
  Core/
    SocketService.cs           ← TCP (8180) + HTTP (9180) JSON-RPC server
    CommandExecutor.cs         ← Dispatches JSON-RPC → IAutoCADCommand
    AutoCADCommandRegistry.cs  ← Registry of available commands
    CommandManager.cs          ← Loads commands from config / assembly scan
    DocumentContextQueue.cs    ← Thread-safe bridge (Application.Idle + ConcurrentQueue)
  Commands/
    DocumentContextCommandBase.cs  ← Base class for commands needing document access
    SayHelloCommand.cs
    GetDocumentInfoCommand.cs
    GetLayersCommand.cs
    GetEntitiesCommand.cs
    CreateLineCommand.cs
    WriteMessageCommand.cs
    SendCodeToAutoCADCommand.cs
  Configuration/
    ConfigurationManager.cs
    CommandConfig.cs / FrameworkConfig.cs / ServiceSettings.cs
  MCP/
    Interfaces/  ← IAutoCADCommand, ICommandRegistry, ILogger
    Models/      ← JsonRPCRequest/Response (no external SDK dependency)
  Models/
    AIResult.cs
  Utils/
    Logger.cs / PathManager.cs
```

---

## Prerequisites

### AutoCAD SDK DLLs

This project requires AutoCAD .NET managed DLLs. They are **not on NuGet** — you must reference them from your AutoCAD installation:

| DLL | Location |
|-----|----------|
| `acdbmgd.dll` | `C:\Program Files\Autodesk\AutoCAD <version>\` |
| `acmgd.dll` | `C:\Program Files\Autodesk\AutoCAD <version>\` |
| `AcCoreMgd.dll` | `C:\Program Files\Autodesk\AutoCAD <version>\` |

**Configure the path** in the `.csproj`:
```xml
<AutoCADInstallDir>C:\Program Files\Autodesk\AutoCAD 2025</AutoCADInstallDir>
```
Or set the `AutoCADInstallDir` environment variable before building.

---

## Build

```powershell
cd autocad-addin/src/AutoCADMCPPlugin

# Set AutoCAD install dir if not default
$env:AutoCADInstallDir = "C:\Program Files\Autodesk\AutoCAD 2025"

dotnet build AutoCADMCPPlugin.csproj
```

The output DLL will be at `bin\Debug\net48\AutoCADMCPPlugin.dll`.

> **Note:** The project targets `net48` (required by AutoCAD .NET API which is .NET Framework).  
> Build requires .NET Framework 4.8 SDK (`dotnet build` works if you have VS Build Tools or Visual Studio).

---

## Load into AutoCAD

1. Open AutoCAD 2025 (or your version)
2. Type `NETLOAD` in the command line
3. Browse to `AutoCADMCPPlugin.dll` (the plugin initializes automatically and starts the MCP server)
4. Run `MCPSTATUS` to verify the plugin status

Alternatively, add to `acad.lsp` or your startup profile for auto-load:
```lisp
(command "NETLOAD" "C:\\path\\to\\AutoCADMCPPlugin.dll")
```

---

## AutoCAD Commands

| Command | Description |
|---------|-------------|
| `MCPSTART` | Start or verify the MCP server |
| `MCPSTOP` | Stop the MCP server |
| `MCPSTATUS` | Show current server status and port |

---

## MCP Tools

| Tool | Description |
|------|-------------|
| `say_hello` | Test command |
| `get_document_info` | Drawing info (file name, units, extents) |
| `get_layers` | List all layers with properties |
| `get_entities` | List entities in model space (filter by layer/type) |
| `create_line` | Create a line from (x1,y1,z1) to (x2,y2,z2) |
| `write_message` | Write message to AutoCAD editor |
| `send_code_to_autocad` | Execute dynamic C# in AutoCAD context |

### Example JSON-RPC call

```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "get_layers",
  "params": {}
}
```

```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "method": "create_line",
  "params": {
    "startX": 0, "startY": 0, "startZ": 0,
    "endX": 100, "endY": 50, "endZ": 0,
    "layer": "0"
  }
}
```

---

## Connection

- **TCP port**: `8180` (default, auto-increments to `8199` if in use)
- **HTTP port**: `9180` (fixed)
- Port is saved to `%APPDATA%\DeepBim-MCP-ACAD\mcp-port.txt`

Configure your MCP client (Claude Desktop, etc.) to connect to `localhost:8180`.

---

## Logging

Log files are written to:
```
%APPDATA%\DeepBim-MCP-ACAD\logs\acad-mcp_YYYYMMDD.log
```

---

## Extending

To add a new tool:

1. Create a class in `Commands/` implementing `IAutoCADCommand`
2. If AutoCAD document access is needed, extend `DocumentContextCommandBase`
3. Add the command entry to `command.json`
4. Rebuild and reload the DLL in AutoCAD

```csharp
public class MyCommand : DocumentContextCommandBase
{
    public override string CommandName => "my_command";

    protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
    {
        // AutoCAD API calls here — doc is locked, on main thread
        return new AIResult<string> { Success = true, Response = "done" };
    }
}
```
