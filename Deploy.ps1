<#
.SYNOPSIS
    Builds SkyrimForge.PrismaUi and copies it into its own Mod Organizer 2 mod folder.

.PARAMETER Root
    The SKSE\Plugins folder of the mod. Defaults to the SkyrimForge.PrismaUi mod.

.PARAMETER NoBuild
    Copies the last build instead of running Build.ps1.

.EXAMPLE
    .\Deploy.ps1
#>
[CmdletBinding()]
param(
    [string] $Root = "$env:LOCALAPPDATA\ModOrganizer\Skyrim Special Edition\mods\SkyrimForge.PrismaUi\SKSE\Plugins",
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'

$running = Get-Process SkyrimSE, SkyrimVR -ErrorAction SilentlyContinue
if ($running) {
    throw 'Skyrim is running. Close it before deploying, or the loaded DLLs cannot be replaced.'
}

$modRoot = Split-Path (Split-Path $Root -Parent) -Parent
$mods = Split-Path $modRoot -Parent

$pluginHost = Join-Path $mods 'SkyrimForge\SKSE\Plugins\SkyrimForge\SkyrimForge.PluginHost.dll'
if (-not (Test-Path $pluginHost)) {
    throw "$pluginHost does not exist. Deploy SkyrimForge itself first."
}

if (-not $NoBuild) {
    & (Join-Path $PSScriptRoot 'Build.ps1')
}

$framework = Join-Path $Root 'SkyrimForge'
$library = Join-Path $framework 'SharedLibraries\SkyrimForge.PrismaUi'
$plugin = Join-Path $framework 'Plugins\SkyrimForge.PrismaUi.ExamplePlugin'
$src = Join-Path $PSScriptRoot 'src'
$output = 'bin\Release\net10.0\SKSE\Plugins\SkyrimForge'
$views = Join-Path $src 'SkyrimForge.PrismaUi.PluginTemplate\templates\PrismaUiPlugin\PrismaUI\views'

$files = @(
    @{ From = "$src\SkyrimForge.PrismaUi.Native\x64\Release\SkyrimForge.PrismaUi.Native.dll"; To = $library }
    @{ From = "$src\SkyrimForge.PrismaUi\$output\SharedLibraries\SkyrimForge.PrismaUi\SkyrimForge.PrismaUi.dll"; To = $library }
    @{ From = "$src\SkyrimForge.PrismaUi.ExamplePlugin\$output\Plugins\SkyrimForge.PrismaUi.ExamplePlugin\SkyrimForge.PrismaUi.ExamplePlugin.dll"; To = $plugin }
    @{ From = "$src\SkyrimForge.PrismaUi.ExamplePlugin\$output\Plugins\SkyrimForge.PrismaUi.ExamplePlugin\SkyrimForge.PrismaUi.ExamplePlugin.deps.json"; To = $plugin }
)
foreach ($page in Get-ChildItem $views -Recurse -File) {
    $files += @{ From = $page.FullName; To = Join-Path $modRoot "PrismaUI\views$($page.DirectoryName.Substring($views.Length))" }
}

Write-Host "Deploying to $modRoot" -ForegroundColor Cyan
$failed = 0
foreach ($file in $files) {
    if (-not (Test-Path $file.From)) {
        throw "$($file.From) is missing. Run .\Build.ps1 first."
    }

    $name = Split-Path $file.From -Leaf
    New-Item -ItemType Directory -Force -Path $file.To | Out-Null
    Copy-Item $file.From -Destination $file.To -Force

    if ((Get-FileHash $file.From).Hash -eq (Get-FileHash (Join-Path $file.To $name)).Hash) {
        Write-Host "  ok   $name"
    }
    else {
        Write-Host "  FAIL $name" -ForegroundColor Red
        $failed++
    }
}

if ($failed -gt 0) {
    throw "$failed file(s) differ from the build output."
}

if (-not (Test-Path (Join-Path $mods '*\SKSE\Plugins\PrismaUI.dll'))) {
    Write-Host ''
    Write-Host 'No mod supplies PrismaUI.dll. The PrismaUiApi constructor throws without it.' -ForegroundColor Yellow
}

Write-Host ''
Write-Host 'Deployed.' -ForegroundColor Green
