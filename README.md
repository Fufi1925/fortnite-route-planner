# Fortnite Route Planner · Routenatelier

Windows-Desktop-App zum **offline** Planen von Routen mit Quellcode unter MIT-Lizenz. Version 0.1.0 verwendet ausschließlich eine neutrale Beispielkarte und 600 generierte mögliche Spawnpunkte. **Keine Fortnite-Verbindung, keine echten Fortnite-Kartendaten, keine Live-Erkennung und keine Freigabe für den Einsatz mit Fortnite.**

## Starten

Vorgesehenes öffentliches Repository: `Fufi1925/fortnite-route-planner`. Die aktuelle GitHub-Integration hat die Repository-Erstellung mit HTTP 403 abgelehnt; die Veröffentlichung ist vorbereitet, aber noch nicht erfolgt. Nach Erstellung und Upload entstehen Windows-Builds unter [GitHub Actions](https://github.com/Fufi1925/fortnite-route-planner/actions): **Windows build and offline checks → erfolgreicher Lauf → Artifacts → Routenatelier-win-x64**. Quellcode und Buildskripte sind vollständig enthalten; generierte EXE-/ZIP-Dateien werden als CI-Artefakte ausgeliefert.

Das selbst gebaute Paket liegt unter `artifacts/Routenatelier-win-x64.zip`. Entpacken und `RoutePlanner.exe` als normaler Benutzer starten. Die Anwendung ist selbstenthaltend; eine separate .NET-Installation ist nicht nötig. Windows x64 erforderlich. Das Paket ist nicht signiert. Der ursprüngliche lokale Build wurde unter Linux für Windows kompiliert; dessen Windows-Ausführung wurde nicht geprüft. Sicherheitsfunktionen von Windows nicht abschalten.

Einrichten aus Quellcode: [.NET SDK 10.0.403](https://dotnet.microsoft.com/download/dotnet/10.0) installieren und unter Windows im Projektordner ausführen:

```powershell
dotnet run --project src/RoutePlanner.App -c Release
```

## Bedienung

1. Die mitgelieferte Demo-Route erkunden oder **Neu** wählen. Linksklick auf die Karte setzt einen Wegpunkt; vorhandene Punkte lassen sich ziehen.
2. Rechte oder mittlere Maustaste zum Verschieben, Mausrad zum Zoomen. **Einpassen** setzt den Ausschnitt zurück.
3. Wegpunkte in der Liste auswählen, umsortieren, entfernen oder X/Y bearbeiten. Der weiße erste Punkt ist der Start; der letzte trägt eine Zielfahne.
4. Radius und Modus einstellen: Abstand zur gesamten Strecke oder Abstand zu einzelnen Wegpunkten. Die alternative Anzeige in Prozent bezeichnet den Anteil an der Kartenbreite; sie verändert die zugrunde liegenden Karteneinheiten nicht.
5. Offene Kreise sind **mögliche Demo-Spawns**, keine bestätigten Truhen. Ein Klick auf einen sichtbaren Spawn setzt eine **manuelle Demo-Bestätigung** (Quadrat). Sie gilt nur in dieser Sitzung und wird bei Routenwechsel/Demo-Neustart verworfen. Keine Behauptung über eine echte Partie.
6. Namen vergeben und **Speichern**. **Laden** liest die lokale Bibliothek erneut. **Duplizieren**, **Löschen**, **Import** und **Export** stehen links bereit. Routenwechsel und Schließen fragen bei ungespeicherten Änderungen nach.
7. **Demo abspielen** führt deterministische Testereignisse im Zeitraffer aus. Die simulierten Sekunden sind keine gemessene Spielzeit. Die Zusammenfassung wird lokal gespeichert.
8. **Kompaktansicht** öffnet ein gewöhnliches, skalierbares Fenster oben rechts auf dem primären Bildschirm. Per Titelleiste verschieben, auch auf andere Monitore. „Immer oben“ ist standardmäßig aus und nur für Tests über neutralen Anwendungen vorgesehen. **Zur Planung** oder das Schließen des Kompaktfensters bringt den Planer zurück.

## Daten und Grenzen

- Speicherung: `%LOCALAPPDATA%\Routenatelier\library.json`; maximal 200 Routen und die letzten 200 Demo-Zusammenfassungen. Kein Server, keine Telemetrie, keine Werbung.
- Pro Route maximal 500 Punkte, Radius 0–1500 KE; JSON maximal 2 MiB. Import führt keine Inhalte aus. Unbekannte Kartenversionen bleiben exportierbar, erhalten aber keine Darstellung auf der Demo-Karte, Spawnfilter oder Demo-Simulation.
- **Lokale Daten löschen** entfernt Bibliothek und Verlauf nach Rückfrage. Exporte separat löschen. Deinstallation: App-Verzeichnis entfernen und bei Bedarf das obige Datenverzeichnis löschen. Keine Dienste, Treiber, Registry-Einträge oder Autostarts.
- Kein Click-through, kein globaler Hotkey, kein Screenshot/OCR, kein freigegebener Live-Adapter. Kein offizielles Epic-, Overwolf- oder Tracker-Produkt.
- Windows 10/11, mehrere Monitore, reale Mausinteraktionen und Skalierungen sind noch praktisch abzunehmen. Primäre Arbeitsfläche für Erstplatzierung; kein Speichern der Fensterposition.

## Entwicklung und Nachweise

```powershell
dotnet run --project tests/RoutePlanner.Tests -c Release
dotnet run --project tests/RoutePlanner.UiSmoke -c Release  # nur Windows
./scripts/publish.ps1
```

Die Tests sind eigenständige Programme ohne Testpakete; **`dotnet test` führt diese Tests nicht aus**. Rückgabecode 0 bedeutet Erfolg. Der Windows-Smoke-Test öffnet nur die eigene App mit isoliertem temporärem Datenordner.

Weitere Unterlagen: [Quellen und Compliance](docs/COMPLIANCE_AND_SOURCES.md), [Architektur](docs/ARCHITECTURE.md), [Testbericht](docs/TEST_REPORT.md), [Build und Auslieferung](docs/RELEASE_BUILD.md), [Abhängigkeiten](docs/DEPENDENCIES.md). Den aktuellen CI-Status und die Download-Artefakte zeigt [GitHub Actions](https://github.com/Fufi1925/fortnite-route-planner/actions).
