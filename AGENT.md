# autocad-addin — Project Context for AI Agents

## What This Project Does

Bridges **AI clients (Claude, Cursor, VS Code Copilot)** ↔ **Autodesk AutoCAD 2024** via the MCP protocol.

```
AI Client  ──stdio──▶  autocad-mcp-server (TypeScript)  ──TCP:8180──▶  AutoCADMCPPlugin (C# .NET 4.8)  ──AutoCAD API──▶  AutoCAD 2024
```

## Skill Files (`.agents/`)

| File | Content |
|------|---------|
| [.agents/architecture.md](.agents/architecture.md) | Full project structure: C# plugin + TypeScript MCP server |
| [.agents/patterns.md](.agents/patterns.md) | How to add a command, DocumentContextQueue pattern, JSON-RPC flow |
| [.agents/commands.md](.agents/commands.md) | All available MCP commands with parameters |
| [.agents/server.md](.agents/server.md) | TypeScript MCP server: transport, tools, connection manager |

---

## Architecture (2 Layers)

```
AI Client (Claude / VS Code Copilot)
    │ stdio (MCP Protocol)
    ▼
autocad-mcp-server/          ← TypeScript MCP Server  (Node.js)   — see .agents/server.md
    │ TCP localhost:8180 (JSON-RPC 2.0)
    ▼
autocad-addin/               ← AutoCAD .NET Plugin (C#, net48, runs inside AutoCAD process)
    │ DocumentContextQueue (Application.Idle + ConcurrentQueue)
    ▼
Autodesk AutoCAD 2024
```

## Key Difference from Revit Plugin

| | Revit Plugin | AutoCAD Plugin |
|--|--|--|
| Thread bridge | `ExternalEvent.Raise()` | `Application.Idle` + `ConcurrentQueue` |
| Entry point | `IExternalApplication` | `IExtensionApplication` |
| Loading | `.addin` manifest | `NETLOAD` command |
| Target framework | `net8.0-windows` | `net48` (.NET Framework 4.8) |
| TCP port | 8080–8099 | 8180–8199 |

## Quick Start

1. Build C# plugin: `dotnet build autocad-addin/src/AutoCADMCPPlugin/AutoCADMCPPlugin.csproj`
2. Open AutoCAD 2024 → `NETLOAD` → select `bin\Debug\net48\AutoCADMCPPlugin.dll`
3. Type `MCPSTART` (or it auto-starts on load)
4. VS Code: the `autocad` MCP server in `.vscode/mcp.json` connects automatically
