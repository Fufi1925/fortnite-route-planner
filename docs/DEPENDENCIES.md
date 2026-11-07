# Abhängigkeiten und Lizenzen

Projektcode und leere Datenvorlagen: `LICENSE` (MIT). Keine übernommenen Epic-/Tracker-/Overwolf-Assets oder SDKs.

- .NET SDK **10.0.403**, im Build verwendet, über `global.json` festgelegt (neuere Patches desselben Featurebands erlaubt).
- Zielframeworks `net10.0` und `net10.0-windows`; WPF mit WindowsDesktop Framework. Das Publish-Skript fixiert Runtime **10.0.14** für den aufgezeichneten Build. Spätere Sicherheitsupdates bewusst aktualisieren, bauen und prüfen.
- Microsoft .NET Runtime / WindowsDesktop / SDK: Open Source, überwiegend MIT mit Komponenten unter weiteren Lizenzen; siehe mitgelieferte `third-party/`-Lizenz- und Notice-Dateien. WPF: [dotnet/wpf](https://github.com/dotnet/wpf); Runtime: [dotnet/runtime](https://github.com/dotnet/runtime). Lizenzangaben dieser Projekte gelten für die betreffenden Bestandteile.
- Keine expliziten NuGet-`PackageReference`s, keine Drittanbieter-UI-, Telemetrie-, Hook-, Capture- oder Netzwerkbibliothek. Restore lädt Microsoft-Framework-/Runtime-Packs für den Cross-Build. Keine Overwolf-Lizenz als Bestandteil der Anwendung. Öffentliche Kartenbilder sind Epic-IP außerhalb der MIT-Lizenz; siehe third-party/EPIC_FAN_CONTENT_NOTICE.md. Keine Rohdaten oder Kartenbilder werden mit dem Build gebündelt.
- CI-Werkzeuge: GitHub `checkout@v4`, `setup-dotnet@v4`, `upload-artifact@v4`; sie laufen in CI und werden nicht in die App eingebaut.

Der selbstenthaltende Build liefert die .NET-Runtime mit aus. Lizenz-/Notice-Dateien bleiben dem ZIP beigelegt. Der normale Desktop-Betrieb nutzt Windows/WPF-Systemschnittstellen; es wird keine Spielebibliothek integriert.
