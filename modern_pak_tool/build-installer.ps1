param(
    [string]$InnoSetupCompiler = "",
    [switch]$KeepStage
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$bin = Join-Path $root "bin"
$stage = Join-Path $root "obj\installer-stage"
$dist = Join-Path $root "dist"
$iss = Join-Path $root "installer\ModernPakTool.iss"

function Resolve-Iscc {
    param([string]$ExplicitPath)

    if (![String]::IsNullOrWhiteSpace($ExplicitPath)) {
        if (Test-Path -LiteralPath $ExplicitPath) {
            return (Resolve-Path -LiteralPath $ExplicitPath).Path
        }
        throw "Inno Setup compiler not found at $ExplicitPath"
    }

    $command = Get-Command "iscc.exe" -ErrorAction SilentlyContinue
    if ($command -ne $null) {
        return $command.Source
    }

    $candidates = @()
    if (![String]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
        $candidates += (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe")
    }
    if (![String]::IsNullOrWhiteSpace(${env:ProgramFiles(x86)})) {
        $candidates += (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe")
    }
    if (![String]::IsNullOrWhiteSpace($env:ProgramFiles)) {
        $candidates += (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    }

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "Inno Setup 6 compiler was not found. Install Inno Setup 6 or pass -InnoSetupCompiler with the full path to ISCC.exe."
}

function Assert-File {
    param([string]$Path)

    if (!(Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required release file missing: $Path"
    }
}

function Reset-Directory {
    param([string]$Path)

    $resolvedRoot = (Resolve-Path -LiteralPath $root).Path
    $parent = Split-Path -Parent $Path
    if (!(Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    if (Test-Path -LiteralPath $Path) {
        $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
        if (!$resolvedPath.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to clean path outside project root: $resolvedPath"
        }
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

Write-Host "Building Kiro..."
& (Join-Path $root "build.ps1") -Clean
if ($LASTEXITCODE -ne 0) {
    throw "Application build failed."
}

$appExe = Join-Path $bin "ModernPakTool.exe"
$hostExe = Join-Path $bin "PakEngineHost.exe"
$engineDll = Join-Path $bin "engine.dll"
$luaDll = Join-Path $bin "lualibdll.dll"
$setupIcon = Join-Path $bin "kiro_app_icon.ico"

Assert-File $appExe
Assert-File $hostExe
Assert-File $engineDll
Assert-File $luaDll
Assert-File $setupIcon

Write-Host "Staging compact install layout..."
Reset-Directory $stage
$stageEngine = Join-Path $stage "Engine"
New-Item -ItemType Directory -Force -Path $stageEngine | Out-Null

Copy-Item -LiteralPath $appExe -Destination $stage -Force
Copy-Item -LiteralPath $hostExe -Destination $stageEngine -Force
Copy-Item -LiteralPath $engineDll -Destination $stageEngine -Force
Copy-Item -LiteralPath $luaDll -Destination $stageEngine -Force

$forbidden = @(
    (Join-Path $stage "PAKMAKER.exe"),
    (Join-Path $stage "RecoverySmoke.exe"),
    (Join-Path $stage "kiro_app_icon.ico")
)
foreach ($path in $forbidden) {
    if (Test-Path -LiteralPath $path) {
        throw "Forbidden file was staged: $path"
    }
}

Write-Host "Probing staged legacy engine..."
Push-Location $stageEngine
try {
    & (Join-Path $stageEngine "PakEngineHost.exe") probe
    if ($LASTEXITCODE -ne 0) {
        throw "Staged engine probe failed."
    }
}
finally {
    Pop-Location
}

$iscc = Resolve-Iscc $InnoSetupCompiler
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$legacyInstallerOutputs = @(
    (Join-Path $dist "PAKToolInstallation.exe"),
    (Join-Path $dist "PAKToolInstallation.exe.sha256")
)
foreach ($legacyOutput in $legacyInstallerOutputs) {
    if (Test-Path -LiteralPath $legacyOutput) {
        Remove-Item -LiteralPath $legacyOutput -Force
    }
}

Write-Host "Compiling installer with $iscc..."
& $iscc $iss "/O$dist"
if ($LASTEXITCODE -ne 0) {
    throw "Installer build failed."
}

$installer = Join-Path $dist "KiroSetup.exe"
Assert-File $installer

$hash = Get-FileHash -LiteralPath $installer -Algorithm SHA256
$checksumPath = $installer + ".sha256"
Set-Content -LiteralPath $checksumPath -Encoding ASCII -Value ($hash.Hash + "  KiroSetup.exe")

Write-Host "Built $installer"
Write-Host "SHA256 written to $checksumPath"

if (!$KeepStage) {
    if (Test-Path -LiteralPath $stage) {
        $resolvedRoot = (Resolve-Path -LiteralPath $root).Path
        $resolvedStage = (Resolve-Path -LiteralPath $stage).Path
        if (!$resolvedStage.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to clean path outside project root: $resolvedStage"
        }
        Remove-Item -LiteralPath $stage -Recurse -Force
    }
    $obj = Join-Path $root "obj"
    if (Test-Path -LiteralPath $obj) {
        $remaining = @(Get-ChildItem -LiteralPath $obj -Force)
        if ($remaining.Count -eq 0) {
            Remove-Item -LiteralPath $obj -Force
        }
    }
    Write-Host "Removed temporary installer staging layout."
}
