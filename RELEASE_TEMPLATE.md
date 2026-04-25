# 🚀 DeepBim AutoCAD MCP Plugin

**Release date:** 2026-04-25  
**License:** MIT  
**Platform:** Windows x64  
**Supported AutoCAD versions:** AutoCAD 2018–2027  

DeepBim AutoCAD MCP Plugin connects AutoCAD with MCP-compatible AI tools, allowing AI assistants to inspect drawings, read layers and entities, and execute supported AutoCAD automation commands.

The plugin supports both local MCP workflows and hosted MCP integrations with MCP-compatible AI clients such as ChatGPT, Claude, Cursor, Copilot, and other MCP-compatible tools.

For users who want a simpler setup and lower operating cost, DeepBim provides a hosted MCP endpoint for ChatGPT, allowing AutoCAD to connect without manually running a local MCP server.

---

## 📌 1. Overview

DeepBim AutoCAD MCP Plugin provides two ways to connect AutoCAD with AI assistants:

| Connection Mode | Recommended For | Description |
| --- | --- | --- |
| **ChatGPT Online MCP** | Community users, simple setup, lower operating cost | Connect through the hosted DeepBim MCP endpoint without manually running a local MCP server |
| **Local MCP Server** | Developers, private workflows, enterprise/internal use | Run the MCP server locally and connect from MCP-compatible desktop clients |

Hosted MCP endpoint:

```text
https://autocad-mcp.deepbim.app/mcp
```

---

## 🧭 2. Architecture Diagram

### 💬 ChatGPT Online MCP Mode

```text
👤 User
   ↓
💬 ChatGPT App
   ↓
🌐 DeepBim Hosted MCP Endpoint
   ↓
🧩 AutoCAD MCP Plugin
   ↓
📐 Active DWG Drawing
```

### 🖥️ Local MCP Server Mode

```text
🤖 MCP Client
   Claude / Cursor / Copilot / Custom Client
   ↓
🖥️ Local MCP Server
   ↓
🧩 AutoCAD MCP Plugin
   ↓
📐 Active DWG Drawing
```

---

## ✨ 3. Highlights

- Windows MSI installer for AutoCAD.
- AutoCAD bundle installation for AutoCAD 2018-2024 with automatic loading on startup.
- AutoCAD 2025-2027 supported through manual `NETLOAD` after installation.
- MCP server files included in the installer.
- Support for AutoCAD 2018–2027 on Windows x64.
- Local MCP server support for desktop and private workflows.
- Hosted MCP endpoint support for ChatGPT Apps.
- MCP server path stored in Windows Registry for easier client configuration.
- Compatible with MCP clients such as ChatGPT, Claude Desktop, Cursor, Copilot, and other MCP-compatible tools.

---

## 🧩 4. Connection Options

| Mode | Best For | Configuration |
| --- | --- | --- |
| **ChatGPT Online MCP** | Community users who want a simple setup and lower usage cost | ChatGPT app uses `https://autocad-mcp.deepbim.app/mcp`; the AutoCAD machine exposes local HTTP `localhost:9180` through Cloudflare Tunnel |
| **Local MCP Server** | Local desktop workflows, private network use, custom MCP clients | `node "C:\Program Files\DeepBim\AutoCAD-MCP\server\build\index.js"` |

### Recommended Option

For most community users, **ChatGPT Online MCP** is recommended because it does not require running the Node.js local MCP server. The AutoCAD machine still needs the plugin loaded and a Cloudflare Tunnel connected to local HTTP port `9180`.

For developers and enterprise users, **Local MCP Server** is recommended when drawings or workflows must remain inside a private environment.

---

## ⚙️ 5. System Requirements

- Windows 10 or Windows 11 x64.
- AutoCAD 2018–2027 x64.
- .NET Framework 4.8 for AutoCAD 2018-2024.
- .NET 8 Desktop Runtime for AutoCAD 2025-2026.
- .NET runtime required by AutoCAD 2027.
- Node.js LTS installed and available as `node` in PATH.
- `cloudflared` installed on the machine running AutoCAD, if using ChatGPT Online MCP over HTTP tunnel.
- Administrator permission to install the MSI.
- ChatGPT account with Apps support, if using the hosted online MCP endpoint.

---

## 📦 6. Installation

1. Close AutoCAD.
2. Download the MSI installer for your AutoCAD version.
3. Run the MSI installer as administrator.
4. Restart AutoCAD.
5. Load the plugin using the version-specific instructions below.
6. In AutoCAD, run:

```text
MCPSTATUS
```

If the plugin is installed correctly, AutoCAD will display the MCP server status and port information.

### AutoCAD 2018-2024

AutoCAD 2018-2024 should discover the installed bundle automatically at startup. After restarting AutoCAD, run:

```text
MCPSTATUS
```

### AutoCAD 2025-2027

AutoCAD 2025, 2026, and 2027 may not load the bundle automatically after MSI installation. If `MCPSTATUS` is not recognized, load the plugin manually:

1. In AutoCAD, run:

```text
NETLOAD
```

2. Browse to:

```text
C:\Program Files\Autodesk\ApplicationPlugins\DeepBimAutoCADMCP.bundle\Contents\AutoCADMCPPlugin.dll
```

3. Select `AutoCADMCPPlugin.dll`.
4. Run:

```text
MCPSTATUS
```

---

## 💬 7. Quick Start: ChatGPT Online Mode

Use this option if you want to connect ChatGPT to AutoCAD through the hosted DeepBim MCP endpoint.

This mode uses the AutoCAD plugin's local HTTP endpoint and a Cloudflare Tunnel. ChatGPT connects to the hosted DeepBim MCP endpoint, and the hosted endpoint reaches the user's AutoCAD machine through the registered tunnel URL.

Important: ChatGPT can access AutoCAD only after the local machine is ready. AutoCAD must be open, the DeepBim AutoCAD MCP plugin must be loaded, the local HTTP server must be running on port `9180`, and the current Cloudflare Tunnel URL must be registered at `https://autocad-mcp.deepbim.app/connect`.

### Steps

1. Install `cloudflared` on the machine running AutoCAD:

```powershell
winget install Cloudflare.cloudflared
```

2. Install the MSI package for your AutoCAD version.
3. Restart AutoCAD.
4. For AutoCAD 2025-2027, run `NETLOAD` and select:

```text
C:\Program Files\Autodesk\ApplicationPlugins\DeepBimAutoCADMCP.bundle\Contents\AutoCADMCPPlugin.dll
```

5. In AutoCAD, run:

```text
MCPSTART
```

6. Verify that the status output shows HTTP port `9180`:

```text
MCPSTATUS
```

7. In a terminal on the AutoCAD machine, start the Cloudflare Tunnel:

```powershell
cloudflared tunnel --url http://localhost:9180
```

8. Copy the generated public URL, for example:

```text
https://xxxx.trycloudflare.com
```

9. Open:

```text
https://autocad-mcp.deepbim.app/connect
```

10. Paste the Cloudflare Tunnel URL and click `Connect`.
11. Wait until the page shows:

```text
Connected! AutoCAD URL updated successfully.
```

12. Keep AutoCAD open and keep the `cloudflared` terminal running.
13. Open ChatGPT.
14. Go to `Settings`.
15. Open `Apps`.
16. Select `Create app`.
17. Fill in the app information:

| Field | Value |
| --- | --- |
| Logo | Use the DeepBim logo |
| Name | `DeepBim AutoCAD MCP` |
| Description | `Connect ChatGPT to AutoCAD through DeepBim MCP to inspect drawings and run supported automation commands.` |
| MCP Server URL | `https://autocad-mcp.deepbim.app/mcp` |
| Authentication | `No Auth` |

18. Save the app.
19. Enable the app in ChatGPT.
20. In AutoCAD, run:

```text
MCPSTATUS
```

If the plugin is loaded correctly, AutoCAD will show the MCP server status and port information.

After the Cloudflare Tunnel URL is registered at `https://autocad-mcp.deepbim.app/connect`, the page shows `Connected! AutoCAD URL updated successfully.`, and `MCPSTATUS` shows the plugin is running, ChatGPT can call the hosted MCP endpoint and reach the active AutoCAD session through HTTP.

Every time the Cloudflare Tunnel is restarted, a new `trycloudflare.com` URL is generated. Paste the new URL into `https://autocad-mcp.deepbim.app/connect`, click `Connect`, and wait for `Connected! AutoCAD URL updated successfully.` again.

> Authentication is not implemented yet. Select `No Auth` when creating the ChatGPT app.

---

## 🖥️ 8. Local MCP Client Configuration

After installation, configure your MCP client to run the installed local server.

Example configuration:

```json
{
  "servers": {
    "autocad": {
      "type": "stdio",
      "command": "node",
      "args": [
        "C:\\Program Files\\DeepBim\\AutoCAD-MCP\\server\\build\\index.js"
      ]
    }
  }
}
```

The MCP server path is also stored in Windows Registry:

```text
HKLM\SOFTWARE\DeepBim\AutoCAD-MCP\ServerPath
```

---

## 📁 9. Installed Locations

AutoCAD plugin bundle:

```text
C:\Program Files\Autodesk\ApplicationPlugins\DeepBimAutoCADMCP.bundle
```

Plugin DLL:

```text
C:\Program Files\Autodesk\ApplicationPlugins\DeepBimAutoCADMCP.bundle\Contents\AutoCADMCPPlugin.dll
```

MCP server:

```text
C:\Program Files\DeepBim\AutoCAD-MCP\server
```

---

## 🛠️ 10. Available AutoCAD Commands

| Command | Description |
| --- | --- |
| `MCPSTART` | Start or verify the MCP server |
| `MCPSTOP` | Stop the MCP server |
| `MCPSTATUS` | Show the current MCP server status and port information |
| `MCPSETTINGS` | Open the command settings window |

---

## 🔐 11. Security Notes

- The plugin runs locally on the user's machine.
- MCP access should only be enabled for trusted clients.
- The hosted ChatGPT MCP endpoint currently uses `No Auth`.
- Do not use confidential production drawings with online workflows unless this matches your organization's security policy.
- Review AI-generated commands before applying them to production drawings.
- Keep backups of important DWG files before automation-heavy workflows.
- For enterprise environments, review firewall, endpoint security, and MCP client policies before deployment.

---

## ⚠️ 12. Known Limitations

- AutoCAD must be installed locally.
- Node.js must be installed and available as `node` in PATH.
- ChatGPT online mode currently uses `No Auth`.
- Some automation commands may depend on the active drawing state.
- The plugin should be tested with your AutoCAD version before production use.
- Endpoint security tools may block unsigned or newly built plugin files until they are reviewed or allowlisted.
- AutoCAD 2025-2027 may require manual `NETLOAD` to load `AutoCADMCPPlugin.dll` after installation.

---

## 🔄 13. Upgrade Notes

1. Close AutoCAD.
2. Install the new MSI package for your AutoCAD version.
3. Restart AutoCAD.
4. For AutoCAD 2025-2027, run `NETLOAD` and select:

```text
C:\Program Files\Autodesk\ApplicationPlugins\DeepBimAutoCADMCP.bundle\Contents\AutoCADMCPPlugin.dll
```

5. Run the following command in AutoCAD:

```text
MCPSTATUS
```

6. Verify that the plugin status and MCP server information are displayed correctly.

---

## 🧾 14. Support

When reporting an issue, please include the following information:

- AutoCAD version.
- Windows version.
- Plugin version.
- MCP client name and version.
- Installation method.
- Connection mode: local MCP server or hosted ChatGPT MCP.
- Steps to reproduce the issue.
- Relevant logs or screenshots.
- Any endpoint security alerts, such as SentinelOne, Windows Defender, or antivirus warnings.

---

## 👤 15. Project Information

| Item | Value |
| --- | --- |
| Project | DeepBim AutoCAD MCP Plugin |
| Author | Nguyễn Ngọc Duệ |
| GitHub | `https://github.com/nguyenngocdue` |
| License | MIT |
