# Build MSI — DeepBim AutoCAD MCP Plugin

## Prerequisites

1. **WiX Toolset v5** — install via `dotnet`:
   ```powershell
   dotnet tool install --global wix --version 5.0.2
   wix extension add WixToolset.UI.wixext/5.0.2
   ```

2. **Node.js** — required for `npm run build` and `npm prune`

3. **AutoCAD 2024** installed at `C:\Program Files\Autodesk\AutoCAD 2024\` (for C# build)

## Commands

```powershell
# Standard build (full pipeline)
.\installers\msi\Build-Installer.ps1

# With specific version number
.\installers\msi\Build-Installer.ps1 -ProductVersion 1.2.0

# Skip rebuilding plugin + server (use existing build output)
.\installers\msi\Build-Installer.ps1 -ProductVersion 1.0.0 -SkipBuild

# Skip npm prune (keep devDependencies in node_modules)
.\installers\msi\Build-Installer.ps1 -SkipNpmPrune
```

## Output

```
autocad-addin\installers\msi\output\
    DeepBimMCP-AutoCAD-v1.0.0.msi
```

## What Gets Installed

| What | Where |
|------|-------|
| AutoCAD plugin DLL | `C:\ProgramData\Autodesk\ApplicationPlugins\DeepBimAutoCADMCP.bundle\Contents\` |
| PackageContents.xml | `C:\ProgramData\Autodesk\ApplicationPlugins\DeepBimAutoCADMCP.bundle\` |
| Node.js MCP server | `%ProgramFiles%\DeepBim\AutoCAD-MCP\server\` |
| Registry paths | `HKLM\SOFTWARE\DeepBim\AutoCAD-MCP\ServerPath` |

## Post-Install VS Code Config

After installing the MSI, update `.vscode/mcp.json`:

```json
{
  "servers": {
    "autocad": {
      "type": "stdio",
      "command": "node",
      "args": ["C:\\Program Files\\DeepBim\\AutoCAD-MCP\\server\\build\\index.js"]
    }
  }
}
```

> Tip: The exact path is stored in `HKLM\SOFTWARE\DeepBim\AutoCAD-MCP\ServerPath`

## Manual WiX Steps (if needed)

//cd "E:\C# Tool Revit\revit-mcp\mcp-addin\autocad-addin"

```powershell
cd installers\msi

# Regenerate ServerFiles.wxs after server changes
.\Generate-ServerFiles.ps1

# Regenerate ServerNodeModules.wxs after npm install/update
.\Generate-ServerNodeModules.ps1

# Build only the MSI (after manual file changes)
dotnet build DeepBimMCP.AutoCAD.Installer.wixproj --configuration Release /p:ProductVersion=1.0.0
```
