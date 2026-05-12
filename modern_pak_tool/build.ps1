param(
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $root "bin"
$src = Join-Path $root "src"
$assets = Join-Path $root "..\assets"
$logoPng = Join-Path $assets "super_logo.png"
$logoIco = Join-Path $out "super_logo.ico"
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
$wpf = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\WPF"

if (!(Test-Path $csc)) {
    throw "C# compiler not found at $csc"
}

if (!(Test-Path $logoPng)) {
    throw "App logo not found at $logoPng"
}

if ($Clean -and (Test-Path $out)) {
    Remove-Item -LiteralPath $out -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $out | Out-Null

function New-IcoFromPng($pngPath, $icoPath) {
    Add-Type -AssemblyName System.Drawing

    $source = [System.Drawing.Image]::FromFile($pngPath)
    $bitmap = $null
    $graphics = $null
    $stream = $null
    $file = $null
    $writer = $null

    try {
        $size = 256
        $bitmap = New-Object System.Drawing.Bitmap -ArgumentList $size, $size
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($source, 0, 0, $size, $size)

        $stream = New-Object System.IO.MemoryStream
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngBytes = $stream.ToArray()

        $file = [System.IO.File]::Create($icoPath)
        $writer = New-Object System.IO.BinaryWriter -ArgumentList $file
        $writer.Write([UInt16]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]1)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$pngBytes.Length)
        $writer.Write([UInt32]22)
        $writer.Write($pngBytes)
    }
    finally {
        if ($writer -ne $null) { $writer.Dispose() }
        elseif ($file -ne $null) { $file.Dispose() }
        if ($stream -ne $null) { $stream.Dispose() }
        if ($graphics -ne $null) { $graphics.Dispose() }
        if ($bitmap -ne $null) { $bitmap.Dispose() }
        if ($source -ne $null) { $source.Dispose() }
    }
}

New-IcoFromPng $logoPng $logoIco

$hostOut = Join-Path $out "PakEngineHost.exe"
$appOut = Join-Path $out "ModernPakTool.exe"

& $csc /nologo /target:exe /platform:x86 /optimize+ "/out:$hostOut" `
    (Join-Path $src "PakEngineHost.cs")
if ($LASTEXITCODE -ne 0) { throw "PakEngineHost build failed." }

& $csc /nologo /target:winexe /platform:x86 /optimize+ "/out:$appOut" `
    "/win32icon:$logoIco" `
    "/resource:$logoPng,ModernPakTool.super_logo.png" `
    "/reference:$(Join-Path $wpf 'PresentationCore.dll')" `
    "/reference:$(Join-Path $wpf 'PresentationFramework.dll')" `
    "/reference:$(Join-Path $wpf 'WindowsBase.dll')" `
    /reference:System.Xaml.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Drawing.dll `
    (Join-Path $src "ModernPakTool.cs")
if ($LASTEXITCODE -ne 0) { throw "ModernPakTool build failed." }

Copy-Item -LiteralPath (Join-Path $root "..\engine.dll") -Destination $out -Force
Copy-Item -LiteralPath (Join-Path $root "..\lualibdll.dll") -Destination $out -Force

Write-Host "Built $out\ModernPakTool.exe"
