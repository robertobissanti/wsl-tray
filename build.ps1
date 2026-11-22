# Builds WslTray.exe with the C# compiler that ships with Windows (.NET Framework 4.x).
# No SDK required.
#   .\build.ps1            -> bin\WslTray.exe
#   .\build.ps1 -Package   -> also dist\WslTray-<version>.zip + SHA256SUMS.txt
param([switch]$Package)
$ErrorActionPreference = 'Stop'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw "csc.exe not found: $csc" }

$root = $PSScriptRoot
$out  = Join-Path $root 'bin'
New-Item -ItemType Directory -Force $out | Out-Null

$sources = Get-ChildItem (Join-Path $root 'src') -Filter *.cs | ForEach-Object FullName
$ico = Join-Path $root 'assets\app.ico'
$iconArg = @()
if (Test-Path $ico) { $iconArg = @("/win32icon:$ico") }

& $csc /nologo /target:winexe /optimize+ /codepage:65001 `
    /out:"$out\WslTray.exe" `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll `
    @iconArg $sources
if ($LASTEXITCODE -ne 0) { throw "Build failed ($LASTEXITCODE)" }
"Built: $out\WslTray.exe"

if ($Package) {
    $ver  = (Get-Item "$out\WslTray.exe").VersionInfo.ProductVersion
    $dist = Join-Path $root 'dist'
    New-Item -ItemType Directory -Force $dist | Out-Null
    $zip  = Join-Path $dist "WslTray-$ver.zip"
    Compress-Archive -Force -DestinationPath $zip -Path "$out\WslTray.exe", (Join-Path $root 'LICENSE'), (Join-Path $root 'README.md')
    Copy-Item "$out\WslTray.exe" $dist -Force
    $sums = foreach ($f in (Get-Item "$dist\WslTray.exe", $zip)) {
        "{0}  {1}" -f (Get-FileHash $f -Algorithm SHA256).Hash.ToLower(), $f.Name
    }
    Set-Content (Join-Path $dist 'SHA256SUMS.txt') $sums -Encoding ascii
    "Packaged: $zip"
    $sums
}
