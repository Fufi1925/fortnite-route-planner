param([string]$RuntimeVersion = "10.0.14")
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$output = Join-Path $PWD "artifacts/win-x64"
if (Test-Path $output) { Remove-Item $output -Recurse -Force }
dotnet publish src/RoutePlanner.App -c Release -r win-x64 --self-contained true `
    "-p:RuntimeFrameworkVersion=$RuntimeVersion" -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false -p:PublishReadyToRun=false -o $output
if ($LASTEXITCODE -ne 0) { throw "Publish fehlgeschlagen: $LASTEXITCODE" }
# Portable PDBs are optional debugging symbols, not runtime dependencies.
Get-ChildItem $output -Filter *.pdb -File | Remove-Item
Copy-Item README.md,LICENSE -Destination $output
Copy-Item docs,third-party,examples -Destination $output -Recurse
@"
ROUTE PLANNER 0.2.1 - LOKALE ROUTENPLANUNG

RoutePlanner.exe unter Windows x64 starten. Keine .NET-Installation notwendig.
Eigene Karten, Routen und Orte; keine Fortnite-Verbindung oder Live-Erkennung.
Fortschritte werden ausschliesslich manuell bestaetigt.
Keine Freigabe fuer Nutzung mit Fortnite. README.md und docs lesen.

Die EXE enthaelt App und Runtime. Native Runtime-Dateien koennen von .NET
beim Start ins temporaere .net-Verzeichnis des Benutzers entpackt werden.
README, Lizenzdateien, Beispiele und docs bleiben separate Dateien im Paket.
"@ | Set-Content (Join-Path $output "STARTEN.txt") -Encoding utf8
$sdk = dotnet --version
@{ sdk = $sdk; runtime = $RuntimeVersion; rid = "win-x64"; configuration = "Release";
   builtUtc = [DateTime]::UtcNow.ToString("o"); executionVerified = $false } |
    ConvertTo-Json | Set-Content (Join-Path $output "BUILD_INFO.json") -Encoding utf8
$lines = Get-ChildItem $output -File -Recurse | Sort-Object FullName | ForEach-Object {
    $relative = [System.IO.Path]::GetRelativePath($output, $_.FullName).Replace('\','/')
    "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $relative
}
$lines | Set-Content (Join-Path $output "SHA256SUMS.txt") -Encoding ascii
$zip = Join-Path $PWD "artifacts/Routenatelier-win-x64.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path "$output/*" -DestinationPath $zip
"{0}  Routenatelier-win-x64.zip" -f (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant() |
    Set-Content "artifacts/Routenatelier-win-x64.zip.sha256" -Encoding ascii
Write-Host "Paket: $zip"
