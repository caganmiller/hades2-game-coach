param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\build\app'),
    [switch]$Tests
)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x C# compiler not found. Install the .NET Framework developer tools.' }
$metadataRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\UnionMetadata'
$metadata = Get-ChildItem -LiteralPath $metadataRoot -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
    Sort-Object { [version]$_.Name } -Descending |
    ForEach-Object { Join-Path $_.FullName 'Windows.winmd' } |
    Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $metadata) { throw 'Windows SDK metadata is missing. Install a Windows 10/11 SDK (tested with 10.0.26100.0).' }
$speech = Join-Path $env:WINDIR 'Microsoft.NET\assembly\GAC_MSIL\System.Speech\v4.0_4.0.0.0__31bf3856ad364e35\System.Speech.dll'
$runtime = Join-Path $env:WINDIR 'Microsoft.NET\assembly\GAC_MSIL\System.Runtime\v4.0_4.0.0.0__b03f5f7f11d50a3a\System.Runtime.dll'
foreach ($required in @($speech, $runtime)) { if (-not (Test-Path -LiteralPath $required)) { throw "Required framework assembly not found: $required" } }
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$options = @('/nologo', '/optimize+', '/debug-', '/platform:x64', '/target:winexe', "/out:$outputRoot\GameCoach.exe", "/win32icon:$repoRoot\assets\game-coach.ico")
if ($Tests) {
    $options = @('/nologo', '/optimize+', '/debug-', '/platform:x64', '/target:exe', '/main:GameCoach.DistributionChecks', "/out:$outputRoot\DistributionChecks.exe")
}
$references = @(
    (Join-Path $framework 'System.Runtime.WindowsRuntime.dll'), $runtime, $metadata, $speech,
    'System.IO.Compression.dll', 'System.IO.Compression.FileSystem.dll', 'System.Windows.Forms.dll',
    'System.Drawing.dll', 'System.Web.Extensions.dll', 'System.Net.Http.dll', 'System.Security.dll', 'Microsoft.CSharp.dll'
) | ForEach-Object { "/reference:$_" }
$sources = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Filter '*.cs' -File | Sort-Object Name | Select-Object -ExpandProperty FullName
if ($Tests) { $sources += Join-Path $repoRoot 'tests\DistributionChecks.cs' }
& $compiler @options @references @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
New-Item -ItemType Directory -Path (Join-Path $outputRoot 'assets') -Force | Out-Null
foreach ($name in @('game-coach.ico', 'game-coach.png')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot "assets\$name") -Destination (Join-Path $outputRoot "assets\$name") -Force
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'scripts\export_recap_pdf.py') -Destination $outputRoot -Force
Write-Output "Build succeeded: $outputRoot"
