# Compliance und Quellen

Prüfdatum: **2026-11-07 (UTC)**. Die unten genannten Seiten wurden an diesem Tag per HTTPS abgerufen, HTML-Inhalt gelesen und relevante Fundstellen ausgewertet. Abrufmetadaten und SHA-256 der abgerufenen HTML-Seiten stehen in `SOURCE_AUDIT.json`. Das dokumentiert einen Prüfstand, keine dauerhaft gültige Erlaubnis. Keine Anbieter wurden kontaktiert, keine individuellen Freigaben eingeholt und kein Spiel gestartet.

## Entscheidung

**HARD STOP für jede Fortnite-Live-Anbindung und einen aktivierten Fortnite-Overlay-Modus.** Der ausgelieferte Funktionsumfang ist ein eigenständiger Offline-Planer mit selbst erzeugten neutralen Daten und einem gewöhnlichen Testfenster. „Nur zwei Freunde“ begründet keine Ausnahme von Bedingungen oder Zulassungsverfahren. Keine Bannfreiheit wird zugesagt; ein Bannrisiko lässt sich nicht auf null garantieren. Mit Fortnite erst nach erneuter dokumentierter Prüfung und belastbarer Klärung des konkreten Anwendungsfalls verwenden.

## Offizielle Quellen und Fundstellen

| Quelle | Gelesene Fundstelle und Befund |
|---|---|
| [Epic Games Terms of Service](https://legal.epicgames.com/epicgames/tos) | Seite nennt „Last Updated“ und „Effective: September 10th, 2026“. §3 „License Terms and Use of Licensed Products“ schränkt Nutzung/Kopien/abgeleitete Inhalte ein; keine impliziten Rechte. §4 „Gameplay Integrity“ verbietet Cheats, unerlaubte Manipulation, Umgehung und Automatisierung. Die Definition nennt unerlaubte Add-ons, die unfaire Vorteile verschaffen. Kein pauschales Okay für diese Karten-/Live-App. |
| [Epic Support: Drittanbieter-Software](https://www.epicgames.com/help/c-34254770/c-40491939/a15028164?lang=de) | „Der Support von Epic Games kann weder ermitteln noch bekanntgeben, ob die Verwendung bestimmter Drittanbieter-Software zu einer Sperrung in Fortnite führen kann.“ Weiter: keine offizielle Unterstützung, Nutzung auf eigenes Risiko. Daraus folgt keine individuelle Freigabe. |
| [Overwolf Fortnite GEP](https://dev.overwolf.com/ow-native/live-game-data-gep/supported-games/fortnite/) | „Available Features“, Tabellen `match`, `location`, `phase`, `items`, `map`, `match_info`; Details unten. Dokumentation ist keine Feststellung aktueller Laufzeitverfügbarkeit oder Freigabe jedes UI-Anwendungsfalls. |
| [Overwolf GEP Intro](https://dev.overwolf.com/ow-native/live-game-data-gep/live-game-data-gep-intro/) | „Set Required Features“ nennt `overwolf.games.events.setRequiredFeatures()`, Listener, Info-Updates und temporäre Ausfälle. „Event Status Endpoints“ erläutert Statusabfragen und Abschaltungen, u. a. auf Wunsch des Studios. Keine frei nutzbare, host-unabhängige Fortnite-REST-Schnittstelle. |
| [Overwolf Native Einstieg](https://dev.overwolf.com/ow-native/getting-started/overview/) | Native-Framework mit eigenen SDK-/API- und Appstore-Prozessen. Aktueller Seitenbanner: „From November 1, 2026, Overwolf only approves apps with Overwolf monetization.“ Ein kostenloses werbefreies Privatprojekt ist daher nicht einfach als genehmigt anzunehmen. Keine Registrierung/Submission durchgeführt. |
| [Overwolf Electron Einstieg](https://dev.overwolf.com/ow-electron/getting-started/overview/) | Beschreibt eine eigenständig gebrandete Electron-Host-Alternative, weiterhin Overwolf-SDK. Daraus ergibt sich keine API für die gewählte WPF-App. Auch dort obiger Monetarisierungsbanner. |
| [Overwolf Developer Terms](https://legal.overwolf.com/docs/overwolf/developers/developer-terms) | §§2.2 Lizenz ist begrenzt, widerruflich und an Plattformbedingungen gebunden; §2.4 Annahme/Verteilung/API-Zugang nach Overwolf-Ermessen; §§3.1 Informationen/Permissions und 3.5 Grenzen der Verteilung über bereitgestellte Plattformfunktionalität. Keine dokumentierte pauschale private Zweipersonen-Ausnahme ermittelt. Die zuerst versuchte alte URL `https://www.overwolf.com/legal/developers-terms/` lieferte 404; aktuelle Adresse aus dem offiziellen Doku-Link entnommen. |
| [Tracker.gg Fortnite Desktop-App](https://tracker.gg/fortnite/desktop-app/) | Nur Produktvergleich: ein anderes angebotenes Desktop-Produkt ist keine übertragbare Lizenz, Zulassung oder Banngarantie für unsere App. Kein Code und keine Karten/Daten übernommen. |

## Datenmatrix: dokumentierte Felder und Umsetzung

„Unklar“ bedeutet: technisch in offizieller Doku beschrieben, **für diese App nicht zugelassen oder zur Laufzeit verifiziert**. Alle solchen Zeilen sind in der App ausgeschlossen. Im Offline-Modus existiert kein Verbindungsversuch.

| Funktion/Daten | Offizieller Dokumentationsstand am Prüfdatum | Bewertung / Entscheidung |
|---|---|---|
| Eigene Routen, neutrale Vektorkarte, generierte Spawnpunkte | Eigene Daten, keine Fortnite-Georeferenzierung | Im Offline-MVP umgesetzt; als DEMO beschriftet. Keine Aussage über reale Fortnite-Spawns. |
| Normales verschiebbares Kompaktfenster | Betriebssystem-Fensterverwaltung | Als neutrales Testfenster umgesetzt, Topmost nur optional. Zulässigkeit während Fortnite ungeklärt; keine aktivierte Fortnite-Funktion. |
| Matchbeginn/-ende | Feature `match`, Events `matchStart` / `matchEnd` | Unklar für private WPF-App; keine Integration. Nur eigene `Started` / `Finished`-Demo-Ereignisse. |
| Eigene Position | Feature `location`, Key `location`, Kategorie `game_info`, `(x,y,z)`, laut Doku oben links `(0,0,z)`, unten rechts `(3000,3000,z)` | Dokumentiert, aber Host-/Freigabe-/Laufzeitfragen offen. Nicht implementiert. Demo-Koordinaten sind ein eigenes planares Testsystem, keine belegte aktuelle Fortnite-Transformation. |
| Buslinie | `location` / `bus_line`, Doku nennt deaktiviert im Tournament-Modus | Kein Adapter; keine Nutzung. Einschränkung zeigt, dass Modi gesondert geprüft werden müssen. |
| Kartenkennung | `map` / Key `map`, Kategorie `match_info`; `creative_map` mit `map_id`, `title`, `account_id`, Versionen; Doku nennt Creative-2.0-Feldausfälle | Dokumentiert, keine garantierte BR-Seasonkennung, keine Datenverfügbarkeit geprüft. Nicht implementiert. |
| Spielphase | `phase` / `game_info.phase`, u. a. `lobby`, `loading_screen`, `airfield`, `aircraft`, `freefly`, `landed`, `replay` | Nicht implementiert. Keine erfundenen Phasenfelder. |
| Eliminierungen / eigene Ereignisse | Features u. a. `kill`, `killed`, `killer`, `death`, `assist`, `revived` | Dokumentiert, aber hier keine Berechtigung/Verfügbarkeit nachgewiesen; nicht implementiert. |
| Items | Feature `items`, u. a. `inventory.item_X`, `selected_slot`, `quickbar_X`, `selected_material`; Item-Beispiele mit `name`, `count`, `ammo`, `rarity` | Betrifft lokales Inventar, keine verborgenen Weltobjekte. Nicht implementiert. |
| Nach-Match-Statistik | `match_info.match_stats`: „Statistics when the match is over“ | Dokumentiert, aber keine Echtwerte abgerufen. Lokal nur eindeutig benannte Demo-Zusammenfassungen. |
| Tatsächlich gespawnte, verborgene Truhen der Partie | In den geprüften offiziellen Quellen keine freigegebene Schnittstelle dafür gefunden | **Nicht verfügbar für dieses Projekt. Ausgeschlossen.** Keine Ersatztechnik, kein Ableiten aus Inventar oder sonstigen Daten. |
| Manuelle Bestätigung | Eigene bewusste Aktion des Benutzers | Nur Demo-Sichtung in dieser Sitzung; keine automatische oder gespeicherte Behauptung über eine reale Partie. |
| Screenshot/OCR | Keine konkrete Zulässigkeitsfreigabe ermittelt | Ausschließlich Machbarkeitsnotiz: sichtbare Bildschirmelemente wären technisch separat prüfbar; kein erster Release, kein API-Workaround, keine Implementierung. |
| Speicherzugriff, Injection, Hooks, Treiber, Netzwerkabgriff, Spielautomation | Widerspricht Projektauftrag und Gameplay-Integrity-Ziel | Vollständig ausgeschlossen. Keine entsprechenden Abhängigkeiten oder Rechte. |

## Host, Registrierung, Lizenz und Verteilung

Diese App ist C#/.NET/WPF. Weder Overwolf Native noch Overwolf Electron ist installiert, eingebunden oder Bestandteil des Artefakts. Die dokumentierten GEP-APIs dürfen nicht als allgemeine Windows-API missverstanden werden. Eine spätere Integration erfordert einen gesonderten Host-/Lizenzentwurf, aktuelle Anbieterbedingungen, Registrierung und Bestätigung des erforderlichen Review-/Verteilungswegs. Umfang und mögliche Ausnahmen für private Tests wurden nicht verbindlich geklärt. Der Monetarisierungsbanner ist ein zusätzlicher offener Konflikt mit dem ausdrücklich werbefreien Projekt. Keine Bedingungen wurden im Namen des Nutzers akzeptiert.

## Karten-/Spawnrechte und Aktualisierung

Es wird kein Fortnite-Kartenbild heruntergeladen. Die Vektorkarte in `MapView.cs` und die deterministische Punktformel in `DemoData` sind eigene Projektbeispiele. Für diese Dateien gilt die beiliegende Projektlizenz; eine Attribution eines externen Kartenanbieters ist nicht erforderlich. Datenkennung `DEMO-NEUTRAL-2026-01` bezeichnet **keine Fortnite-Season**. Import unterstützt eigene Routen, keinen stillen Drittanbieter-Datensatz. Alte/fremde Kennungen werden gewarnt und nicht auf die aktive Karte projiziert.

Vor neuen echten Daten: Rechteinhaber, Wiederverwendungs-/Weiterverteilungsrecht, Attribution, Season/Georeferenzierung, Versionskennung und Updateverfahren einzeln belegen. Eine öffentlich abrufbare API oder Community-Karte reicht als Lizenznachweis nicht. Aktuell gibt es keinen automatischen Season-/Map-Updater.

## Vor realem Fortnite-Einsatz offen

Konkrete Karten-/Kompakt-/Live-Funktionen und Spielmodi müssen belastbar geklärt werden; für GEP zusätzlich der gewählte Host, private Verteilung, Lizenz und Anbieterzulassung. Reale Karten-/Spawndaten benötigen belegte Rechte und eine passende Transformation. Änderungen an Regeln und API-Verfügbarkeit erneut prüfen. Ohne diese Nachweise bleibt der Offline-/Neutraltest die Grenze dieser Version.
