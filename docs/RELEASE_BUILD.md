# Windows-Build und Download

Öffentliches Repository: [Fufi1925/fortnite-route-planner](https://github.com/Fufi1925/fortnite-route-planner).

Veröffentlichte Versionen stehen unter [Releases](https://github.com/Fufi1925/fortnite-route-planner/releases). Das vollständige Paket `Routenatelier-win-x64.zip` enthält eine selbstenthaltende EXE sowie Anleitung, Beispiele und Lizenzen. Alternativ kann `RoutePlanner.exe` einzeln geladen werden; die Lizenzdateien liegen zusätzlich in `Lizenzen.zip`. Keine separate .NET-Installation nötig. Für die integrierte Onlinekarte muss Microsoft Edge WebView2 Runtime installiert sein; sie wird nicht gebündelt. Ohne sie: konkrete Fehlermeldung, normaler Browser und offizielle Microsoft-Installation als Auswege. .NET kann native Laufzeitkomponenten in den benutzerbezogenen temporären `.net`-Ordner entpacken. Kein Installer, Dienst, Treiber, Autostart oder Authenticode-Zertifikat.

## Lokal bauen

Windows x64, PowerShell 7 und .NET SDK 10.0.403:

```powershell
dotnet run --project tests/RoutePlanner.Tests -c Release
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
dotnet run --project tests/RoutePlanner.UiSmoke -c Release
if ($LASTEXITCODE -ne 0) { throw 'UI smoke failed' }
./scripts/publish.ps1
./scripts/smoke-published.ps1
```

Runtime 10.0.14, win-x64, self-contained, PublishSingleFile/IncludeNativeLibrariesForSelfExtract/EnableCompressionInSingleFile eingeschaltet; PublishTrimmed und PublishReadyToRun ausgeschaltet. Ausgabe: `artifacts/win-x64/RoutePlanner.exe`, `artifacts/Routenatelier-win-x64.zip` und Prüfsummen. SDK/Runtime in BUILD_INFO.json. Diese Datei entsteht vor dem nachfolgenden EXE-Starttest; dessen Ergebnis steht separat im Prüfprotokoll. Keine Zusage byteidentischer ZIP-Zeitstempel über verschiedene Build-Systeme.

## GitHub Actions

`windows.yml` führt Kernprüfungen, den vollständigen eigenen WPF-Smoke-Test einschließlich öffentlichem Kartenabruf, Farberhalt, Bereichsauswahl und tatsächlicher Fortnite.GG-WebView2-Ansicht mit den drei aktivierten Kistenfiltern, Publish und den tatsächlichen Start der veröffentlichten EXE inklusive Öffnen der Webkarte mit dem gepackten nativen Loader aus. Artefakte: `Routenatelier-win-x64` sowie `offline-verification` mit Logs und echten Screenshots. Die Tests arbeiten auf Windows Server 2025 im GitHub-Runner und isolierten Daten, ohne Fortnite. Das ist keine vollständige manuelle Windows-10/11-/DPI-Abnahme.

`release.yml` veröffentlicht ausschließlich aus einem erfolgreichen Windows-Lauf auf main. Eingaben sind dessen Run-ID und die Version; passende Release Notes müssen unter `docs/releases/<tag>.md` liegen. Nur dieser manuell gestartete Job erhält `contents: write` und `actions: read`. Er prüft Commit-Zuordnung, Paket-/Einzeldatei-Prüfsummen und Testausgaben. Erst nach erfolgreichem Upload aller Dateien wird der Entwurf veröffentlicht. Bereits öffentliche Releases werden nicht überschrieben.

Vor der Auslieferung werden die hochgeladenen EXE-/ZIP-Digests gegen den verifizierten Windows-Build verglichen. Der ursprüngliche Linux-Build dient nicht als Beleg für eine Windows-Ausführung.

## Weiterhin manuell abzunehmen

Windows 10/11 ohne installiertes .NET, unterschiedliche DPI/Monitorwechsel, reale Mausbewegungen und native Dateidialoge, längere Sitzungen und Ressourcen unter realer Nutzung. Unsignierte Dateien entsprechend der eigenen Windows-Sicherheitsrichtlinien prüfen; Sicherheitsfunktionen nicht deaktivieren. Der Offline-Planer bietet keine Fortnite-Freigabe oder aktuelle Fortnite-Daten.
