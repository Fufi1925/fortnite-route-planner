# Tatsächlich ausgeführte Prüfung

Prüfdatum **2026-11-07 UTC**. Umgebung: Linux x86-64 (Kernel 6.18.57), .NET SDK **10.0.403**. Windows, Wine und eine Windows-VM standen nicht zur Verfügung. Keine Fortnite-Installation, kein echter Spielprozess und kein Anti-Cheat wurden gestartet oder untersucht.

## Ergebnisse

| Prüfung | Tatsächliches Ergebnis |
|---|---|
| Ausführbare Kernprüfungen | **27 erfolgreich, 0 fehlgeschlagen**, Exit 0 |
| WPF-App und Windows-UI-Smoke-Testprojekt kompilieren | Erfolgreich, **0 Warnungen / 0 Fehler**, Exit 0 |
| Selbstenthaltendes `win-x64` Publish | Erfolgreich, Exit 0; echte PE32+-GUI-EXE für x86-64 erzeugt |
| Zusätzliche Quellcode-/Manifestprüfung | Erfolgreich; 14 Anwendungsdateien ohne beanstandete API-Muster, keine expliziten `PackageReference`s, normale Benutzerrechte |
| Veröffentlichten Abhängigkeitsgraph gelesen | Nur eigene App/Core sowie Microsoft.NETCore.App und Microsoft.WindowsDesktop.App Runtime 10.0.14 |
| Paketintegrität | Einzeldatei-SHA-256, ZIP-CRC und ZIP-SHA-256 erfolgreich geprüft; eine Anwendungs-EXE, keine benötigten DLL-Nebendateien im Paket |
| Start der EXE unter Windows | **Nicht ausgeführt** |
| WPF-Smoke-Test ausführen / Screenshots | **Nicht ausgeführt**, nur kompiliert; Windows-CI vorbereitet, kein CI-Lauf vorhanden |
| Frisches Windows, DPI/Mehrmonitor, WPF-Speichermessung | **Nicht ausgeführt** |
| Fortnite-Integration / echter In-Game-Test | Bewusst **nicht implementiert / nicht durchgeführt** |

## Ausgeführte Befehle und Protokolle

Die folgenden Kommandos wurden im Projektordner mit `/tmp/route-dotnet/dotnet` ausgeführt. Für die lokale Sandbox wurden `DOTNET_CLI_HOME=/tmp/route-cli`, `NUGET_PACKAGES=/tmp/route-nuget` und `DOTNET_CLI_TELEMETRY_OPTOUT=1` gesetzt. .NET wurde nach Sandbox-Problemen mit vom automatischen Review genehmigten erweiterten Ausführungsrechten gestartet; keine Spiel- oder Systemrechte in der Anwendung hinzugefügt.

```text
dotnet run --project tests/RoutePlanner.Tests -c Release
dotnet build tests/RoutePlanner.UiSmoke -c Release
dotnet publish src/RoutePlanner.App -c Release -r win-x64 --self-contained true
  -p:RuntimeFrameworkVersion=10.0.14 -p:PublishSingleFile=true
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
  -p:PublishTrimmed=false -p:PublishReadyToRun=false -o artifacts/win-x64
python3 scripts/audit-source.py
file artifacts/win-x64/RoutePlanner.exe
```

Vollständige finale Build-/Testausgaben: [core-tests.txt](test-results/core-tests.txt), [windows-build.txt](test-results/windows-build.txt), [windows-publish.txt](test-results/windows-publish.txt), [source-audit.txt](test-results/source-audit.txt). `file` erkannte: `PE32+ executable for MS Windows 6.00 (GUI), x86-64, 9 sections`. Das PE-Feld ist keine Zusage der Unterstützung alter Windows-Versionen.

Anfängliche Versuche scheiterten an schreibgeschütztem CLI-Benutzerverzeichnis und NuGet-Synchronisationsdateien in der Sandbox. Der erste WPF-Build fand fehlende `System.IO`-Imports und mehrdeutige Typnamen; diese wurden korrigiert. Die obige Ergebnistabelle beschreibt den abschließend geprüften Stand. Es gab keine verschwiegenen finalen Testfehler.

## Testabdeckung

- Punkt–Segment: senkrechte Projektion, vor/nach Endpunkt, degenerierte Segmente und Punkt exakt auf Strecke.
- Routen: null/leere Liste, Einzelpunkt, Nullradius, mehrere Abschnitte, Länge und Wegpunktmodus.
- Radius: inklusive Grenze, Gleitkommatoleranz, außerhalb liegende Punkte; beide Filtermodi bei 80 deterministisch zufälligen Routen gegen vollständige Suche.
- Kartengrenzen inklusive Ecken; negative Werte, NaN, Infinity und zu viele Punkte werden verworfen.
- Import/Export-Roundtrip, mitgelieferte Beispieldatei, Schema-1-Migration, unbekanntes Schema, alte/fremde Kartenkennung.
- Beschädigtes JSON, null, doppelte Schlüssel, fremde Felder, fehlende Pflichtfelder/Koordinaten, Radius-/Enum-/Namens-/Größen-/ID-Limits.
- Unabhängiges Duplizieren, tatsächliches Speichern/Laden/Ersetzen/Löschen auf temporärem Datenträger und Erhalten defekter Originaldatei nach Ladefehler.
- Deterministische Provider-Ereignisse durch tatsächlichen `DemoSession`-Reducer bis zur gespeicherten Zusammenfassung, ungültige Reihenfolge/Zeit/Position.
- Ein leerer Name während des Tippens unterbricht den Kartenfilter nicht; Speichern validiert ihn weiterhin.

Die unter Windows noch auszuführende UI-Prüfung nutzt isolierte Daten, echte WPF-Controls und Button-Handler. Sie prüft Laden, Namensänderung/Speichern, Radius-Anbindung, Zoom/Einpassen, Demo-Timer und Verlauf, Kompaktfenster und Topmost/Rückweg sowie eine leere Route. Sie prüft keine physischen Mausbewegungen oder nativen Import-/Export-Dateidialoge. Diese Punkte stehen zusätzlich auf der manuellen Abnahmeliste in `RELEASE_BUILD.md`.

## Leistungsstichprobe

Letzter protokollierter Release-Test: **100.000 generierte Punkte**, Indexaufbau **10,2 ms**, 100 vollständige Filterabfragen im Mittel **13,33 ms/Abfrage**. Ergebnismenge mit vollständiger Suche verglichen. Momentaufnahme im Testprozess: **16,8 MiB verwalteter Speicher**, **70,3 MiB Working Set**. Das ist eine Linux-Core-Messung, keine Messung der WPF-App oder ein verbindliches Budget. Keine Worst-Case-Zusage für 500 Segmente mit großem Radius. Die Anwendung verwendet standardmäßig 600 Testpunkte und einen gezeichneten Canvas statt Hunderter UI-Controls.

## Abnahme und verbleibende Grenzen

Offline-Kern, persistente Routen, Radiusfilter, klare Demo-/Spawnkennzeichnung, Demo-Zusammenfassung und Windows-Artefakt sind implementiert und soweit oben beschrieben geprüft. Kompaktfenster und Kartensteuerung sind kompiliert, ihre reale Windows-Bedienung bleibt offen. Keine aktuellen Fortnite-Daten, keine Kartenbild-/Datenanbieterintegration und keine Live-Freigabe. Das gewöhnliche Fenster ist keine zugesicherte Fullscreen-Overlay-Lösung.

Eine Instanz pro Benutzer vorgesehen; kein konkurrierendes Bearbeiten derselben Bibliothek. Daten sind lokales Klartext-JSON; keine Verschlüsselung, Cloud oder automatische Backups. Unsignierter Build, keine Installer-/Updater-Prüfung. Aktuelle Rechte und Regeln vor jedem späteren Fortnite-Einsatz erneut prüfen. Quellcode-Musterprüfung und manuelle Prüfung der Imports/Projektdateien fanden keine Spielprozess-, Hook-, Speicher-, Capture-, Treiber-, Netzwerkabgriff- oder Eingabeautomatisierungstechnik; das ist keine Anti-Cheat-Freigabe oder Banngarantie.
