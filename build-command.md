# Build & Install — AutoCAD MCP Plugin

## 1. Build C# Plugin

```powershell
dotnet clean src/AutoCADMCPPlugin/AutoCADMCPPlugin.csproj; dotnet build src/AutoCADMCPPlugin/AutoCADMCPPlugin.csproj -c Release 2>&1 | tee src/AutoCADMCPPlugin/build_output.txt
```

## 2. Build MSI

```powershell
cd installers\msi
dotnet build DeepBimMCP.AutoCAD.Installer.wixproj --configuration Release /p:ProductVersion=1.0.0 /p:AutoCADVersion=2024
```

Output: `installers\msi\output\DeepBimMCP-AutoCAD2024-v1.0.0.msi`

> `AutoCADVersion` có thể là `2024`, `2025`, `2026`, ... (default: `2025`). Các file MSI khác tên trong `installers\msi\output` sẽ được giữ lại.

Build bằng script có thể nhập một version, danh sách version, hoặc cả range `2020-2027`:

```powershell
.\installers\msi\Build-Installer.ps1 -AutoCADVersion 2027
.\installers\msi\Build-Installer.ps1 -AutoCADVersion 2020,2024,2027
.\installers\msi\Build-Installer.ps1 -AutoCADVersion 2020-2027
```

Nếu muốn build DLL theo bộ AutoCAD API thấp nhất để dùng chung cho nhiều bản, truyền thêm `AutoCADInstallDir`:

```powershell
.\installers\msi\Build-Installer.ps1 -AutoCADVersion 2020-2027 -AutoCADInstallDir "C:\Program Files\Autodesk\AutoCAD 2020"
```

## 3. Install

> Close AutoCAD first.

```powershell
msiexec /i "installers\msi\output\DeepBimMCP-AutoCAD2024-v1.0.0.msi"
```

After installing, update `.vscode/mcp.json`:
```json
{ "servers": { "autocad": { "type": "stdio", "command": "node", "args": ["C:\\Program Files\\DeepBim\\AutoCAD-MCP\\server\\build\\index.js"] } } }
```

---

## Extras

```powershell
# Full pipeline (build C# + server + MSI)
.\installers\msi\Build-Installer.ps1 -AutoCADVersion 2025

# Build all MSI versions from AutoCAD 2020 to 2027
.\installers\msi\Build-Installer.ps1 -AutoCADVersion 2020-2027

# Uninstall
msiexec /x "installers\msi\output\DeepBimMCP-AutoCAD2024-v1.0.0.msi" /qn

# Check build errors
Select-String -Path src/AutoCADMCPPlugin/build_output.txt -Pattern "error CS|Error"
```
