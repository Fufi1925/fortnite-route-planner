# Live-Anforderung aus Sprachauftrag – Prüfung 2026-11-07

Gewünscht: Fortnite.GG-Editor vollständig in die EXE übernehmen; drei unabhängig schaltbare Kistentypen; beim Routenstart Fortnite öffnen; dem Charakter folgen; einzelne Truhenöffnungen erkennen; oben rechts einen automatischen optimalen Leave-Zeitpunkt für möglichst viele Truhen je Zeit anzeigen.

## Ergebnis

Der ursprüngliche Projektauftrag verlangt vor aktiven Live-/Overlay-Funktionen einen positiven Zulässigkeits- und Schnittstellennachweis (HARD STOP). Dieser Nachweis liegt für die gewünschte Automatik nicht vor. Keine Live-Anbindung aktiviert und kein tatsächlicher Spielstart oder echter Fortnite-Test durchgeführt und keine Bildschirm-/Audio-/Speicher-/Netzwerkersatztechnik eingebaut. Dass nur bereits geöffnete Truhen erkannt werden sollen, erteilt noch keine Zugriffs- oder Overlayfreigabe.

| Teil | Aktuell belegte Grundlage | Entscheidung |
|---|---|---|
| Drei Kistenfilter und eigene Route im lokalen Editor | Eigener Code, eigene/lizenzierte importierte Punktdaten | In 0.5 implementiert; kein kopierter Fortnite.GG-Code/-Datensatz. |
| Original-Fortnite.GG-Werkzeuge | Normale Nutzung der unveränderten Website | Weiterhin getrennte Webansicht; keine Route-/Daten-Synchronisierung. |
| Spielerposition / Charakter verfolgen | Overwolf dokumentiert `location`/`game_info.location`, normierte Koordinaten 0…3000 | Nur im entsprechenden Overwolf-Host; für diese WPF-App fehlen Host-/Lizenz-/Appfreigabe. Nicht aktiviert. |
| Einzelne Truhenöffnungen | Aktuelle Fortnite-GEP-Referenz vollständig nach `chest` geprüft; kein dokumentiertes passendes Ereignis gefunden | Nicht verfügbar aus der geprüften Quelle. Keine Felder erfinden und keine Inventar-/XP-Änderungen zu Truhenöffnungen umdeuten. |
| Kreis/Bereich tatsächlich leer | Weder bestätigte Partiedaten noch vollständiger bestätigter Restbestand vorhanden | Unbekannt. 0 angezeigte Markierungen, abgeschlossene Wegpunkte oder ausgeschaltete Filter sind kein Leer-Nachweis. |
| Optimaler Leave-Zeitpunkt | Benötigt zumindest verlässliche Öffnungs-/Restdaten und ein begründetes Modell für nächsten Durchlauf einschließlich Warte-/Ladezeit | Nicht berechenbar mit den verfügbaren Daten; kein automatischer Hinweis und kein Verlassen/Makro. Selbst bestätigte Truhenöffnungen allein beweisen keinen leeren Bereich oder mathematisch optimalen Wechsel. |
| Normaler Spielstart über Epic | Epics Dokument „Epic Games Protocol Activation“, Abschnitt Launch Game, enthält den festen Fortnite-Aufruf | Optionaler Startauftrag über die normale Windows-Protokollaktivierung implementiert. Kein Nachweis, dass das Spiel bereits läuft; keine Match-/Overlay-Anbindung. |
| Fenster oben rechts | Bestehendes gewöhnliches WPF-Fenster mit optionalem Windows-Topmost | Keine ins Spiel injizierte Anzeige und keine nachgewiesene Fortnite-In-Game-Zulassung. In 0.5 ohne Lootpositionen. |

## Quellen und genaue Fundstellen

- https://dev.overwolf.com/ow-native/live-game-data-gep/supported-games/fortnite/: Abschnitt `location` liefert Positionsschema, `counters` nennt Ping, `items` Inventarinhalte. Die geprüfte Referenz enthält kein `chest`-Ereignis. `match_info.match_stats` ist für Statistik nach Matchende beschrieben und kein Ersatz für einzelne Öffnungsereignisse während der Partie.
- https://dev.overwolf.com/ow-native/getting-started/overview/: Overwolf Native ist ein eigener Host auf der Overwolf Desktop App. Der aktuelle Dokumentationshinweis nennt zusätzliche Monetarisierungsvoraussetzungen für App-Zulassungen seit 2026-11-01.
- https://dev.overwolf.com/ow-native/getting-started/project-roadmap/: App-Idee, Einreichung und Zulassung sind separate Plattformschritte. Keine Einreichung oder Registrierung im Namen des Nutzers vorgenommen.
- https://legal.overwolf.com/docs/overwolf/developers/developer-terms/: Plattform-/API-Lizenz und Dokumentation, kein allgemeiner frei verwendbarer WPF-REST-Zugang. Keine Anbieterbedingungen oder Monetarisierungsvereinbarung im Namen des Nutzers akzeptiert.
- https://legal.epicgames.com/epicgames/tos, §4 Gameplay Integrity: keine pauschale Freigabe für die angefragte Drittanbieter-Automatik. Die Prüfung behauptet auch kein generelles Verbot jeder lokalen statischen Kartenplanung.
- https://fortnite.gg/legal/terms, §§1/3: Rechtevorbehalt, Inhalte/Software beim Anbieter oder seinen Lizenzgebern. Kein belegtes Recht zur Übernahme des gesamten Website-Codes oder Truhendatensatzes. Normale unveränderte Webansicht bleibt gemäß FORTNITE_GG.md getrennt.

## Konkreter offener Bedarf

Für weitere Live-Arbeit werden dokumentierte Nutzungsrechte für genau diese App und ihren Host sowie dokumentierte aktuelle Spielerpositions- und Truhenöffnungsereignisse benötigt. Eine reine Positionsfreigabe reicht für die Truhenerkennung nicht. Anschließend müssen Datenalter, Ausfälle, Kartenkoordinaten, Ereignisduplikate und Matchwechsel geprüft werden; ein Optimierungsmodell wäre als begründete Schätzung mit sichtbaren Grenzen zu kennzeichnen. Kein generischer, ungeprüfter Live-Adapter wird vorab aktiviert.

## Tatsächlich implementierter Teil

Schema 5 speichert vier Typen: Other (Sonstige/untypisierte Orte), Chest, BuriedChest und RareChest. Die drei Kistentypen lassen sich unabhängig ein-/ausblenden; Other besitzt einen eigenen Schalter, damit alte generische Orte nicht als Truhen erfunden werden. Leere Auswahl ist zulässig. Auswahl, Typen, Route und Bereich bleiben bei Laden, Export/Import und Duplikation erhalten. Alle-Orte-Modus hebt die Typenauswahl nicht auf. Kein Filter verändert gespeicherte Punkte oder bestätigt eine Truhenöffnung.

Der lokale Routenmodus setzt Wegpunkte per Klick und bearbeitet sie per Ziehen/Reihenfolge. Der Marker-Typ wird bei eigenen neuen Markierungen ausdrücklich gewählt und beim Import erhalten. Das Kompaktfenster zeigt ausschließlich die Route, keine Truhen-/Lootpositionen und keine automatische Charakterposition. Es nennt Truhenöffnungen als nicht verbunden und den Leave-Zeitpunkt als unbekannt.

## Separat erlaubter normaler Launcher-Aufruf

Primärquelle: [Epic Games Protocol Activation](https://assets-unreal2-epic-prod-us2.s3.dualstack.us-east-1.amazonaws.com/original/4X/f/c/4/fc4342c50e1a0abf34943e019ee90bacccc62950.pdf), Seiten 3/4, über die Epic-Entwicklerantwort [Deep Linking](https://forums.unrealengine.com/t/link-to-the-main-game-page-from-a-demo/1172275) bereitgestellt. Das Dokument beschreibt einen normalen Startauftrag via OS und den exakten Fortnite-Katalogpfad. Positive Projektentscheidung ausschließlich für diesen vom Nutzer angeforderten Launcher-Aufruf; daraus folgt keine Live-/Overlayfreigabe.

Checkbox „Beim Routenstart Fortnite über Epic öffnen“ ist anfangs aus und nicht Bestandteil importierter Routendaten. Nur bei ausgewählter Option und bewusstem Routenstart wird die schreibgeschützt gelesene Windows-Protokollregistrierung geprüft und genau die konstante dokumentierte URI geöffnet. Kein frei importierter Zielpfad, keine Parameter für Spielautomatisierung und keine Prozess-/Dateiinspektion. Fehlt die Registrierung oder schlägt der OS-Aufruf fehl, startet auch kein Routengang; die lokale Option lässt sich ausschalten. Bei erfolgreicher Übergabe lautet der Status ausdrücklich „Epic-Start angefragt“, nicht „Fortnite läuft“. Tatsächlicher Spielstart und vorhandene Installation sind hier nicht getestet. Windows-Tests prüfen nur Aufrufentscheidung und Fehlerverhalten über eine injizierte Testfunktion, ohne ein Spiel zu starten.
