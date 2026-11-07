# Testbericht 0.4

Stand: 2026-11-07. Lokale Umgebung Linux x64, SDK 10.0.403. Der [Windows-Lauf 42356295734](https://github.com/Fufi1925/fortnite-route-planner/actions/runs/42356295734) hat 42 Kernprüfungen, den WPF-Test inklusive tatsächlichem HTTPS-Kartenabruf und den Start der selbstenthaltenden EXE erfolgreich ausgeführt. Die echte Screenshot-Abnahme führte anschließend zu höherem Kartenkontrast. Für das endgültige Release sind erneut dessen erfolgreiche CI-Ausgaben und das Artefakt `offline-verification` maßgeblich; eine bloße Kompilierung wird nicht als Windows-Ausführung ausgegeben.

## Lokal tatsächlich ausgeführt

- `dotnet run --project tests/RoutePlanner.Tests -c Release`: 43 erfolgreiche Prüfungen, 0 Fehler.
- `dotnet build tests/RoutePlanner.UiSmoke -c Release --no-restore`: Windows-UI-Test und Anwendung kompiliert.
- `python3 scripts/audit-source.py`: ergänzende Prüfung von Anwendungsdateien, Manifest und Abhängigkeiten.

Die vollständigen lokalen Ausgaben stehen in `docs/test-results/core-tests.txt`, `windows-build.txt` und `source-audit.txt`. Das ältere `windows-publish.txt` dokumentiert den ursprünglichen Publish-Lauf; für den 0.3-Build sind ausschließlich die neuen CI-Ausgaben maßgeblich.

## Kernprüfungen

Distanz zu Segmenten mit Projektion/Endpunkten/Nullsegmenten, leere Routen und Nullradius, inklusive Radiusgrenze und Rechentoleranz, Kartenlimits, Wegpunkt-/Korridormodus, Index gegen vollständige Suche, JSON-Roundtrip und Validierungsfälle, Schema-1/2-Migration ohne Übernahme simulierter Historie, passende Kartenkennung, atomare Dateiaustausche, Erhalt beschädigter Dateien, unabhängige Duplikation, Karten-/Ortsdaten inklusive ungültiger Pfade und Quellen sowie echte manuelle Routensitzung mit expliziten Fortschritten und gemessener Zeit. Die Routenidentität bleibt bei Namensänderungen und UI-Bindungen stabil. Eine Prüfung kontrolliert die ausschließlich monochrome Anwendungsfarbpalette. Testdaten existieren nur innerhalb der Testprogramme, nicht als automatisch geladene Produktdaten.

Der Leistungstest verarbeitet 100.000 Testpunkte und 100 Filterabfragen. Messwerte stehen im Protokoll und beziehen sich auf den Kern, nicht auf eine reale Fortnite-Partie oder das WPF-Gesamtbudget.

## Windows-UI und ausführbare Datei

Der UI-Test startet ohne voreingestellte Daten und setzt Wegpunkte über tatsächliche eigene Button-Handler. Er prüft Speichern/Laden, Export/Import, Duplikation, eigene Orte, Ortsquellen, Radiusfilter, PNG-Import, Ablehnung defekter/nichtquadratischer Bilder, Neustart mit tatsächlich gespeicherter Karte, Kompaktfenster, Topmost, manuelle Fortschritte und gemessene Zeit. Ohne die Aktion „Wegpunkt erreicht“ entsteht kein Fortschritt. Echte Screenshots entstehen aus gerenderten WPF-Fenstern, nicht durch Bildgenerierung.

`scripts/smoke-published.ps1` startet die selbstenthaltende EXE auf dem Windows-Runner und erkennt das Planerfenster. Es beendet ausschließlich die eigene gestartete Testinstanz; kein Spielprozess ist erforderlich oder angesprochen.

Die Testumgebung von GitHub Actions ist Windows Server 2025 x64. Reale Windows-10/11-Nutzung mit wechselnder DPI, Mehrmonitorbedingungen, nativen Dateidialogen und physischen Mausbewegungen bleibt zusätzlich manuell abzunehmen. Es gibt keinen echten Fortnite-Test und keine Live-Datenbehauptung.

## Ergänzungen 0.3

Zusätzliche Prüfungen für Rechtecknormalisierung, Randpunkte, ungültige und unvollständige Bereichskoordinaten, Bereichsfilter ohne Wegpunkte, Wiederherstellung des Radiusfilters, Duplikation und Export/Import. Netzwerkprüfungen verwenden ausschließlich Test-HTTP-Handler für die dokumentierten statischen Ziele: unbekannte Host-/Bildadressen, Weiterleitungen, Rate-Limits und Retry-After, Größenlimits und ungültige Bilder werden verworfen. Keine Truhen werden aus POI-Daten erzeugt.

Der Windows-UI-Workflow setzt ROUTE_VERIFY_PUBLIC_MAP=1 und ruft zusätzlich tatsächlich das öffentliche Kartenbild per HTTPS ab. Er prüft die sichtbare Truhen-Datenlücke, Fan-Content-Kennzeichnung und eine gespeicherte Bereichsauswahl auf dem real geladenen Kartenbild. Für diesen zusätzlichen Integrationsschritt ist Internet erforderlich; die lokalen Tests und die lokale Planung funktionieren weiterhin ohne Internet. Maßgeblich bleiben der tatsächliche Windows-Lauf und dessen Protokolle, nicht bloß diese Beschreibung.

## Ergänzungen 0.4

Lokal ausgeführt: 43 Kernprüfungen einschließlich exakter Website-/Link-URL-Abgrenzung. WebView2-SDK 1.0.4314.35 aus offizieller NuGet-Quelle wiederhergestellt; WPF und UI-Test kompiliert. Tatsächlicher Browseraufruf der Originalseite auf Linux: farbige Karte geladen, Spawns aufgeklappt, Chests/Buried Chests/Rare Chests über ihre Schalter aktiviert und Stiftwerkzeug geöffnet. Kein Datensatz exportiert oder übernommen.

Der folgende Windows-Workflow prüft zusätzlich Farberhalt beim Import/Speichern/Laden, den tatsächlichen Onlineabruf der farbigen Karte, die echte Fortnite.GG-Ansicht in WebView2, sichtbare und aktivierbare Kistenfilter, geladene Kartenkacheln, Aufnahme über CapturePreviewAsync und Rückweg zum lokalen Planer. Produktcode injiziert keine Skripte; DOM-Prüfungen sind ausschließlich im Testprogramm. Der EXE-Test öffnet über UI Automation nur die eigene auf dem Runner gestartete App und prüft den gepackten WebView2-Loader. Maßgeblich sind erst die tatsächlichen erfolgreichen CI-Ausgaben; fehlende Runtime oder Netzwerkfehler werden nicht als Erfolg ausgegeben.
