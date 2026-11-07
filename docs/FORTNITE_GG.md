# Fortnite.GG als Original-Webseite – 2026-11-07

Quelle: https://fortnite.gg/. Die tatsächliche farbige Battle-Royale-Karte wurde im Browser aufgerufen, der Bereich Spawns aufgeklappt und die Kategorien Chests, Buried Chests und Rare Chests geprüft. Rare Chests ist der Originalname für die seltenen Kisten; die App erfindet keine zusätzliche Kategorie oder Seltenheit. Das Stiftwerkzeug der Originalseite dient zum Zeichnen und Markieren.

## Entscheidung

https://fortnite.gg/legal/terms, Abschnitte 1 und 3, behalten nicht ausdrücklich gewährte Rechte vor und ordnen Inhalte/Software dem Anbieter oder seinen Lizenzgebern zu. Keine belegte Lizenz für das Kopieren des kompletten Truhendatensatzes oder des Website-Codes gefunden. Deshalb kein Scraper, kein lokales Datenpaket und keine undokumentierte API. Die positive Entscheidung betrifft ausschließlich die normale Nutzung der unveränderten öffentlichen Website in einem Browserfenster. Sie stellt keine Lizenz für die Weiterverteilung der dortigen Daten dar.

Die App enthält eine eigene, erst nach Klick geöffnete Browseransicht mit Microsoft Edge WebView2. Die gesamte Originalseite bleibt sichtbar und wird vom Anbieter geliefert; Branding, Werbung, Quellen, Filter und Zeichenwerkzeuge werden nicht entfernt oder ersetzt. Kein automatischer Export oder Import von Website-Routen in die lokale Bibliothek. Kein Zugriff auf eine laufende Partie. Vollständigkeit und Aktualität der statischen Spawn-Karte werden nicht zugesichert.

Datenschutz: https://fortnite.gg/legal/privacy, insbesondere Cookies/Logdaten/werbliche Dienste. Die Website ist ein externer Dienst mit eigenen Bedingungen, Cookies und möglicher Werbung/Analyse. Sie lädt erst beim bewussten Öffnen; keine Website-Anmeldung durch die App. Das separate Profil unter `%LOCALAPPDATA%\Routenatelier\web-fortnite-gg` bleibt zwischen Sitzungen erhalten. „Webdaten löschen“ entfernt dessen Browserdaten, einschließlich lokal gespeicherter Zeichnungsdaten; die native `library.json` bleibt erhalten. Das normale Browserprofil des Nutzers wird nicht eingebunden. Daten, die der Nutzer bewusst in die Website eingibt oder über deren Speicher-/Teilenfunktion sendet, unterliegen dem Dienst, nicht dem lokalen Routenmodell.

## Technische Grenze und Laufzeit

Nur HTTPS-Navigation zum exakten Host fortnite.gg im Hauptfenster; keine lokalen Dateiziele, abweichenden Ports oder Benutzerdaten in URLs. Normale Unterressourcen der Website werden durch den Browser geladen. Keine JavaScript-Injektion im Produkt, keine Hostobjekte, keine WebMessage-Brücke, keine Zertifikatsausnahmen und kein Zugriff auf lokale Routen. Berechtigungsanfragen und Downloads werden in dieser eingeschränkten Ansicht abgelehnt. Für zusätzliche externe Links/Downloads steht „Im Browser“ bereit; der Launcher akzeptiert ausschließlich zwei feste HTTPS-Ziele (Fortnite.GG und Microsofts WebView2-Seite).

SDK Microsoft.Web.WebView2 1.0.4314.35, offizielle NuGet-Quelle und beigefügte BSD-Lizenz `third-party/WebView2-LICENSE.txt`. Microsoft-Dokumentation: https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security und https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution.

Die automatisch aktualisierte Microsoft WebView2 Runtime muss auf Windows vorhanden sein (auf Windows 11 vorinstalliert, auf den meisten Windows-10-Systemen vorhanden). Sie wird nicht als eingefrorene Browser-Runtime gebündelt. Fehlt sie, zeigt die App einen konkreten Hinweis sowie Links zum normalen Browser und zur offiziellen Microsoft-Installation. Kein automatischer System-Download/Installer. Die lokale Planung benötigt diese Runtime nicht. Der ZIP-Build enthält die App, .NET und den WebView2-SDK-Loader; die Runtime-Voraussetzung bleibt ausdrücklich bestehen.

## Farbige lokale Karte

Neu geladene/importierte Karten behalten Originalfarben und volle Deckkraft. Die native Oberfläche bleibt schwarz/grau. Alte Graustufen-Caches werden nicht manipuliert: Sie besitzen eine andere Inhaltskennung. Für eine farbige neue Karte „Fortnite-Karte laden“ verwenden; alte Routen bleiben mit ihrer bisherigen Karte gespeichert. Kein automatisches Umprojizieren oder Löschen alter Routen.
