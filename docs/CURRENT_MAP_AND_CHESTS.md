# Kartenabruf und Truhendaten – Entscheidung 2026-11-07

Ergänzung 0.4: [Fortnite.GG als unveränderte Original-Webseite](FORTNITE_GG.md) bietet die gewünschten Kistenfilter und Zeichenwerkzeuge in einer getrennten WebView2-Ansicht. Das ändert nichts an der folgenden Datensatzentscheidung für den nativen Planer: keine Kopie oder Synchronisierung dieser Website-Daten und keine Live-Partie.

## Öffentliche Kartenabbildung

Dokumentation: https://dash.fortnite-api.com/endpoints/map
API: https://fortnite-api.com/v1/map?language=de
Bildziel der tatsächlich geprüften Antwort: https://fortnite-api.com/images/map.png

Am 2026-11-07 tatsächlich abgerufen: HTTP 200, JSON mit Bildern und benannten POIs; Bild HTTP 200, PNG 2048×2048, 2.379.325 Bytes, Last-Modified 2026-11-02 05:37:38 UTC. Die POI-Namen enthalten Arlong Park und Windmill Village. Die offizielle Saisonankündigung https://www.fortnite.com/news/fortnite-treasure-hunters-the-greatest-adventure-of-all-time?lang=en-US nennt diese Orte für die seit 1. November verfügbare Crossbones Isle. Der Anbieter liefert keinen belastbaren Season-/Build-Zeitstempel im Karten-JSON; Abrufdatum und Bilddatum werden deshalb getrennt angezeigt, ohne Garantie, dass der Anbieter jederzeit sofort auf neue Patches reagiert.

Positive Projektentscheidung ausschließlich für den öffentlichen statischen Bildabruf in der kostenlosen nichtkommerziellen eigenständigen App. Grundlage für Epic-Bildmaterial ist die Fan Content Policy https://legal.epicgames.com/epicgames/fan-art-policy, insbesondere Definition öffentlich zugänglicher nichtkommerzieller Fan-Apps, §§1.1–1.4 und der verpflichtende Hinweis in §1.10. Dieser Hinweis wird bei der bekannten öffentlichen Kartenquelle sichtbar angezeigt. Keine offizielle Zugehörigkeit oder Epic-Zulassung behauptet. Das geprüfte blanke Kartenbild enthält kein zu verfremdendes Epic-Logo. Download-Bildmaterial gehört nicht zur MIT-Lizenz des Programmcodes.

Die Anwendung lädt ausschließlich das dokumentierte JSON und das fest erlaubte Bildziel. Keine Spielprozess-, Konto-, Speicher- oder Match-Anbindung. Keine Bündelung des Kartenbilds oder einer Rohdatenkopie in Quellcode/EXE. Lokal notwendiger Bildcache, begrenzte Größen, Zeitlimit, keine automatischen Wiederholungen bei Sperren und keine Weiterleitungen auf fremde Hosts. Ein vorhandener lokaler Kartenstand bleibt bei Fehlern erhalten. Die Erstinstallation lädt das Bild nur bei leerem eigenständigem Planer; bestehende eigene Routen werden nicht automatisch umprojiziert.

Diese Entscheidung erlaubt kein aktiviertes Fortnite-Live-Overlay und keine Beobachtung einer Partie. Die Fan Content Policy ersetzt die Gameplay-Integrity-Regeln nicht und ist widerruflich. Mit Fortnite zusammen weiterhin keine pauschale Nutzungsfreigabe oder Banngarantie.

## Truhen

Fortnite-API dokumentiert für /v1/map Kartenbild und POIs, keine Truhenkoordinaten. Benannte Orte werden nicht zu Truhen umgedeutet.

https://www.api-fortnite.com/fortnite-map-objects-api dokumentiert echte statische Objektkoordinaten und anhand vergangener Turnier-Replays gemessene Spawn-Raten. Erforderlich: API-Schlüssel und Custom-Plan ab 30 EUR/Monat. https://www.api-fortnite.com/terms (Stand 2026-11-02), §4 nennt mindestens drei Monate mit Vorauszahlung und automatischer Verlängerung; §§6/7 begrenzen Weiterverteilung und verlangen für eigene Materialien vorherige schriftliche Zustimmung. Kein Konto, Schlüssel, Plan oder eine passende Freigabe für dieses öffentliche Projekt vorhanden. Kein Kauf und keine Integration vorgenommen.

https://fn-api.cc/tos §§5/9 gewähren Zugriff auf den Dienst, aber keine Rechte am zugrunde liegenden Spielmaterial; §6 verbietet u. a. missbräuchliche Weiterverteilung und Cheat-Produkte. Die geprüfte API-Referenz https://fn-api.cc/docs bietet keinen belegten vollständigen Truhen-Koordinaten-Datensatz dieses Anwendungsfalls. Keine Rohdatei-/Schlüsselextraktion als Ersatz durchgeführt.

Ergebnis: Kein legal und technisch verifizierter vollständiger Truhendatensatz für diese App verfügbar. Kein Nachweis tatsächlich gespawnter Truhen einer laufenden Partie. Diese beiden Grenzen bleiben sichtbar. Die Bereichsauswahl filtert ausschließlich tatsächlich vorhandene eigene beziehungsweise berechtigt importierte Punkte. Keine erfundenen Punkte, keine Vollständigkeits- oder Live-Garantie.
