# Build & Install — AutoCAD MCP Plugin

## 1. Build C# Plugin

```powershell
dotnet clean src/AutoCADMCPPlugin/AutoCADMCPPlugin.csproj; dotnet build src/AutoCADMCPPlugin/AutoCADMCPPlugin.csproj -c Release 2>&1 | tee src/AutoCADMCPPlugin/build_output.txt
```

## 2. Build MSI

```powershell
cd installers\msi
dotnet build DeepBimMCP.AutoCAD.Installer.wixproj --configuration Release /p:ProductVersion=1.0.0
```

Output: `installers\msi\output\DeepBimMCP-AutoCAD-v1.0.0.msi`

## 3. Install

> Close AutoCAD first.

```powershell
msiexec /i "installers\msi\output\DeepBimMCP-AutoCAD-v1.0.0.msi"
```

After installing, update `.vscode/mcp.json`:
```json
{ "servers": { "autocad": { "type": "stdio", "command": "node", "args": ["C:\\Program Files\\DeepBim\\AutoCAD-MCP\\server\\build\\index.js"] } } }
```

---

## Extras

```powershell
# Full pipeline (build C# + server + MSI)
.\installers\msi\Build-Installer.ps1

# Uninstall
msiexec /x "installers\msi\output\DeepBimMCP-AutoCAD-v1.0.0.msi" /qn

# Check build errors
Select-String -Path src/AutoCADMCPPlugin/build_output.txt -Pattern "error CS|Error"
```
