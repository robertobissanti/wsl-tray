# Compila WslTray.exe con il compilatore C# incluso in Windows (.NET Framework 4.x).
# Nessun SDK da installare.
$ErrorActionPreference = 'Stop'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw "csc.exe non trovato: $csc" }

$root = $PSScriptRoot
$out  = Join-Path $root 'bin'
New-Item -ItemType Directory -Force $out | Out-Null

& $csc /nologo /target:winexe /optimize+ `
    /out:"$out\WslTray.exe" `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll `
    (Join-Path $root 'src\WslTray.cs')
if ($LASTEXITCODE -ne 0) { throw "Compilazione fallita ($LASTEXITCODE)" }
"Creato: $out\WslTray.exe"
