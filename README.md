# Fortnite Route Planner

Ein lokaler Windows-Routenplaner mit einer durchgehend schwarzen und grauen Oberfläche. **Kein Login, keine Simulation, keine automatisch erzeugten Orte und keine erfundenen Spielstatistiken.** Du arbeitest mit deinen eigenen Routen, Kartenbildern und Daten.

[Windows-Download](https://github.com/Fufi1925/fortnite-route-planner/releases) · [Build und Tests](https://github.com/Fufi1925/fortnite-route-planner/actions) · [Designentscheidungen](docs/DESIGN.md)

## Starten

Unter **Releases** das Paket `Routenatelier-win-x64.zip` herunterladen, entpacken und `RoutePlanner.exe` starten. Alternativ ist die EXE einzeln verfügbar; Lizenzhinweise gehören zur Auslieferung. Eine separate .NET-Installation und ein Benutzerkonto sind nicht erforderlich. Der Build ist unsigniert.

## Was funktioniert

- Neue Routen anlegen, benennen, speichern, laden, duplizieren und löschen.
- Wegpunkte per Klick oder Koordinateneingabe setzen, verschieben, umsortieren und entfernen.
- Zoomen, verschieben und den Ausschnitt einpassen.
- Eigene oder berechtigt nutzbare **quadratische PNG-/JPEG-Karten** öffnen. Das Kartenbild wird lokal gespeichert und für die monochrome Darstellung in Graustufen umgewandelt.
- Eigene Orte setzen und dauerhaft speichern; statische Ortsdaten mit passender Kartenkennung importieren.
- Orte nach ihrem Abstand zur Strecke oder zu Wegpunkten filtern. „Alle Orte zeigen“ ist ein ausdrücklich sichtbarer Bearbeitungsmodus ohne Radiusausblendung.
- Routen als JSON exportieren und importieren. Zum Übertragen einer bildgebundenen Route auch das zugehörige Kartenbild mitnehmen.
- Eine Route im normalen Kompaktfenster abarbeiten: **Wegpunkt erreicht** bestätigt jeden Fortschritt bewusst manuell. Der Verlauf speichert tatsächlich vergangene Zeit und manuelle Bestätigungen. Die Distanz ist die **geplante** Streckenlänge, keine gemessene Spielerbewegung.
- Ohne Netzverbindung arbeiten und lokale Daten gezielt löschen.

Die App startet mit einer leeren Arbeitsfläche. Es gibt keine eingebauten fiktiven Inseln, Test-Truhen, Fake-Matches oder voreingestellten Routen. Für die Arbeitsfläche gelten normierte Koordinaten von 0 bis 3000 KE je Achse; 1 % Kartenbreite entspricht 30 KE. **KE sind keine bestätigten Fortnite-Meter.**

## Zu Fortnite und echten Daten

Dies ist eine funktionierende lokale Planungsanwendung. Es gibt **keine Fortnite-Verbindung, automatische Spielerortung, Truhen-Erkennung oder zugelassene Live-Schnittstelle**. Eine offizielle aktuelle Fortnite-Karte beziehungsweise ein lizenzierter Spawn-Datensatz ist nicht enthalten. Deine importierten statischen Orte sind kein Nachweis dafür, dass in einer aktuellen Partie dort eine Truhe existiert.

Nur Bilder und Daten öffnen, die du verwenden darfst. Die App prüft Dateiformat, Integrität und Kartenkennung; sie kann keine Nutzungsrechte für eine fremde Datei erteilen. Der Einsatz zusammen mit Fortnite ist weiterhin nicht pauschal freigegeben. Details: [Compliance und Quellen](docs/COMPLIANCE_AND_SOURCES.md).

## Dateien und Grenzen

Speicherort: `%LOCALAPPDATA%\Routenatelier\library.json`; importierte Karten liegen im dortigen Unterordner `maps`. Keine Telemetrie, Werbung oder Cloudübertragung. Maximal 200 Routen, 500 Punkte pro Route, 100 Karten, 10.000 Orte pro Karte und 200 Verlaufseinträge. JSON maximal 2 MiB, Bildimport maximal 20 MiB, quadratische Bilder mit 64–16.000 Pixeln; für die lokale Darstellung auf maximal 2048 Pixel normalisiert.

Schema 1/2 wird nach Schema 3 migriert. Eigene alte Routen bleiben erhalten; historische Beispieldaten werden nicht als aktuelle Nutzerdaten angezeigt und simulierte Verlaufswerte werden nicht in den echten Verlauf übernommen. Eine Instanz pro Benutzer vorgesehen. Zum Deinstallieren App-Verzeichnis und bei Bedarf das benutzerbezogene Datenverzeichnis entfernen; exportierte Dateien separat löschen.

## Entwickeln

.NET SDK 10.0.403 unter Windows installieren und im Projektordner ausführen:

```powershell
dotnet run --project src/RoutePlanner.App -c Release
dotnet run --project tests/RoutePlanner.Tests -c Release
dotnet run --project tests/RoutePlanner.UiSmoke -c Release
./scripts/publish.ps1
```

Die Testprogramme liefern Exitcodes; `dotnet test` führt sie nicht aus. Der Windows-Test arbeitet mit isolierten temporären Dateien und erstellt echte Screenshots der WPF-Oberfläche. Die dort erzeugten Testeingaben werden nicht mit der Anwendung ausgeliefert.

Weitere Unterlagen: [Architektur](docs/ARCHITECTURE.md), [Prüfstand](docs/TEST_REPORT.md), [Windows-Build](docs/RELEASE_BUILD.md), [Abhängigkeiten](docs/DEPENDENCIES.md).
