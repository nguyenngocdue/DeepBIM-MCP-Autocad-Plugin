# DeepBim AutoCAD MCP Plugin v[VERSION]

Release date: [YYYY-MM-DD]

DeepBim AutoCAD MCP Plugin connects AutoCAD with MCP-compatible AI tools, allowing assistants to inspect drawings, read layers/entities, and execute supported AutoCAD automation commands through either a local MCP server or the hosted DeepBim MCP endpoint for ChatGPT.

## Highlights

- AutoCAD plugin packaged as a Windows MSI installer.
- MCP server files included in the installer.
- Supports AutoCAD 2018-2027 on Windows x64.
- Installs the AutoCAD bundle for automatic loading on startup.
- Stores the MCP server path in Windows Registry for easier client configuration.
- Supports both local MCP configuration and hosted ChatGPT app configuration.
- Hosted online MCP endpoint for ChatGPT: `https://autocad-mcp.deepbim.app/mcp`

## Quick Start: Use With ChatGPT

Use this option if you want to connect through ChatGPT Apps using the hosted DeepBim AutoCAD MCP endpoint. This is the easiest setup for community users because you do not need to manually configure or run a local MCP server.

1. Install the MSI for your AutoCAD version.
2. Restart AutoCAD.
3. Open ChatGPT.
4. Go to `Settings`.
5. Open `Apps`.
6. Select `Create app`.
7. Fill in the app information:

| Field | Value |
| --- | --- |
| Logo | Use the DeepBim logo. |
| Name | `DeepBim AutoCAD MCP` |
| Description | `Connect ChatGPT to AutoCAD through DeepBim MCP to inspect drawings and run supported automation commands.` |
| MCP Server URL | `https://autocad-mcp.deepbim.app/mcp` |
| Authentication | `No Auth` |

8. Save the app.
9. Enable the app in ChatGPT.
10. In AutoCAD, run:

```text
MCPSTATUS
```

Hosted MCP endpoint:

```text
https://autocad-mcp.deepbim.app/mcp
```

Authentication is not implemented yet. Select `No Auth` when creating the ChatGPT app.

## Downloads

Choose the MSI that matches your AutoCAD version. Do not download or install `.wixpdb` files; those are developer debug symbols.

| AutoCAD Version | Installer |
| --- | --- |
| AutoCAD 2018 | `DeepBimMCP-AutoCAD2018-v[VERSION].msi` |
| AutoCAD 2019 | `DeepBimMCP-AutoCAD2019-v[VERSION].msi` |
| AutoCAD 2020 | `DeepBimMCP-AutoCAD2020-v[VERSION].msi` |
| AutoCAD 2021 | `DeepBimMCP-AutoCAD2021-v[VERSION].msi` |
| AutoCAD 2022 | `DeepBimMCP-AutoCAD2022-v[VERSION].msi` |
| AutoCAD 2023 | `DeepBimMCP-AutoCAD2023-v[VERSION].msi` |
| AutoCAD 2024 | `DeepBimMCP-AutoCAD2024-v[VERSION].msi` |
| AutoCAD 2025 | `DeepBimMCP-AutoCAD2025-v[VERSION].msi` |
| AutoCAD 2026 | `DeepBimMCP-AutoCAD2026-v[VERSION].msi` |
| AutoCAD 2027 | `DeepBimMCP-AutoCAD2027-v[VERSION].msi` |

## System Requirements

- Windows 10/11 x64.
- AutoCAD 2018-2027 x64.
- .NET Framework 4.8.
- Node.js LTS installed and available as `node` in PATH.
- Administrator permission to install the MSI.
- ChatGPT account with Apps support, if using the hosted online MCP endpoint.

## Installation

1. Close AutoCAD.
2. Download the MSI for your AutoCAD version.
3. Run the MSI installer.
4. Restart AutoCAD.
5. In AutoCAD, run:

```text
MCPSTATUS
```

If the plugin is loaded correctly, AutoCAD will show the MCP server status and port information.

## Connection Options

You can connect to DeepBim AutoCAD MCP in either local mode or online ChatGPT mode.

| Mode | Best For | MCP Server URL / Command |
| --- | --- | --- |
| Local MCP Server | Local desktop workflows, private network use, custom MCP clients. | `node "C:\Program Files\DeepBim\AutoCAD-MCP\server\build\index.js"` |
| ChatGPT Online MCP | Community users who want to connect through ChatGPT Apps without manually running a local MCP server. | `https://autocad-mcp.deepbim.app/mcp` |

## Local MCP Client Configuration

After installation, configure your MCP client to run the installed server:

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

The server path is also stored in:

```text
HKLM\SOFTWARE\DeepBim\AutoCAD-MCP\ServerPath
```

## ChatGPT Online Setup

Use this option if you want to connect through ChatGPT Apps using the hosted DeepBim MCP endpoint. The quick-start version is at the top of this release note; the same settings are repeated here for reference.

1. Open ChatGPT.
2. Go to `Settings`.
3. Open `Apps`.
4. Select `Create app`.
5. Fill in the app information:

| Field | Value |
| --- | --- |
| Logo | Use the DeepBim logo. |
| Name | `DeepBim AutoCAD MCP` |
| Description | `Connect ChatGPT to AutoCAD through DeepBim MCP to inspect drawings and run supported automation commands.` |
| MCP Server URL | `https://autocad-mcp.deepbim.app/mcp` |
| Authentication | `No Auth` |

6. Save the app.
7. Enable the app in ChatGPT.
8. Restart AutoCAD if it was already open.
9. In AutoCAD, run:

```text
MCPSTATUS
```

The online endpoint is:

```text
https://autocad-mcp.deepbim.app/mcp
```

Authentication is not implemented yet. Select `No Auth` when creating the ChatGPT app.

## Installed Locations

AutoCAD plugin bundle:

```text
C:\ProgramData\Autodesk\ApplicationPlugins\DeepBimAutoCADMCP.bundle
```

MCP server:

```text
C:\Program Files\DeepBim\AutoCAD-MCP\server
```

## Available AutoCAD Commands

| Command | Description |
| --- | --- |
| `MCPSTART` | Start or verify the MCP server. |
| `MCPSTOP` | Stop the MCP server. |
| `MCPSTATUS` | Show current MCP server status and port. |

## Security Notes

- The plugin runs locally on the user's machine.
- MCP access should only be enabled for trusted clients.
- The hosted ChatGPT MCP endpoint is currently configured with `No Auth`.
- Do not use confidential production drawings with online workflows unless that matches your organization's security policy.
- Review AI-generated commands before applying them to production drawings.
- Keep backups of important DWG files before automation-heavy workflows.

## Checksums

Optional but recommended for public releases. Generate SHA256 checksums after building the MSI files.

```powershell
Get-FileHash .\DeepBimMCP-AutoCAD2025-v[VERSION].msi -Algorithm SHA256
```

| File | SHA256 |
| --- | --- |
| `DeepBimMCP-AutoCAD2018-v[VERSION].msi` | `[SHA256]` |
| `DeepBimMCP-AutoCAD2019-v[VERSION].msi` | `[SHA256]` |
| `DeepBimMCP-AutoCAD2020-v[VERSION].msi` | `[SHA256]` |
| `DeepBimMCP-AutoCAD2021-v[VERSION].msi` | `[SHA256]` |
| `DeepBimMCP-AutoCAD2022-v[VERSION].msi` | `[SHA256]` |
| `DeepBimMCP-AutoCAD2023-v[VERSION].msi` | `[SHA256]` |
| `DeepBimMCP-AutoCAD2024-v[VERSION].msi` | `[SHA256]` |
| `DeepBimMCP-AutoCAD2025-v[VERSION].msi` | `[SHA256]` |
| `DeepBimMCP-AutoCAD2026-v[VERSION].msi` | `[SHA256]` |
| `DeepBimMCP-AutoCAD2027-v[VERSION].msi` | `[SHA256]` |

## What's New

- [Feature or improvement 1]
- [Feature or improvement 2]
- [Bug fix 1]

## Known Limitations

- Requires AutoCAD to be installed locally.
- Requires Node.js to be available as `node`.
- ChatGPT online mode currently uses `No Auth`.
- Some automation commands may depend on the active drawing state.
- The plugin should be tested with your AutoCAD version before production use.

## Upgrade Notes

1. Close AutoCAD.
2. Install the new MSI for your AutoCAD version.
3. Restart AutoCAD.
4. Run `MCPSTATUS` to verify the plugin status.

## Uninstall

Use Windows Settings:

```text
Settings -> Apps -> Installed apps -> DeepBim AutoCAD MCP
```

Or run:

```powershell
msiexec /x "DeepBimMCP-AutoCAD[VERSION_YEAR]-v[VERSION].msi"
```

## Support

Please report issues with:

- AutoCAD version.
- Windows version.
- Plugin version.
- MCP client name and version.
- Steps to reproduce the problem.
- Relevant logs from:

```text
%APPDATA%\DeepBim-MCP-ACAD\logs
```

## Release Checklist

- [ ] Build MSI files for all supported AutoCAD versions.
- [ ] Test install and uninstall on a clean machine.
- [ ] Confirm AutoCAD loads the plugin after restart.
- [ ] Confirm `MCPSTATUS` works.
- [ ] Confirm MCP client can launch the server.
- [ ] Confirm ChatGPT app setup works with `https://autocad-mcp.deepbim.app/mcp`.
- [ ] Generate SHA256 checksums.
- [ ] Upload only `.msi` files and release notes.
- [ ] Do not upload `.wixpdb` files for public users.
