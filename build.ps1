# Builds WslTray.exe with the C# compiler that ships with Windows (.NET Framework 4.x).
# No SDK required.
$ErrorActionPreference = 'Stop'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw "csc.exe not found: $csc" }

$root = $PSScriptRoot
$out  = Join-Path $root 'bin'
New-Item -ItemType Directory -Force $out | Out-Null

$sources = Get-ChildItem (Join-Path $root 'src') -Filter *.cs | ForEach-Object FullName
& $csc /nologo /target:winexe /optimize+ /codepage:65001 `
    /out:"$out\WslTray.exe" `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll `
    $sources
if ($LASTEXITCODE -ne 0) { throw "Build failed ($LASTEXITCODE)" }
"Built: $out\WslTray.exe"
