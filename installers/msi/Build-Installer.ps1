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
      6. dotnet build DeepBimMCP.AutoCAD.Installer.wixproj -> output\MSI

.PARAMETER ProductVersion
    Version string embedded in the MSI and output filename. Default: 1.0.0

.PARAMETER AutoCADVersion
    AutoCAD release year(s) embedded in the MSI name and package title.
    Supports a single year, comma-separated years, or a range from 2020 to 2027.
    Default: 2025

.PARAMETER AutoCADInstallDir
    Optional AutoCAD install folder used to build the C# plugin references.
    Example: C:\Program Files\Autodesk\AutoCAD 2020

.PARAMETER SkipBuild
    Skip steps 1-2 (use existing build output).

.PARAMETER SkipNpmPrune
    Skip npm prune step (use current node_modules as-is).

.PARAMETER Configuration
    MSBuild configuration. Default: Release

.EXAMPLE
    .\Build-Installer.ps1
    .\Build-Installer.ps1 -ProductVersion 1.2.0 -AutoCADVersion 2025
    .\Build-Installer.ps1 -ProductVersion 1.2.0 -AutoCADVersion 2020,2024,2027
    .\Build-Installer.ps1 -ProductVersion 1.2.0 -AutoCADVersion 2020-2027 -SkipBuild
#>
param(
    [string]$ProductVersion  = "1.0.0",
    [string[]]$AutoCADVersion = @("2025"),
    [string]$AutoCADInstallDir = "",
    [switch]$SkipBuild,
    [switch]$SkipNpmPrune,
    [string]$Configuration   = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Expand-AutoCADVersions {
    param([string[]]$VersionTokens)

    $versions = [System.Collections.Generic.List[int]]::new()
    $seen = @{}

    function Add-Version([int]$Version) {
        if ($Version -lt 2020 -or $Version -gt 2027) {
            throw "AutoCADVersion '$Version' is outside the supported range 2020-2027."
        }

        $key = $Version.ToString()
        if (-not $seen.ContainsKey($key)) {
            $seen[$key] = $true
            $versions.Add($Version)
        }
    }

    foreach ($token in $VersionTokens) {
        foreach ($part in ($token -split ",")) {
            $value = $part.Trim()
            if ($value -eq "") { continue }

            if ($value -match "^(\d{4})\s*(?:-|\.\.)\s*(\d{4})$") {
                [int]$startVersion = $Matches[1]
                [int]$endVersion = $Matches[2]
                if ($startVersion -gt $endVersion) {
                    throw "AutoCADVersion range '$value' is invalid. Use ascending ranges like 2020-2027."
                }

                foreach ($version in $startVersion..$endVersion) {
                    Add-Version $version
                }
            }
            elseif ($value -match "^\d{4}$") {
                Add-Version ([int]$value)
            }
            else {
                throw "Invalid AutoCADVersion value '$value'. Use 2027, 2020,2024,2027, or 2020-2027."
            }
        }
    }

    if ($versions.Count -eq 0) {
        throw "At least one AutoCADVersion is required."
    }

    return $versions.ToArray()
}

$AutoCADVersions = Expand-AutoCADVersions $AutoCADVersion
$AutoCADVersionsText = $AutoCADVersions -join ", "

$InstallerDir   = $PSScriptRoot
$RepoRoot       = Resolve-Path "$InstallerDir\..\.."          # autocad-addin\
$PluginProjDir  = Resolve-Path "$RepoRoot\src\AutoCADMCPPlugin"
$ServerDir      = Resolve-Path "$InstallerDir\..\..\..\..\autocad-mcp-server"
$OutputDir      = [System.IO.Path]::Combine($InstallerDir, "output")
$WixProj        = "$InstallerDir\DeepBimMCP.AutoCAD.Installer.wixproj"

$PluginBinDir   = "$PluginProjDir\bin\$Configuration\net48"

# -----------------------------------------------------------------------------
Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  DeepBim AutoCAD MCP $AutoCADVersionsText - Build Installer v$ProductVersion" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build C# plugin
if (-not $SkipBuild) {
    Write-Host "[ 1/6 ] Building AutoCAD plugin (C#, net48, $Configuration)..." -ForegroundColor Yellow
    Push-Location $PluginProjDir
    try {
        $pluginBuildArgs = @(
            "build",
            "AutoCADMCPPlugin.csproj",
            "--configuration", $Configuration,
            "--nologo"
        )

        if (-not [string]::IsNullOrWhiteSpace($AutoCADInstallDir)) {
            $pluginBuildArgs += "/p:AutoCADInstallDir=$AutoCADInstallDir"
        }

        dotnet @pluginBuildArgs
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)" }
    }
    finally { Pop-Location }

    if (-not (Test-Path "$PluginBinDir\AutoCADMCPPlugin.dll")) {
        throw "Build succeeded but AutoCADMCPPlugin.dll not found at: $PluginBinDir"
    }
    Write-Host "    OK Plugin DLL: $PluginBinDir\AutoCADMCPPlugin.dll" -ForegroundColor Green
}
else {
    Write-Host "[ 1/6 ] Skipping C# plugin build (-SkipBuild)" -ForegroundColor Gray
    if (-not (Test-Path "$PluginBinDir\AutoCADMCPPlugin.dll")) {
        throw "SkipBuild set but DLL not found: $PluginBinDir\AutoCADMCPPlugin.dll"
    }
}

# Step 2: Build Node.js server
if (-not $SkipBuild) {
    Write-Host "[ 2/6 ] Building Node.js MCP server (npm run build)..." -ForegroundColor Yellow
    Push-Location $ServerDir
    try {
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "npm run build failed (exit $LASTEXITCODE)" }
    }
    finally { Pop-Location }
    Write-Host "    OK Server build: $ServerDir\build\" -ForegroundColor Green
}
else {
    Write-Host "[ 2/6 ] Skipping Node.js build (-SkipBuild)" -ForegroundColor Gray
}

# Step 3: npm prune --production
if (-not $SkipNpmPrune) {
    Write-Host "[ 3/6 ] Running npm prune --production (removes devDependencies)..." -ForegroundColor Yellow
    Push-Location $ServerDir
    try {
        npm prune --production
        if ($LASTEXITCODE -ne 0) { throw "npm prune failed (exit $LASTEXITCODE)" }
    }
    finally { Pop-Location }
    Write-Host "    OK node_modules pruned to production-only" -ForegroundColor Green
}
else {
    Write-Host "[ 3/6 ] Skipping npm prune (-SkipNpmPrune)" -ForegroundColor Gray
}

# Step 4: Generate ServerFiles.wxs
Write-Host "[ 4/6 ] Generating ServerFiles.wxs..." -ForegroundColor Yellow
& "$InstallerDir\Generate-ServerFiles.ps1" `
    -ServerBuildDir "$ServerDir\build" `
    -OutputFile     "$InstallerDir\ServerFiles.wxs"
Write-Host "    OK ServerFiles.wxs updated" -ForegroundColor Green

# Step 5: Generate ServerNodeModules.wxs
Write-Host "[ 5/6 ] Generating ServerNodeModules.wxs (~4000+ files, may take a minute)..." -ForegroundColor Yellow
& "$InstallerDir\Generate-ServerNodeModules.ps1" `
    -NodeModulesDir "$ServerDir\node_modules" `
    -OutputFile     "$InstallerDir\ServerNodeModules.wxs"
Write-Host "    OK ServerNodeModules.wxs updated" -ForegroundColor Green

# Step 6: Build MSI
Write-Host "[ 6/6 ] Building MSI package(s)..." -ForegroundColor Yellow

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$builtMsis = [System.Collections.Generic.List[string]]::new()

foreach ($targetAutoCADVersion in $AutoCADVersions) {
    $msiName = "DeepBimMCP-AutoCAD${targetAutoCADVersion}-v$ProductVersion.msi"
    $outputMsi = [System.IO.Path]::Combine($OutputDir, $msiName)
    $outputName = [System.IO.Path]::GetFileNameWithoutExtension($msiName)

    Write-Host "    Building AutoCAD $targetAutoCADVersion -> $msiName" -ForegroundColor Yellow

    if (Test-Path -LiteralPath $outputMsi) {
        Write-Host "    Removing existing MSI with the same name: $outputMsi" -ForegroundColor Gray
        Remove-Item -LiteralPath $outputMsi -Force
    }

    $buildArgs = @(
        "build",
        $WixProj,
        "--configuration", $Configuration,
        "/p:ProductVersion=$ProductVersion",
        "/p:AutoCADVersion=$targetAutoCADVersion",
        "/p:OutputName=$outputName",
        "--nologo"
    )

    dotnet @buildArgs

    if ($LASTEXITCODE -ne 0) { throw "WiX build failed for AutoCAD $targetAutoCADVersion (exit $LASTEXITCODE)" }

    if (-not (Test-Path -LiteralPath $outputMsi)) {
        throw "MSI not found after build: $outputMsi"
    }

    $builtMsis.Add($outputMsi)
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  BUILD SUCCEEDED" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  MSI package(s):" -ForegroundColor Green
foreach ($builtMsi in $builtMsis) {
    Write-Host "    $builtMsi" -ForegroundColor Green
}
Write-Host ""
Write-Host "After installation:" -ForegroundColor Cyan
Write-Host "  1. Restart AutoCAD - plugin loads automatically from bundle folder"
Write-Host "  2. Type MCPSTATUS to verify the plugin is running"
Write-Host "  3. Update VS Code .vscode/mcp.json:"
$serverIndexPath = [System.IO.Path]::Combine($env:ProgramFiles, "DeepBim", "AutoCAD-MCP", "server", "build", "index.js")
Write-Host "     `"args`": [`"$serverIndexPath`"]"
Write-Host "  4. (Path also stored in: HKLM\SOFTWARE\DeepBim\AutoCAD-MCP\ServerPath)"
