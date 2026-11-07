# Fortnite Route Planner

Ein Windows-Routenplaner mit schwarzer/grauer Oberfläche und farbiger Karte. **Kein App-Login, keine Simulation und keine erfundenen Spielstatistiken.** Eigene lokale Routen plus eine integrierte Ansicht der Original-Webseite Fortnite.GG für Kistenfilter und Zeichnungen.

[Windows-Download](https://github.com/Fufi1925/fortnite-route-planner/releases) · [Build und Tests](https://github.com/Fufi1925/fortnite-route-planner/actions) · [Designentscheidungen](docs/DESIGN.md)

## Starten

Unter **Releases** das Paket `Routenatelier-win-x64.zip` herunterladen, entpacken und `RoutePlanner.exe` starten. Alternativ ist die EXE einzeln verfügbar; Lizenzhinweise gehören zur Auslieferung. Eine separate .NET-Installation und ein Benutzerkonto sind nicht erforderlich. Der Build ist unsigniert.

Für die integrierte Onlinekarte ist **Microsoft Edge WebView2 Runtime** erforderlich (auf Windows 11 vorinstalliert). Falls sie fehlt, bietet die App einen Link zur offiziellen Microsoft-Installation und das Öffnen im normalen Browser an. Die lokale Planung benötigt WebView2 nicht.

## Fortnite.GG: farbige Karte, Kisten und Zeichnen

**Fortnite.GG öffnen ↗** öffnet die unveränderte Original-Webseite in einer eigenen App-Ansicht. **Spawns** über den Pfeil aufklappen, dann die Schalter für **Chests** (normale Kisten), **Buried Chests** (vergrabene Kisten) und **Rare Chests** (seltene Kisten) einschalten. Das **Stiftwerkzeug** auf der Karte öffnet die Zeichen- und Markierungswerkzeuge des Anbieters.

Die Website hat eigene Cookies, mögliche Werbung und eigene Speicher-/Teilenfunktionen. Web-Routen werden **nicht automatisch in die lokale Bibliothek übernommen**. „Zur lokalen Planung“ führt zurück, „Webdaten löschen“ löscht nur die Daten dieser Webansicht. Keine laufenden Matchdaten oder bestätigten Live-Spawns. [Quellenentscheidung, Datenschutz und genaue Grenzen](docs/FORTNITE_GG.md).

## Aktuelle Fortnite-Karte und Suchbereich

**Fortnite-Karte laden** ruft das derzeit vom öffentlichen Anbieter Fortnite-API bereitgestellte Kartenbild ab und speichert es lokal. Bei leerer Erstinstallation erfolgt dieser Abruf einmal automatisch. Bestehende eigene Routen werden nicht automatisch auf eine andere Karte projiziert. Abrufzeit und der vom Server gemeldete Bildstand sind nachvollziehbar; der Anbieter kann nach Patches verzögert sein. Es gibt keine Verbindung zu einer laufenden Partie.

**Bereich auswählen** anklicken, dann auf der Karte mit gedrückter linker Maustaste ein Rechteck ziehen. Der Bereich wird mit der Route gespeichert und filtert tatsächlich vorhandene Orte innerhalb seiner Grenzen, auch ohne Wegpunkte. Währenddessen pausiert der Radiusfilter; **×** entfernt den Bereich und aktiviert ihn wieder.

**Im lokalen Planer sind keine vollständigen Truhendaten enthalten.** Der öffentliche Karten-Endpunkt liefert Bild und POIs, keine Truhenkoordinaten. Die Kistenfilter von Fortnite.GG stehen in der getrennten Original-Webansicht bereit; deren Daten werden nicht kopiert. [Quellenprüfung und genaue Grenzen](docs/CURRENT_MAP_AND_CHESTS.md).

## Was funktioniert

- Neue Routen anlegen, benennen, speichern, laden, duplizieren und löschen.
- Wegpunkte per Klick oder Koordinateneingabe setzen, verschieben, umsortieren und entfernen.
- Zoomen, verschieben und den Ausschnitt einpassen.
- Eigene oder berechtigt nutzbare **quadratische PNG-/JPEG-Karten** öffnen. Das Kartenbild wird mit Originalfarben lokal gespeichert. Alte Graustufen-Caches behalten ihre bisherige Kennung; für eine neue farbige Karte erneut laden/importieren.
- Eigene Orte setzen und dauerhaft speichern; statische Ortsdaten mit passender Kartenkennung importieren.
- Orte nach ihrem Abstand zur Strecke oder zu Wegpunkten filtern. „Alle Orte zeigen“ ist ein ausdrücklich sichtbarer Bearbeitungsmodus ohne Radiusausblendung.
- Routen als JSON exportieren und importieren. Zum Übertragen einer bildgebundenen Route auch das zugehörige Kartenbild mitnehmen.
- Eine Route im normalen Kompaktfenster abarbeiten: **Wegpunkt erreicht** bestätigt jeden Fortschritt bewusst manuell. Der Verlauf speichert tatsächlich vergangene Zeit und manuelle Bestätigungen. Die Distanz ist die **geplante** Streckenlänge, keine gemessene Spielerbewegung.
- Ohne Netzverbindung arbeiten und lokale Daten gezielt löschen.

Eine leere Erstinstallation versucht den öffentlichen Kartenabruf; ohne Internet bleibt die freie Arbeitsfläche nutzbar. Es gibt keine eingebauten fiktiven Inseln, Test-Truhen, Fake-Matches oder voreingestellten Routen. Für die Arbeitsfläche gelten normierte Koordinaten von 0 bis 3000 KE je Achse; 1 % Kartenbreite entspricht 30 KE. **KE sind keine bestätigten Fortnite-Meter.**

## Zu Fortnite und echten Daten

Dies ist eine funktionierende lokale Planungsanwendung. Es gibt **keine Fortnite-Verbindung, automatische Spielerortung, Truhen-Erkennung oder zugelassene Live-Schnittstelle**. Die öffentliche Kartenabbildung wird bei Bedarf online von Fortnite-API geladen, nicht im Programm mitgeliefert. Ein vollständiger lizenzierter Spawn-Datensatz ist nicht enthalten. Deine importierten statischen Orte sind kein Nachweis dafür, dass in einer aktuellen Partie dort eine Truhe existiert.

Nur Bilder und Daten öffnen, die du verwenden darfst. Die App prüft Dateiformat, Integrität und Kartenkennung; sie kann keine Nutzungsrechte für eine fremde Datei erteilen. Der Einsatz zusammen mit Fortnite ist weiterhin nicht pauschal freigegeben. Details: [Compliance und Quellen](docs/COMPLIANCE_AND_SOURCES.md).

## Dateien und Grenzen

Speicherort: `%LOCALAPPDATA%\Routenatelier\library.json`; importierte Karten liegen im Unterordner `maps`. Der lokale Planer hat keine eigene Telemetrie oder Werbung und überträgt keine lokalen Routen. Der öffentliche Bildabruf sendet HTTPS-Anfragen an fortnite-api.com. Beim Öffnen von Fortnite.GG gelten zusätzlich dessen Netzwerkzugriffe, Cookies, Werbung und Bedingungen; das separate Browserprofil liegt unter `web-fortnite-gg`. Maximal 200 Routen, 500 Punkte pro Route, 100 Karten, 10.000 Orte pro Karte und 200 Verlaufseinträge. JSON maximal 2 MiB, Bildimport maximal 20 MiB, quadratische Bilder mit 64–16.000 Pixeln; für die lokale Darstellung auf maximal 2048 Pixel normalisiert.

Schema 1/2/3 wird nach Schema 4 migriert. Eigene alte Routen bleiben erhalten; historische Beispieldaten werden nicht als aktuelle Nutzerdaten angezeigt und simulierte Verlaufswerte werden nicht in den echten Verlauf übernommen. Eine Instanz pro Benutzer vorgesehen. Zum Deinstallieren App-Verzeichnis und bei Bedarf das benutzerbezogene Datenverzeichnis entfernen; exportierte Dateien separat löschen.

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

## Fan-Content-Hinweis

Öffentliches Kartenbild und Fortnite-Inhalte sind nicht Teil der MIT-Lizenz des Programmcodes. Nutzung als kostenlose nichtkommerzielle Fan-App unter den Bedingungen der [Epic Fan Content Policy](https://legal.epicgames.com/epicgames/fan-art-policy). Keine offizielle Zugehörigkeit oder Epic-Zulassung.

Portions of the materials used are trademarks and/or copyrighted works of Epic Games, Inc. All rights reserved by Epic. This material is not official and is not endorsed by Epic.
