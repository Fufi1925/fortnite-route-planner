# Testbericht 0.2

Stand: 2026-11-07. Lokale Umgebung Linux x64, SDK 10.0.403. Windows-Lauf, Screenshots und Start der veröffentlichten EXE erfolgen anschließend über den [Windows-Workflow](https://github.com/Fufi1925/fortnite-route-planner/actions). Maßgeblich sind dessen tatsächliche Ausgaben und das Artefakt `offline-verification`; eine bloße Kompilierung wird nicht als Windows-Ausführung ausgegeben.

## Lokal tatsächlich ausgeführt

- `dotnet run --project tests/RoutePlanner.Tests -c Release`: 31 erfolgreiche Prüfungen, 0 Fehler.
- `dotnet build tests/RoutePlanner.UiSmoke -c Release --no-restore`: Windows-UI-Test und Anwendung kompiliert.
- `python3 scripts/audit-source.py`: ergänzende Prüfung von Anwendungsdateien, Manifest und Abhängigkeiten.

Die vollständigen Ausgaben stehen in `docs/test-results/core-tests.txt`, `windows-build.txt` und `source-audit.txt`. Das ältere `windows-publish.txt` dokumentiert den ursprünglichen Publish-Lauf; für den 0.2-Build sind ausschließlich die neuen CI-Ausgaben maßgeblich.

## Kernprüfungen

Distanz zu Segmenten mit Projektion/Endpunkten/Nullsegmenten, leere Routen und Nullradius, inklusive Radiusgrenze und Rechentoleranz, Kartenlimits, Wegpunkt-/Korridormodus, Index gegen vollständige Suche, JSON-Roundtrip und Validierungsfälle, Schema-1/2-Migration ohne Übernahme simulierter Historie, passende Kartenkennung, atomare Dateiaustausche, Erhalt beschädigter Dateien, unabhängige Duplikation, Karten-/Ortsdaten inklusive ungültiger Pfade und Quellen sowie echte manuelle Routensitzung mit expliziten Fortschritten und gemessener Zeit. Die Routenidentität bleibt bei Namensänderungen und UI-Bindungen stabil. Eine Prüfung kontrolliert die ausschließlich monochrome Anwendungsfarbpalette. Testdaten existieren nur innerhalb der Testprogramme, nicht als automatisch geladene Produktdaten.

Der Leistungstest verarbeitet 100.000 Testpunkte und 100 Filterabfragen. Messwerte stehen im Protokoll und beziehen sich auf den Kern, nicht auf eine reale Fortnite-Partie oder das WPF-Gesamtbudget.

## Windows-UI und ausführbare Datei

Der UI-Test startet ohne voreingestellte Daten und setzt Wegpunkte über tatsächliche eigene Button-Handler. Er prüft Speichern/Laden, Export/Import, Duplikation, eigene Orte, Ortsquellen, Radiusfilter, PNG-Import, Ablehnung defekter/nichtquadratischer Bilder, Neustart mit tatsächlich gespeicherter Karte, Kompaktfenster, Topmost, manuelle Fortschritte und gemessene Zeit. Ohne die Aktion „Wegpunkt erreicht“ entsteht kein Fortschritt. Echte Screenshots entstehen aus gerenderten WPF-Fenstern, nicht durch Bildgenerierung.

`scripts/smoke-published.ps1` startet die selbstenthaltende EXE auf dem Windows-Runner und erkennt das Planerfenster. Es beendet ausschließlich die eigene gestartete Testinstanz; kein Spielprozess ist erforderlich oder angesprochen.

Die Testumgebung von GitHub Actions ist Windows Server 2025 x64. Reale Windows-10/11-Nutzung mit wechselnder DPI, Mehrmonitorbedingungen, nativen Dateidialogen und physischen Mausbewegungen bleibt zusätzlich manuell abzunehmen. Es gibt keinen echten Fortnite-Test und keine Live-Datenbehauptung.
