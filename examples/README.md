# Eigene Daten vorbereiten

Es werden keine Test-Truhen oder vorgefertigten Inseln ausgeliefert. Die beiden JSON-Dateien sind **leere Vorlagen**: Eine Route für die freie Arbeitsfläche sowie ein leerer Ortsdatensatz. Eigene Koordinaten, Namen und Quellen eintragen; Werte liegen zwischen 0 und 3000 je Achse. Der Import verlangt dieselbe Kartenkennung wie die geöffnete Karte.

Für bildgebundene Daten die MAP-…-Kennung aus einer exportierten eigenen Route verwenden. Originale oder berechtigt nutzbare quadratische PNG-/JPEG-Karten über „Karte öffnen“ laden. Keine fremden Dateien oder Datensätze ohne Nutzungsrecht übernehmen. Statische Orte sind kein Nachweis tatsächlich gespawnter Truhen.

Schema 5: `visibleKinds` enthält beliebige eindeutige Kombinationen aus `Other`, `Chest`, `BuriedChest`, `RareChest`; `[]` blendet alle Markierungen aus. Jeder importierte Punkt benötigt `kind` mit einem dieser Werte. Alte Dateien werden übernommen, untypisierte alte Punkte bleiben `Other`. Typen sind Herkunftsangaben des Datensatzes, kein Nachweis einer Öffnung oder tatsächlichen Existenz in einer Partie.
