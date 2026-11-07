# Compliance und Quellen

Version 0.5: [Live-Anforderung erneut geprüft](LIVE_AUTOMATION_REVIEW.md). Einzelne Truhenöffnungen sind in der geprüften Fortnite-GEP-Referenz nicht dokumentiert; Positionsdokumentation ist keine Freigabe für diese WPF-App. Keine aktive Spielanbindung. Lokale Typfilter umgesetzt, Lootpositionen im Kompaktfenster entfernt, keine Ableitung „leer“ aus der Filteranzeige. Separat positiv geprüft: optionaler normaler Epic-Startauftrag nach dokumentierter OS-Protokollaktivierung, ohne Spielzustandszugriff oder bestätigten Spielstart.

Version 0.4: [Fortnite.GG-Entscheidung und Browsergrenzen](FORTNITE_GG.md). Die unveränderte öffentliche Website wird erst nach Klick als gewöhnlicher Browserinhalt angezeigt. Keine Lizenz zur Übernahme des Website-Codes oder gesamten Truhendatensatzes behauptet; keine Synchronisierung, Prozessanbindung oder Live-Match-Daten.

Prüfstand: 2026-11-07 UTC. Version 0.3 ergänzt einen separat geprüften öffentlichen statischen Kartenabruf und die Bereichsauswahl. Einzelentscheidung, aktuelle Saisonreferenz und Truhen-Datenlücke: [CURRENT_MAP_AND_CHESTS.md](CURRENT_MAP_AND_CHESTS.md). Die normale Kartenanzeige ist keine Live-Partie-Anbindung. Version 0.2 ersetzte die bisherige Simulation durch einen realen lokalen Planer. Sie enthält weder generierte Spielobjekte noch eine behauptete Fortnite-Anbindung. Die offiziellen Quellen aus der ursprünglichen Prüfung sind mit Abrufmetadaten in `SOURCE_AUDIT.json` festgehalten.

## Entscheidung

Eigene Routen, bewusst importierte lokale Kartenbilder und manuelle Einträge werden tatsächlich verarbeitet und gespeichert. Das Entfernen des Demo-Modus ist keine Freigabe für eine Fortnite-Live-Schnittstelle. Eine belastbar zugelassene Integration für diese konkrete WPF-App ist weiterhin nicht nachgewiesen; sie wird deshalb nicht implementiert. Kein echter Fortnite-Test und keine Banngarantie.

| Quelle | Fundstelle / Bedeutung |
|---|---|
| [Epic Terms](https://legal.epicgames.com/epicgames/tos) | Gelesener Stand mit Änderungs-/Wirksamkeitsdatum 2026-09-10; §3 Lizenz-/Inhaltsrechte, §4 Gameplay Integrity. Keine pauschale Erlaubnis für die konkrete Live-/Overlay-App. |
| [Epic Support zu Drittanbieter-Software](https://www.epicgames.com/help/c-34254770/c-40491939/a15028164?lang=de) | Keine individuelle Banngarantie oder offizielle Unterstützung; Verwendung auf eigenes Risiko. |
| [Overwolf Fortnite GEP](https://dev.overwolf.com/ow-native/live-game-data-gep/supported-games/fortnite/) | Dokumentiert u. a. matchStart/matchEnd, location/game_info.location, map/match_info.map, phase, items und match_info.match_stats. Dokumentation allein belegt weder Nutzungsrecht dieser App noch aktuelle Laufzeitverfügbarkeit. Keine zugelassene Quelle für verborgene tatsächlich gespawnte Truhen ermittelt. |
| [GEP Intro](https://dev.overwolf.com/ow-native/live-game-data-gep/live-game-data-gep-intro/) | Host-gebundene Registrierung über overwolf.games.events.setRequiredFeatures und Ereignislistener; zeitweise Ausfälle und Statusprüfung. Kein allgemeiner Fortnite-REST-Dienst für WPF. |
| [Overwolf Native](https://dev.overwolf.com/ow-native/getting-started/overview/) / [Electron](https://dev.overwolf.com/ow-electron/getting-started/overview/) | Eigene SDK-Hosts; Doku-Banner nennt seit 2026-11-01 zusätzliche Monetarisierungsanforderungen für App-Zulassung. Kein SDK eingebunden, keine Registrierung vorgenommen. |
| [Overwolf Developer Terms](https://legal.overwolf.com/docs/overwolf/developers/developer-terms) | §§2.2, 2.4 und 3.5: begrenzte Lizenz, Annahme/API-Zugang und Verteilung unter Plattformbedingungen. Private Nutzung zu zweit ist keine belegte Ausnahme. |
| [Tracker Desktop](https://tracker.gg/fortnite/desktop-app/) | Produktvergleich, keine übertragbare Freigabe. Keine Inhalte übernommen. |

## Funktionsentscheidungen 0.2

| Funktion | Entscheidung |
|---|---|
| Lokale Planung, Geometrie, Import/Export, eigene Orte | Implementiert; ausschließlich bewusste Benutzereingaben und lokale Dateien. |
| Eigene Kartenbilder | Implementiert, nur PNG/JPEG und quadratische Abbildung. Keine mitgelieferte Fortnite-Karte. Separat erlaubter öffentlicher Bildabruf mit Fan-Content-Hinweis gemäß neuer Quellenentscheidung; kein Game-Event- oder Match-Abruf. Dateiformatprüfung ist kein Rechtebeleg. Nutzer muss die Datei rechtmäßig verwenden dürfen. |
| Importierte Ortsdaten | Quellenangabe und passende Kartenkennung erforderlich. Als statisch behandelt; keine Garantie tatsächlicher Truhen in einer Partie. Keine öffentlichen Spawn-Daten ohne belegte Lizenz übernommen. |
| Manuelle Routengänge | Tatsächliche vergangene Zeit und ausdrücklich bestätigte Schritte; keine Spielposition, Eliminierungen oder erfundenen Match-Ereignisse. |
| Kompaktes Fenster | Gewöhnliches WPF-Fenster, optional normales Topmost. Keine Zusage zur Zulässigkeit mit Fortnite; reale Tests ausschließlich über neutralen Anwendungen. |
| Fortnite-Position, Events, Matchdaten | Für diese App ungeklärt und nicht implementiert. Overwolf wäre ein eigener Host-/Lizenz-/Review-Entwurf. |
| Tatsächliche verborgene Truhen | Keine genehmigte Quelle ermittelt; nicht verfügbar und ausgeschlossen. |
| Bildschirm/OCR | Keine klare Zulässigkeitsprüfung für diesen Fall; nicht aktiviert, kein API-Workaround. |
| Speicherzugriff, Injection, Render-Hooks, Treiber, Netzwerkabgriff, Eingabemakros | Vollständig ausgeschlossen. |

Die Arbeitsfläche ist kein echtes Fortnite-Kartenbild. Importierte Bilder werden nicht als offiziell, aktuell oder korrekt lizenziert bestätigt. Kartenkennung ist ein Inhaltsfingerabdruck, keine automatische Season-Verifikation. Änderungen an Daten müssen bewusst importiert werden. Rechte, Attribution und Aktualität jeder fremden Quelle bleiben gesondert zu belegen.

Vor Einsatz mit Fortnite sind der konkrete Overlay-/Datenanwendungsfall und gegebenenfalls Anbieterzulassung, Host, private Verteilung sowie Bild-/Datenrechte erneut belastbar zu klären. Bis dahin arbeitet diese Anwendung als eigenständiger lokaler Planer.
