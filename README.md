# DeepBIM-MCP Autocad Plugin

Connect AutoCAD with AI assistants via MCP over TCP/HTTP, enabling drawing inspection, project data access, and supported AutoCAD automation.


---
### DeepBIM MCP

This screen shows the DeepBIM MCP interface after the plugin has been installed successfully.

![DeepBIM MCP](<public/DeepBim MCP.png>)

### Connect AutoCAD

This screen guides users through creating the connection between AutoCAD and ChatGPT. It shows how to expose the local AutoCAD HTTP endpoint with Cloudflare Tunnel, copy the generated URL, and register it so ChatGPT can reach the active AutoCAD session.

![Connect AutoCAD](<public/Connect AutoCad.png>)

### Dashboard

The dashboard is the MCP interface for monitoring and managing the active AutoCAD MCP connection.

![Dashboard](<public/Dashboard.png>)

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

## Checked AutoCAD Versions

The plugin has currently only been checked with the following AutoCAD versions:

| AutoCAD Version | Check Status | Notes |
|-----------------|--------------|-------|
| AutoCAD 2022 | Checked | Initial compatibility check completed |
| AutoCAD 2024 | Checked | Initial compatibility check completed |
| AutoCAD 2027 | Checked | Initial compatibility check completed |

> **Note:** Other AutoCAD versions have not been confirmed yet.

### AutoCAD SDK DLLs

This project requires AutoCAD .NET managed DLLs. They are **not on NuGet** — you must reference them from your AutoCAD installation:

| DLL | Location |
|-----|----------|
| `acdbmgd.dll` | `C:\Program Files\Autodesk\AutoCAD <version>\` |
| `acmgd.dll` | `C:\Program Files\Autodesk\AutoCAD <version>\` |
| `AcCoreMgd.dll` | `C:\Program Files\Autodesk\AutoCAD <version>\` |

**Configure the path** in the `.csproj`:
```xml
<AutoCADInstallDir>C:\Program Files\Autodesk\AutoCAD 2024</AutoCADInstallDir>
```
Or set the `AutoCADInstallDir` environment variable before building.

---

## Build

```powershell
cd autocad-addin/src/AutoCADMCPPlugin

# Set AutoCAD install dir if not default
$env:AutoCADInstallDir = "C:\Program Files\Autodesk\AutoCAD 2024"

dotnet build AutoCADMCPPlugin.csproj
```

The output DLL will be at `bin\Debug\net48\AutoCADMCPPlugin.dll`.

> **Note:** The project targets `net48` (required by AutoCAD .NET API which is .NET Framework).  
> Build requires .NET Framework 4.8 SDK (`dotnet build` works if you have VS Build Tools or Visual Studio).
> The installer script builds AutoCAD 2025/2026 as `net8.0-windows` and AutoCAD 2027 as `net10.0-windows`.

---

## Load into AutoCAD

1. Open AutoCAD 2024 (or your compatible version)
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
| `MCPRIBBON` | Create or refresh the DeepBim MCP ribbon tab |

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
