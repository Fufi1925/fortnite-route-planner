# Windows-Build und Auslieferung

## Vorhandenes lokales Artefakt

`artifacts/win-x64/RoutePlanner.exe` ist der selbstenthaltende Windows-x64-Build. Das transportierbare Gesamtpaket ist `artifacts/Routenatelier-win-x64.zip`; SHA-256 steht daneben in `artifacts/Routenatelier-win-x64.zip.sha256`, Einzeldateien in `win-x64/SHA256SUMS.txt`.

**Wichtig:** Hier wurde von Linux nach Windows kompiliert. Die EXE wurde hier nicht unter Windows ausgeführt. Ein erfolgreicher Cross-Build beweist weder Start noch korrekte DPI-/Mehrmonitoranzeige. Das ist eine vorläufige Auslieferung zur Windows-Abnahme, kein als auf frischem Windows geprüft ausgegebener Release.

ZIP entpacken und unter Windows `RoutePlanner.exe` starten. Nicht als Administrator starten. Das ZIP enthält eine einzelne selbstenthaltende Anwendungs-EXE **und separate** Dokumentation, Beispiele und Lizenzdateien. Die gesamte Auslieferung ist daher kein Ein-Datei-Paket. .NET kann gebündelte native DLLs beim Start ins benutzerbezogene temporäre `.net`-Verzeichnis entpacken. Keine weitere .NET-Installation nötig. Keine Authenticode-Signatur, kein Installer oder automatischer Updater. Kein Release wurde veröffentlicht.

## Reproduzierbarer Build unter Windows

Voraussetzungen: Windows x64 und .NET SDK **10.0.403**, PowerShell 7. Einmaliger Internetzugang für Microsoft-Runtime-Packs; die fertige App arbeitet offline. Kommandos im Projektverzeichnis:

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
dotnet --version
dotnet run --project tests/RoutePlanner.Tests -c Release
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
dotnet run --project tests/RoutePlanner.UiSmoke -c Release
if ($LASTEXITCODE -ne 0) { throw 'UI smoke failed' }
./scripts/publish.ps1
Get-FileHash artifacts/win-x64/RoutePlanner.exe -Algorithm SHA256
```

Das Skript fixiert Runtime **10.0.14** und verwendet:

```powershell
dotnet publish src/RoutePlanner.App -c Release -r win-x64 --self-contained true `
  -p:RuntimeFrameworkVersion=10.0.14 -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
  -p:PublishTrimmed=false -p:PublishReadyToRun=false -o artifacts/win-x64
```

WPF wird nicht getrimmt. Kein ReadyToRun-Crossgen nötig. `BUILD_INFO.json` benennt SDK/Runtime/RID/Datum; `executionVerified:false` ist bewusst konservativ und bleibt auch nach bloßen Quellcode-Smoke-Tests falsch. Byteidentische ZIPs werden wegen Zeitstempeln und verschiedener Build-Systeme nicht zugesagt. Befehle, SDK und Runtime sind festgehalten; bei Sicherheitsupdates Runtimeversion und Notice-Dateien kontrolliert anpassen.

## Windows-CI und Download

`.github/workflows/windows.yml` führt die beiden ausführbaren Testprogramme und Publish auf `windows-latest` aus und lädt `Routenatelier-win-x64` sowie `offline-verification` mit Logs/Screenshots hoch. Keine geheimen API-Tokens erforderlich; Workflowrechte nur `contents: read`.

Öffentliches Repository: [Fufi1925/fortnite-route-planner](https://github.com/Fufi1925/fortnite-route-planner). Das leere Repository wurde vom Nutzer erstellt. Der vollständige Quellstand wird mit seiner ausdrücklichen Freigabe auf `main` veröffentlicht.

Nach dem Upload läuft der Workflow bei Pushes auf `main` oder `feature/fortnite-route-overlay` sowie manuell. Download: **Actions → Windows build and offline checks → erfolgreicher Lauf → Artifacts → Routenatelier-win-x64**. Das GitHub-Artefakt entpacken, dann das enthaltene Produkt-ZIP entpacken. Bei fehlgeschlagenem UI-Smoke werden Logs hochgeladen, aber kein erfolgreicher Build behauptet. Den tatsächlichen Laufstatus zeigt GitHub Actions. CI verändert keine Branches und veröffentlicht keinen Release.

## Noch nötige Windows-Abnahme

1. Auf frischem Windows 10/11 x64 ohne installiertes .NET starten; Runtime-Entpackung und Standardbenutzerrechte prüfen.
2. Linksklick/Drag/Zoom/Pan, neue Route, Speichern/Laden, Import/Export und absichtlich defekte Datei testen.
3. Kompaktfenster über einem neutralen Editor prüfen: Rückweg, Größenänderung und optional Topmost.
4. 100/125/150/200 % Skalierung, 1080p/4K, Monitore mit unterschiedlicher DPI und Taskleistenlage prüfen.
5. Offlinebetrieb ohne Netzwerk, Speicher/Ressourcen im WPF-Prozess und längere Sitzung messen.

Dabei Fortnite geschlossen lassen. Ein erfolgreicher Windows-Test ersetzt keine Fortnite-Freigabe.
