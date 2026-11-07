# Architektur 0.4

## Komponenten

`RoutePlanner.Core`: typisierte Routen, Kartendefinitionen und Orte, reine Geometrie, räumlicher Spawnindex, JSON-Validierung, atomare Dateispeicherung und `ManualRouteSession`. Keine UI- oder Spielprozessabhängigkeit. Der einzige getrennte Netzwerk-Client ruft öffentliche statische Kartenbilder ab. Der frühere Demo-Provider ist entfernt.

`RoutePlanner.App`: WPF mit genau einem expliziten SDK-Paket, Microsoft.Web.WebView2. `MainWindow` verwaltet die Planung; `MapView` zeichnet freie Arbeitsfläche beziehungsweise eigenes Kartenbild und Routen. `MapImages` validiert und normalisiert lokale Bilder. `CompactWindow` zeigt die Route und registriert ausschließlich bewusste manuelle Bestätigungen. `WebMapWindow` lädt erst nach Klick die Original-Webseite Fortnite.GG in einem separaten WebView2-Profil. Keine native Datenbrücke oder Übernahme der Website-Routen. `Dialogs` liefert dunkle eigene Bestätigungen. Gewöhnliches WindowChrome, keine Spiel-Hooks. `App.OpenPlannerOnStartup` erlaubt den isolierten UI-Test ohne zweite Anwendungsinstanz.

## Koordinaten und Datenherkunft

Ursprung oben links, X nach rechts, Y nach unten; beide Achsen einschließlich 0 bis 3000 KE. KE sind ein normiertes lokales System und keine Zusage einer Fortnite-Georeferenzierung oder Meterumrechnung. Nur quadratische Kartenbilder verhindern eine unterschiedliche Achsenskalierung. Prozentanzeige = Radius / 3000 × 100.

Leere Arbeitsfläche hat Kennung `WORKSPACE-3000-V1`. Importierte Karten werden mit Originalfarben als BGRA32 auf maximal 2048 Pixel normalisiert und als PNG gespeichert. Ihre Kennung ist `MAP-` plus SHA-256 des gespeicherten Inhalts. Die gespeicherte Karte wird beim Laden erneut gegen diesen Hash geprüft. Pfad wird ausschließlich aus validierter Kennung innerhalb des eigenen `maps`-Ordners gebildet; JSON enthält keinen auszuführenden oder frei steuerbaren Dateipfad. Alte Graustufen-Caches behalten ihre bisherige Kennung und Routenbindung.

Jede Route und jeder Ortsdatensatz gehört zu einer Kartenkennung. Eine unbekannte oder fehlende Karte sperrt die Bearbeitung und Darstellung der zugehörigen Route, erhält aber Exportierbarkeit. Keine alternative Karte wird stillschweigend untergelegt. Ortsimporte benötigen dieselbe Kennung und eine Quellenangabe. Importierte Orte werden als statische Daten übernommen, nicht automatisch als eigene Sichtung bestätigt. Eigene manuelle Orte und Quellen sind persistent. Statische Orte sind kein aktueller Spawn-Nachweis.

## Geometrie

Für Punkt P und Segment A–B: t = clamp(dot(P−A,B−A)/|B−A|²,0,1), Abstand zum Punkt A+t(B−A). Nullsegment wird als Einzelpunkt behandelt. Korridor verwendet das Minimum über Segmente; Wegpunktmodus das Minimum über Punkte. Leere Route hat Abstand unendlich. Grenze inklusive mit 1e-7 KE Rechentoleranz. Indexzellen 150 KE; erweiterte Segment-Bounding-Boxes reduzieren Kandidaten, dann exakte Prüfung. Ergebnisse werden gegen vollständige Suche getestet.

Bildschirm = Welt × Maßstab + Offset; Maßstab min(Breite,Höhe)/3240 × Zoom. Zoom 0,6–8; Cursorposition bleibt beim Mausradzoom stabil. Route und Radien bleiben in Weltkoordinaten. Bilder erscheinen mit Originalfarben und voller Deckkraft. Ein aktiver Suchbereich ersetzt auch die gezeichneten Radiusflächen.

## Speicherung und Migration

Schema 4 enthält `routes`, `maps` und `activities`. Maximal 2 MiB JSON, Tiefe 20, doppelte Schlüssel und fremde Felder abgelehnt. Identität, Kartenkennung, endliche Koordinaten und Datenmengen validiert. Schreiben über temporäre Datei im selben Ordner, Datenträger-Flush und Ersetzen; kein Schutzversprechen gegen jeden Hardwareausfall. Beschädigte Startbibliothek bleibt vor automatischem Überschreiben gesperrt.

Schema 1/2/3: Routen bleiben erhalten, simuliertes `demoHistory` wird nicht als echter Verlauf migriert. Historische Beispielrouten bleiben gespeichert, werden aber nicht automatisch angezeigt oder gestartet. Sichern geschieht erst bei bewusster Speicheraktion; vorher bleibt die alte Datei erhalten. Bilder sind separate lokale Dateien. Kein automatisches Backup und keine Konfliktauflösung zwischen mehreren Instanzen.

## Tatsächliche manuelle Routengänge

`ManualRouteSession` erstellt eine Routenkopie und misst die vergangene Uhrzeit. Ein Wegpunkt wird ausschließlich durch die Aktion „Wegpunkt erreicht“ bestätigt. Kein Timer erzeugt Positionen oder Fortschritte. Gespeichert werden Anfangszeit, tatsächliche Dauer (begrenzt auf 24 Stunden), bestätigte/gesamte Wegpunkte, Abschlussstatus und ausdrücklich **geplante** Streckenlänge. Diese Länge behauptet keine gemessene Spielerbewegung. Abbrüche bleiben als beendet markiert. Bei Schreibfehler bleibt der noch ungespeicherte Verlauf im Speicher für einen erneuten Speicherversuch.

Keine Live-API, automatische Truhensuche, Bildschirmaufnahme/OCR oder Spielsteuerung. Der UI-Timer aktualisiert ausschließlich die Anzeige der tatsächlichen vergangenen Zeit.

## Öffentliche Karte und Suchbereich

PublicFortniteMapClient erlaubt genau das dokumentierte HTTPS-JSON und das feste PNG-Bildziel von Fortnite-API. Keine Weiterleitung, integrierten Kontozugangsdaten oder Live-Match-Daten. Gesamtzeitlimit 25 Sekunden, JSON maximal 2 MiB, Bild maximal 20 MiB; HTTP-Fehler und Retry-After werden ohne automatische Wiederholung respektiert. Provider-Abrufzeit und Bilddatum werden getrennt gespeichert. Eigene Routen und Karten bleiben bei einem Abruffehler erhalten. Ein neues Kartenbild erhält eine eigene Inhaltskennung; alte Routen werden nicht umprojiziert.

RoutePlan.Area ist ein optionales normiertes Rechteck. Positive Fläche, endliche Werte und Kartengrenzen werden validiert. Auswahl im MapView über den vorhandenen Zoom-/Pan-Transform; Ziehen in jede Richtung wird normalisiert. Ein Bereich ersetzt den Radiusfilter und funktioniert ohne Wegpunkte. Die Query filtert nur tatsächlich gespeicherte Orte der passenden Karte. Alle-Orte-Modus ist während eines Bereichs deaktiviert. Export, Duplikation, Schema-Migration und Kompaktansicht erhalten den Bereich. Keine Generierung fehlender Truhen.

## Original-Webseite

WebMapWindow verwendet Microsoft Edge WebView2 mit automatisch aktualisierter, separat installierter Runtime. Der Browser lädt erst beim Öffnen fortnite.gg und besitzt ein eigenes Profil. Hostobjekte und WebMessage-Zugriff sind deaktiviert; kein Produkt-Skript wird injiziert. Hauptnavigation auf exakten HTTPS-Host begrenzt; Berechtigungsanfragen und Downloads abgewiesen. Website-Unterressourcen bleiben normale Browseranfragen. Native Bibliothek und Website-Zeichnungen werden nicht synchronisiert. ExternalLinks startet nur zwei feste HTTPS-Ziele über die normale Windows-Shell. Quellen, Runtime-Voraussetzungen und Datenschutz: FORTNITE_GG.md.
