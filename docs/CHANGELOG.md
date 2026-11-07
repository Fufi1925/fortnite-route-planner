# Änderungen 0.1.0

Neues eigenständiges Projekt im zuvor leeren Workspace; keine vorhandene Anwendung oder Nutzeränderung ersetzt.

- WPF-Offline-Routenplaner mit neutraler Vektorkarte, Punktbearbeitung, Zoom/Pan und deutscher UI.
- Routenverwaltung mit sicherem JSON-Import/-Export, lokaler Bibliothek, expliziter Migration und Löschfunktion.
- Korridor-/Wegpunktradius, räumlicher Spawnindex und transparente Demo-/Versionskennzeichnung.
- Gewöhnliches Kompaktfenster, optionale normale Topmost-Einstellung, Rückweg und DPI-Manifest.
- Deterministischer Offline-Provider, simulierte Positionsfolge und lokaler Demo-Verlauf.
- 27 ausführbare Kernprüfungen, kompiliertes Windows-Smoke-Testprojekt, Windows-CI und Paket-/Prüfsummenskripte.
- Quellenprüfung mit hartem Verzicht auf ungeklärte Fortnite-Integration, fremde Karten und sämtliche verbotenen Techniken.

## Möglicher PR

Titel: `Add offline route planner with neutral demo data and Windows build`

Beschreibung: Fügt einen selbstständigen WPF-Planer hinzu, der eigene Routen auf einer generierten Karte speichert und mögliche Demo-Spawns nach Radius filtert. Ein normales Kompaktfenster und simulierte Rundenzusammenfassungen funktionieren ohne Fortnite-Anbindung. 27 Kernprüfungen bestanden unter Linux; Windows-x64-Publish und UI-Smoke-Projekt kompiliert. Windows-Ausführung/DPI und reale Bedienung noch abzunehmen. Keine Freigabe für Nutzung mit Fortnite und kein Live-Adapter.

Der ursprüngliche Stand wurde lokal auf `feature/fortnite-route-overlay` erstellt. Auf ausdrücklichen Wunsch des Nutzers wird der vollständige Quellcode als neues öffentliches Repository `Fufi1925/fortnite-route-planner` veröffentlicht. Im neuen Repository wird `main` initial befüllt; kein bestehender Hauptbranch wird überschrieben. Kein PR und kein Release vorgesehen. Der Windows-Workflow läuft auch auf `main`.
