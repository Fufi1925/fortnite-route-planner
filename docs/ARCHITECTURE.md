# Architektur

## Schichten

`src/RoutePlanner.Core` ist eine plattformneutrale .NET-10-Bibliothek. Sie enthält typisierte Kartenpunkte, Routen, Spawnindex, Validierung, JSON-/Dateispeicherung und Demo-Provider. Keine UI-, Netzwerk- oder Prozessschnittstellen.

`src/RoutePlanner.App` ist WPF, ohne Drittanbieter-Pakete. `MainWindow` koordiniert Bearbeitung, Dateidialoge und Demo-Timer. `MapView` zeichnet eine neutrale Vektorkarte per `DrawingContext`; dieselbe Komponente wird im `CompactWindow` schreibgeschützt verwendet. Normale Fensterrahmen liefern Verschieben/Skalieren. Der Datenordner ist im Konstruktor für den UI-Test isolierbar, nicht über unvalidierte importierte Pfade steuerbar.

`tests/RoutePlanner.Tests` ist ein ausführbarer, plattformneutraler Testläufer mit Exitcode. `tests/RoutePlanner.UiSmoke` öffnet echte WPF-Fenster unter Windows, ruft reguläre eigene Button-Handler auf, prüft Speicherung/Filter/Demo/Kompakt und erzeugt Screenshots. Kein Test startet oder sucht Fortnite.

## Koordinaten und Filter

Planare Karte mit Ursprung oben links, X nach rechts, Y nach unten; beide Achsen einschließlich 0 bis 3000. **KE sind synthetische Karteneinheiten**, keine geografischen Breiten-/Längengrade und keine bestätigten Fortnite-Meter. Prozentanzeige = `Radius / 3000 * 100`. Die euklidische Länge bleibt unabhängig von Zoom/DPI.

Für Punkt P und Segment A–B: `t = clamp(dot(P-A,B-A)/|B-A|²,0,1)`, nächster Punkt `A+t(B-A)`. Degenerierte Segmente werden als Einzelpunkt behandelt. Der Korridorabstand ist das Minimum über alle Segmente; Wegpunktmodus verwendet das Minimum über alle Punkte. Bei null/leerer Route ist der Abstand unendlich und die Ergebnismenge leer. Radiusgrenze inklusive, mit absoluter Toleranz `1e-7 KE` gegen Gleitkommafehler. Darstellung und Filter verwenden dasselbe Modell; bei Nullradius bleibt nur die Grenze mit dieser Rechentoleranz.

`SpawnIndex` gruppiert Punkte in 150-KE-Zellen. Segment-/Wegpunkt-Bounding-Boxes werden um den Radius erweitert; anschließend erfolgt die genaue Distanzprüfung aller Kandidaten, ohne Ausdünnung. Tests vergleichen beide Modi bei zufälligen Routen mit vollständiger Suche. Wegen maximal 500 Routenpunkten und einem endlichen 21×21-Zellbereich ist die Verarbeitung begrenzt; der 100.000-Punkte-Test ist keine Garantie für alle Worst-Case-Routen. Standarddaten: nur 600 Punkte.

Ansichtstransformation: `screen = world * scale + offset`; `scale = min(width,height)/3240 * zoom`. Der Zoom ist auf 0,6–8 begrenzt. Mausradzoom hält die Weltposition unter dem Cursor fest. Panning ändert nur den Viewport. Dragging begrenzt Wegpunkte auf die Kartengrenzen. Zeichnung wird auf Karte und Control begrenzt. Bei fremder Kartenversion werden keine Route/Spawns auf die Demo-Geometrie gezeichnet.

## Speicherung und Integrität

Pfad: `%LOCALAPPDATA%\Routenatelier\library.json`, Schema 2. Schema 1 (Routen mit denselben Pflichtfeldern, ohne zwingenden Verlauf) wird explizit auf Schema 2 migriert. Unbekannte Schemata werden abgelehnt. Karten-Datenversion und Dateischema sind getrennte Begriffe.

Dateien werden bis maximal 2 MiB gelesen, JSON-Tiefe maximal 20, unbekannte Felder/doppelte Schlüssel/fehlende Routenfelder oder Koordinaten verworfen. Zahlen müssen endlich sein; Limits: 200 Routen, 500 Punkte/Route, Namen 120 Zeichen ohne Steuerzeichen. Import enthält genau eine Route und keinen Verlauf, erhält eine neue lokale ID und führt keinen Inhalt aus. Exporte enthalten nur die gewählte Route. Pfade kommen aus eigenem festem Speicherpfad oder bewusst bedienten OS-Dateidialogen, niemals aus dem JSON.

Speichern validiert zuerst, schreibt eine temporäre Datei im selben Verzeichnis, flush-t auf den Datenträger und ersetzt die Zieldatei per `File.Move(overwrite:true)`. Das verhindert gewöhnliche Teil-Schreibdateien; keine Zusage gegen jedes Dateisystem-/Stromausfallszenario, keine verschlüsselte Speicherung und keine automatische Backup-Rotation. Fehler werden in deutscher UI angezeigt. Bei beschädigter Startbibliothek bleibt Speichern gesperrt, bis die Datei bewusst entfernt oder erfolgreich erneut geladen wurde. Löschfunktion entfernt exakt die Bibliothek, kein rekursives Verzeichnis. Die App ist für einen laufenden Prozess pro Benutzer gedacht; gleichzeitige Instanzen besitzen keine Konfliktsynchronisation.

## Demo und Datenherkunft

`IOfflineEventProvider` liefert ausschließlich `OfflineDemoProvider`: deterministische `Started`, `Position`, `Finished`-Ereignisse. Der UI-Timer spielt alle 700 ms ein Ereignis ab; Ereigniszeit 30 Sekunden pro Wegpunkt plus 10 Sekunden Abschluss ist eine Testzeit. `DemoSession` prüft Reihenfolge/Zeit/Koordinaten, arbeitet auf einer Routenkopie und erstellt eine als Demo benannte Zusammenfassung. Keine Eliminierungen, echten Match-IDs oder erfundenen Fortnite-Live-Werte.

Ein `ApprovedLiveProvider` ist absichtlich **nicht vorhanden**; kein inaktiver Netzwerkcode oder spekulativer Anschluss. Ein zukünftiger Live-Vertrag benötigt zunächst eine positive Compliance-Entscheidung und eine gesonderte Daten-/Statussemantik. Fehlende API ist hier kein Fehlerzustand: es gibt ausschließlich Offlinebetrieb. Manuelle Spawn-Bestätigungen sind flüchtige UI-Sitzungsdaten und werden nicht zu Match-Statistiken umgedeutet.

## Fenster und Betrieb

WPF-Manifest: normaler Benutzer, `PerMonitorV2`, kein `uiAccess`. Initiale Kompaktplatzierung nutzt die primäre Windows-Arbeitsfläche in DIP. Andere Monitore sind über normalen Fensterzug möglich; „Oben rechts“ setzt auf den primären Monitor zurück. Keine gespeicherten offscreen Positionen. Standardmäßig kein Topmost, keine globale Tastaturregistrierung, kein Click-through und kein Eingabeeingriff. DPI-Verhalten und besondere Arbeitsflächen benötigen weiterhin reale Windows-Abnahme.
