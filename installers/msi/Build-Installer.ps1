<#
.SYNOPSIS
    Build the DeepBim AutoCAD MCP Plugin MSI installer.

.DESCRIPTION
    Full pipeline:
      1. Build C# AutoCAD plugin (dotnet build --configuration Release)
      2. Build Node.js MCP server (npm run build)
      3. npm prune --production  (remove dev dependencies)
      4. Generate ServerFiles.wxs from build output
      5. Generate ServerNodeModules.wxs from node_modules
      6. dotnet build DeepBimMCP.AutoCAD.Installer.wixproj  →  MSI
      7. Copy MSI to output\ folder

.PARAMETER ProductVersion
    Version string embedded in the MSI and output filename. Default: 1.0.0

.PARAMETER SkipBuild
    Skip steps 1-2 (use existing build output).

.PARAMETER SkipNpmPrune
    Skip npm prune step (use current node_modules as-is).

.PARAMETER Configuration
    MSBuild configuration. Default: Release

.EXAMPLE
    .\Build-Installer.ps1
    .\Build-Installer.ps1 -ProductVersion 1.2.0
    .\Build-Installer.ps1 -ProductVersion 1.2.0 -SkipBuild
#>
param(
    [string]$ProductVersion = "1.0.0",
    [switch]$SkipBuild,
    [switch]$SkipNpmPrune,
    [string]$Configuration  = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$InstallerDir   = $PSScriptRoot
$RepoRoot       = Resolve-Path "$InstallerDir\..\.."          # autocad-addin\
$PluginProjDir  = Resolve-Path "$RepoRoot\src\AutoCADMCPPlugin"
$ServerDir      = Resolve-Path "$InstallerDir\..\..\..\..\autocad-mcp-server"
$OutputDir      = "$InstallerDir\output"
$WixProj        = "$InstallerDir\DeepBimMCP.AutoCAD.Installer.wixproj"

$PluginBinDir   = "$PluginProjDir\bin\$Configuration\net48"

# ─────────────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   DeepBim AutoCAD MCP — Build Installer v$ProductVersion" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ── Step 1: Build C# plugin ───────────────────────────────────────────────────
if (-not $SkipBuild) {
    Write-Host "[ 1/6 ] Building AutoCAD plugin (C#, net48, $Configuration)..." -ForegroundColor Yellow
    Push-Location $PluginProjDir
    try {
        dotnet build AutoCADMCPPlugin.csproj --configuration $Configuration --nologo
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)" }
    }
    finally { Pop-Location }

    if (-not (Test-Path "$PluginBinDir\AutoCADMCPPlugin.dll")) {
        throw "Build succeeded but AutoCADMCPPlugin.dll not found at: $PluginBinDir"
    }
    Write-Host "    ✓ Plugin DLL: $PluginBinDir\AutoCADMCPPlugin.dll" -ForegroundColor Green
}
else {
    Write-Host "[ 1/6 ] Skipping C# plugin build (-SkipBuild)" -ForegroundColor Gray
    if (-not (Test-Path "$PluginBinDir\AutoCADMCPPlugin.dll")) {
        throw "SkipBuild set but DLL not found: $PluginBinDir\AutoCADMCPPlugin.dll"
    }
}

# ── Step 2: Build Node.js server ──────────────────────────────────────────────
if (-not $SkipBuild) {
    Write-Host "[ 2/6 ] Building Node.js MCP server (npm run build)..." -ForegroundColor Yellow
    Push-Location $ServerDir
    try {
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "npm run build failed (exit $LASTEXITCODE)" }
    }
    finally { Pop-Location }
    Write-Host "    ✓ Server build: $ServerDir\build\" -ForegroundColor Green
}
else {
    Write-Host "[ 2/6 ] Skipping Node.js build (-SkipBuild)" -ForegroundColor Gray
}

# ── Step 3: npm prune --production ────────────────────────────────────────────
if (-not $SkipNpmPrune) {
    Write-Host "[ 3/6 ] Running npm prune --production (removes devDependencies)..." -ForegroundColor Yellow
    Push-Location $ServerDir
    try {
        npm prune --production
        if ($LASTEXITCODE -ne 0) { throw "npm prune failed (exit $LASTEXITCODE)" }
    }
    finally { Pop-Location }
    Write-Host "    ✓ node_modules pruned to production-only" -ForegroundColor Green
}
else {
    Write-Host "[ 3/6 ] Skipping npm prune (-SkipNpmPrune)" -ForegroundColor Gray
}

# ── Step 4: Generate ServerFiles.wxs ─────────────────────────────────────────
Write-Host "[ 4/6 ] Generating ServerFiles.wxs..." -ForegroundColor Yellow
& "$InstallerDir\Generate-ServerFiles.ps1" `
    -ServerBuildDir "$ServerDir\build" `
    -OutputFile     "$InstallerDir\ServerFiles.wxs"
Write-Host "    ✓ ServerFiles.wxs updated" -ForegroundColor Green

# ── Step 5: Generate ServerNodeModules.wxs ───────────────────────────────────
Write-Host "[ 5/6 ] Generating ServerNodeModules.wxs (~4000+ files, may take a minute)..." -ForegroundColor Yellow
& "$InstallerDir\Generate-ServerNodeModules.ps1" `
    -NodeModulesDir "$ServerDir\node_modules" `
    -OutputFile     "$InstallerDir\ServerNodeModules.wxs"
Write-Host "    ✓ ServerNodeModules.wxs updated" -ForegroundColor Green

# ── Step 6: Build MSI ─────────────────────────────────────────────────────────
Write-Host "[ 6/6 ] Building MSI..." -ForegroundColor Yellow

$msiName = "DeepBimMCP-AutoCAD-v$ProductVersion.msi"

dotnet build $WixProj `
    --configuration $Configuration `
    /p:ProductVersion=$ProductVersion `
    --nologo

if ($LASTEXITCODE -ne 0) { throw "WiX build failed (exit $LASTEXITCODE)" }

# Find the built MSI
$builtMsi = Get-ChildItem "$InstallerDir\bin\$Configuration" -Filter "*.msi" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $builtMsi) {
    throw "MSI not found in $InstallerDir\bin\$Configuration after build"
}

# Copy to output\
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$outputMsi = "$OutputDir\$msiName"
Copy-Item $builtMsi.FullName -Destination $outputMsi -Force

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║   BUILD SUCCEEDED                                        ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "  MSI: $outputMsi" -ForegroundColor Green
Write-Host ""
Write-Host "After installation:" -ForegroundColor Cyan
Write-Host "  1. Restart AutoCAD — plugin loads automatically from bundle folder"
Write-Host "  2. Type MCPSTATUS to verify the plugin is running"
Write-Host "  3. Update VS Code .vscode/mcp.json:"
$serverIndexPath = [System.IO.Path]::Combine($env:ProgramFiles, "DeepBim", "AutoCAD-MCP", "server", "build", "index.js")
Write-Host "     `"args`": [`"$serverIndexPath`"]"
Write-Host "  4. (Path also stored in: HKLM\SOFTWARE\DeepBim\AutoCAD-MCP\ServerPath)"
