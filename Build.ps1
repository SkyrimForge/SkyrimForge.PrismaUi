<#
.SYNOPSIS
    Builds SkyrimForge.PrismaUi, including the native shim that dotnet cannot build.

.DESCRIPTION
    dotnet cannot build a C++ project, so the shim is built here with MSBuild.

.EXAMPLE
    .\Build.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Find-MSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) {
        throw "vswhere.exe not found at $vswhere. Install the ""Desktop development with C++"" workload in Visual Studio."
    }

    $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if (-not $msbuild) {
        throw "MSBuild not found. Install the ""Desktop development with C++"" workload in Visual Studio."
    }

    return $msbuild
}

$solution = Join-Path $PSScriptRoot 'SkyrimForge.PrismaUi.slnx'
$shim = Join-Path $PSScriptRoot 'src\SkyrimForge.PrismaUi.Native\SkyrimForge.PrismaUi.Native.vcxproj'

# A build that writes elsewhere would otherwise leave old files where Deploy.ps1 reads them.
foreach ($project in 'SkyrimForge.PrismaUi', 'SkyrimForge.PrismaUi.ExamplePlugin') {
    $output = Join-Path $PSScriptRoot "src\$project\bin\Release"
    if (Test-Path -LiteralPath $output) {
        Remove-Item -LiteralPath $output -Recurse -Force
    }
}

Write-Host 'Building managed projects...' -ForegroundColor Cyan
& dotnet build $solution -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Managed build failed.' }

$msbuild = Find-MSBuild
Write-Host "Building native shim using $msbuild" -ForegroundColor Cyan
& $msbuild $shim -p:Configuration=Release -p:Platform=x64 -nologo -v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Native build failed.' }

Write-Host ''
Write-Host 'Built.' -ForegroundColor Green
