# Monochromes Desktop-Design

Recherche vom 2026-11-07. Primäre Referenzen:

- [Linear: A calmer interface for a product in motion](https://linear.app/now/behind-the-latest-design-refresh): ruhige Hierarchie, kompakte Aktionen, zurückhaltende Trennung von Arbeitsbereichen.
- [Linear: Welcome to the new Linear](https://linear.app/changelog/2024-03-20-new-linear-ui): klarere Seitenleiste und konsistente Dichte.
- [Vercel Geist: Colors](https://vercel.com/geist/colors): abgestufte Flächen, Rahmen und Textkontraste.

Die Referenzen dienen als Inspiration; kein Logo, Asset oder Quellcode wurde übernommen. Keine zusätzlichen Fonts oder Designbibliotheken.

## Umsetzung

Nur neutrale Grautöne mit identischen R/G/B-Werten. Hintergrund #080808, Arbeitsfläche #0A0A0A, Karten #101010, Eingaben #151515. Rahmen #242424 bis #505050; Text #777777 bis #EEEEEE. Weiß dient ausschließlich kleinen Akzenten und gut lesbarer Schrift, nicht als Eingabefläche oder Button-Hintergrund. Keine lila, blauen oder anderen farbigen Akzente. Eigene Kartenbilder werden in Graustufen mit reduzierter Deckkraft auf Schwarz gezeichnet.

Drei Bereiche: Bibliothek und Radius links, große Arbeitsfläche in der Mitte, Wegpunkte und Orte rechts. Selbst gestaltete dunkle Templates für Buttons, Textfelder, Dropdown inklusive Popup, Listen, Slider, Checkboxen und Scrollleisten. Dunkle eigene Titelleiste über normalem WPF WindowChrome; keine Hooks oder fremden Fensterzugriffe. Eigene Bestätigungsdialoge folgen demselben Farbschema. System-Dateiauswahldialoge folgen der Windows-Einstellung.

Keine Anmeldung, Profilattrappe, leeren Fake-Statistiken oder künstlichen Live-Indikatoren. Leere Zustände erklären den nächsten sinnvollen Schritt. Erst nach eigenen Eingaben entstehen Routen, Orte und Verlauf. Der Footer nennt den lokalen Betrieb und die fehlende Fortnite-Verbindung ohne große Warnfläche im Arbeitsbereich.

Die Windows-UI-Prüfung rendert tatsächliche WPF-Fenster: leerer Planer, ausgefüllte eigene Route auf der freien Arbeitsfläche, Kompaktfenster und echter manueller Verlauf. Diese Screenshots sind keine generierten Entwürfe. Testeingaben bleiben in temporären Verzeichnissen und werden nicht als Nutzerdaten ausgeliefert.
