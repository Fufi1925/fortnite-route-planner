using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using RoutePlanner.Core;
using Validation = RoutePlanner.Core.Validation;

namespace RoutePlanner.App;

public partial class MainWindow : Window
{
    private readonly LocalStore store;
    private readonly SpawnIndex spawns = new(DemoData.Spawns);
    private readonly OfflineDemoProvider provider = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private LibraryDocument library = new();
    private RoutePlan current = DemoData.Example();
    private readonly HashSet<string> confirmed = [];
    private bool ready, updating, dirty, storageBlocked, closing;
    private CompactWindow? compact;
    private DemoSession? session;
    private IReadOnlyList<DemoEvent> events = [];
    private int eventIndex;

    public MainWindow() : this(null) { }
    public MainWindow(string? dataDirectory)
    {
        store = new(dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Routenatelier"));
        InitializeComponent();
        try
        {
            library = store.Load();
            current = library.Routes.FirstOrDefault()?.Copy() ?? DemoData.Example();
            dirty = library.Routes.Count == 0;
        }
        catch (Exception ex) when (IsDataError(ex))
        {
            storageBlocked = true; dirty = true;
            MessageBox.Show(this, "Die Bibliothek konnte nicht gelesen werden. Sie wird nicht überschrieben. " +
                "Sichere die Datei und verwende bei Bedarf ‚Lokale Daten löschen‘.\n\n" + store.LibraryPath + "\n" + ex.Message,
                "Beschädigte oder unzugängliche Daten", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        PlannerMap.RouteEdited += () => { dirty = true; Refresh(); };
        PlannerMap.PointSelected += i => { PointsBox.SelectedIndex = i; };
        PlannerMap.SpawnClicked += id =>
        {
            if (!confirmed.Add(id)) confirmed.Remove(id);
            Refresh(); Status("Manuelle Demo-Markierung geändert; keine Erkennung und kein Partienachweis.");
        };
        timer.Tick += DemoTick;
        Loaded += (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Width = Math.Max(MinWidth, Math.Min(Width, area.Width));
            Height = Math.Max(MinHeight, Math.Min(Height, area.Height));
            Left = Math.Max(area.Left, area.Left+(area.Width-Width)/2);
            Top = Math.Max(area.Top, area.Top+(area.Height-Height)/2);
        };
        Closing += OnClosing;
        ready = true; Refresh();
        ProviderStatus.Text = provider.SourceLabel + " · bereit";
    }
    private static bool IsDataError(Exception ex) => ex is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException
        or NotSupportedException or System.Security.SecurityException;
    private bool Run(Action action)
    {
        try { action(); return true; }
        catch (Exception ex) when (IsDataError(ex))
        {
            Status("Fehler: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Aktion nicht ausgeführt", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }
    private void Status(string message) => StatusLabel.Text = message;
    private void Commit(LibraryDocument next)
    {
        if (storageBlocked) throw new InvalidDataException("Bibliothek gesperrt. Erst beschädigte Daten sichern und gezielt löschen.");
        store.Save(next); library = next;
    }
    private bool TrySave()
    {
        bool saved = Run(() =>
        {
            Validation.Route(current);
            var routes = library.Routes.Where(r => r.Id != current.Id).Append(current.Copy()).ToList();
            Commit(library with { Routes = routes }); dirty = false;
        });
        if (saved) { Refresh(); Status("Gespeichert · " + store.LibraryPath); }
        return saved;
    }
    private bool MayLeave()
    {
        if (!dirty) return true;
        var answer = MessageBox.Show(this, "Änderungen an dieser Route speichern?", "Ungespeicherte Route",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
        return answer == MessageBoxResult.No || answer == MessageBoxResult.Yes && TrySave();
    }
    private void Refresh()
    {
        if (!ready) return;
        updating = true;
        int pointSelection = PlannerMap.SelectedPoint;
        var choices = library.Routes.Select(r => r.Id == current.Id ? current : r).ToList();
        if (choices.All(r => r.Id != current.Id)) choices.Add(current);
        RoutesBox.ItemsSource = choices; RoutesBox.SelectedItem = current;
        NameBox.Text = current.Name;
        RadiusSlider.Value = current.Radius; ModeBox.SelectedIndex = (int)current.RadiusMode;
        RadiusLabel.Text = UnitsBox.SelectedIndex == 1
            ? $"{current.Radius / 30:0.00} % Kartenbreite ({current.Radius:0.##} KE)"
            : $"{current.Radius:0.##} KE · 0…1500";
        PointsBox.ItemsSource = current.Points.Select((p, i) =>
            $"{i+1:00}  {(i == 0 ? "Start" : i == current.Points.Count-1 ? "Ziel" : "Wegpunkt")}   {p.X:0.#} / {p.Y:0.#}").ToArray();
        PointsBox.SelectedIndex = Math.Clamp(pointSelection, -1, current.Points.Count-1);
        PlannerMap.Route = current; PlannerMap.Confirmed = confirmed;
        PlannerMap.VisibleSpawns = spawns.Query(current);
        PlannerMap.Editable = session is null;
        PlannerMap.InvalidateVisual();
        bool currentMap = DemoData.IsCurrent(current);
        MapVersionLabel.Text = currentMap ? DemoData.MapVersion + " · eigene Testdaten"
            : "VERALTET / FREMDE KARTE: " + current.MapVersion;
        RouteMetrics.Text = $"{current.Points.Count} Punkte · {Geometry.Length(current.Points):0} KE · " +
            $"{PlannerMap.VisibleSpawns.Count} mögliche Demo-Spawns · {confirmed.Count} manuell markiert";
        HistoryBox.ItemsSource = library.DemoHistory.AsEnumerable().Reverse().Select(h =>
            $"DEMO · {h.StartedAt.ToLocalTime():dd.MM. HH:mm} · {h.RouteName} · {h.DurationSeconds}s simuliert · " +
            $"{h.Distance:0} KE · {h.SimulatedStops} Stopps").ToArray();
        Title = $"{(dirty ? "* " : "")}{current.Name} · Routenatelier · Offline-Demo";
        updating = false; UpdatePointFields();
        compact?.UpdateRoute(current, PlannerMap.VisibleSpawns, confirmed, PlannerMap.DemoPosition);
    }
    private void SwitchTo(RoutePlan route, bool unsaved)
    {
        StopPlayback(); current = route; dirty = unsaved; confirmed.Clear();
        PlannerMap.SelectedPoint = -1; PlannerMap.Fit(); Refresh();
        if (!DemoData.IsCurrent(current)) Status("Datenversion unbekannt oder veraltet. Keine Spawns, Bearbeitung oder Demo-Positionen auf dieser Karte.");
    }
    private void SelectRoute(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || updating || RoutesBox.SelectedItem is not RoutePlan route || route.Id == current.Id) return;
        if (MayLeave()) SwitchTo(route.Copy(), false); else Refresh();
    }
    private void NewRoute(object sender, RoutedEventArgs e)
    { if (MayLeave()) SwitchTo(new(), true); }
    private void DuplicateRoute(object sender, RoutedEventArgs e)
    { if (MayLeave()) SwitchTo(current.Copy(newIdentity: true), true); }
    private void DeleteRoute(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, $"Route ‚{current.Name}‘ endgültig löschen?", "Route löschen",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        if (Run(() => Commit(library with { Routes = library.Routes.Where(r => r.Id != current.Id).ToList() })))
            SwitchTo(library.Routes.FirstOrDefault()?.Copy() ?? new(), library.Routes.Count == 0);
    }
    private void SaveRoute(object sender, RoutedEventArgs e) => TrySave();
    private void ReloadRoute(object sender, RoutedEventArgs e)
    {
        if (!MayLeave()) return;
        Run(() =>
        {
            var loaded = store.Load(); library = loaded; storageBlocked = false;
            SwitchTo(loaded.Routes.FirstOrDefault(r => r.Id == current.Id)?.Copy()
                ?? loaded.Routes.FirstOrDefault()?.Copy() ?? new(), loaded.Routes.Count == 0);
            Status("Bibliothek vom Datenträger geladen.");
        });
    }
    private void RenameRoute(object sender, TextChangedEventArgs e)
    {
        if (!ready || updating) return;
        current.Name = NameBox.Text; dirty = true; Title = "* " + current.Name + " · Routenatelier · Offline-Demo";
    }
    private void ChangeRadius(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!ready || updating) return;
        current.Radius = Math.Round(RadiusSlider.Value, 2); dirty = true; Refresh();
    }
    private void ChangeUnits(object sender, SelectionChangedEventArgs e) { if (ready && !updating) Refresh(); }
    private void ChangeMode(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || updating) return;
        current.RadiusMode = (RadiusMode)ModeBox.SelectedIndex; dirty = true; Refresh();
    }
    private void SelectPoint(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || updating) return;
        PlannerMap.SelectedPoint = PointsBox.SelectedIndex; PlannerMap.InvalidateVisual(); UpdatePointFields();
    }
    private void UpdatePointFields()
    {
        int i = PointsBox.SelectedIndex;
        PointX.Text = i >= 0 ? current.Points[i].X.ToString("0.##", CultureInfo.CurrentCulture) : "";
        PointY.Text = i >= 0 ? current.Points[i].Y.ToString("0.##", CultureInfo.CurrentCulture) : "";
    }
    private bool CanEdit() => session is null && DemoData.IsCurrent(current);
    private void RemovePoint(object sender, RoutedEventArgs e)
    {
        int i = PointsBox.SelectedIndex;
        if (!CanEdit() || i < 0) return;
        current.Points.RemoveAt(i); PlannerMap.SelectedPoint = Math.Min(i, current.Points.Count-1); dirty = true; Refresh();
    }
    private void Reorder(int offset)
    {
        int i = PointsBox.SelectedIndex, next = i+offset;
        if (!CanEdit() || i < 0 || next < 0 || next >= current.Points.Count) return;
        (current.Points[i], current.Points[next]) = (current.Points[next], current.Points[i]);
        PlannerMap.SelectedPoint = next; dirty = true; Refresh();
    }
    private void MoveUp(object sender, RoutedEventArgs e) => Reorder(-1);
    private void MoveDown(object sender, RoutedEventArgs e) => Reorder(1);
    private void SetPoint(object sender, RoutedEventArgs e)
    {
        int i = PointsBox.SelectedIndex;
        if (!CanEdit() || i < 0) return;
        if (!double.TryParse(PointX.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double x)
            || !double.TryParse(PointY.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double y)
            || !Geometry.IsOnMap(new(x, y)))
        { Status("Koordinaten müssen endliche Zahlen zwischen 0 und 3000 sein."); return; }
        current.Points[i] = new(x,y); dirty = true; Refresh();
    }
    private void ImportRoute(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Routendatei (*.json)|*.json", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        Run(() =>
        {
            var imported = RouteJson.Parse(RouteJson.ReadBounded(dialog.FileName));
            if (imported.Routes.Count != 1 || imported.DemoHistory.Count != 0)
                throw new InvalidDataException("Routenimport benötigt genau eine Route ohne Verlauf.");
            if (MayLeave()) SwitchTo(imported.Routes[0].Copy() with { Id = Guid.NewGuid() }, true);
        });
    }
    private void ExportRoute(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Routendatei (*.json)|*.json", FileName = "route.json", AddExtension = true };
        if (dialog.ShowDialog(this) != true) return;
        if (Run(() => RouteJson.WriteAtomic(dialog.FileName, RouteJson.Serialize(new() { Routes = [current.Copy()] }))))
            Status("Route exportiert · " + dialog.FileName);
    }
    private void ClearData(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Alle gespeicherten Routen und Demo-Statistiken sowie ungespeicherte Änderungen löschen? " +
            "Exportierte Dateien bleiben erhalten.\n\n" + store.LibraryPath,
            "Lokale Daten löschen", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        if (Run(store.Delete))
        {
            storageBlocked = false; library = new(); SwitchTo(new(), true);
            Status("Lokale Bibliothek gelöscht. Exporte ggf. separat löschen.");
        }
    }
    private void FitMap(object sender, RoutedEventArgs e) => PlannerMap.Fit();
    private void ZoomIn(object sender, RoutedEventArgs e) => PlannerMap.ZoomBy(1.2);
    private void ZoomOut(object sender, RoutedEventArgs e) => PlannerMap.ZoomBy(1/1.2);

    private void StartDemo(object sender, RoutedEventArgs e)
    {
        if (!Run(() => events = provider.CreateEvents(current))) return;
        StopPlayback(); confirmed.Clear(); session = new(current); eventIndex = 0;
        ProviderStatus.Text = provider.SourceLabel + " · läuft (Zeitraffer)";
        timer.Start(); Refresh();
    }
    private void DemoTick(object? sender, EventArgs e)
    {
        if (session is null || eventIndex >= events.Count) { StopPlayback(); return; }
        DemoSummary? summary = null;
        if (!Run(() => summary = session.Apply(events[eventIndex++]))) { StopPlayback(); return; }
        PlannerMap.DemoPosition = session.Position;
        if (summary is not null)
        {
            var completed = summary;
            bool saved = Run(() => Commit(library with { DemoHistory = library.DemoHistory.TakeLast(199).Append(completed).ToList() }));
            StopPlayback();
            ProviderStatus.Text = $"DEMO beendet · {completed.DurationSeconds}s simuliert · {completed.Distance:0} KE · " +
                (saved ? "lokal gespeichert" : "Speichern fehlgeschlagen");
        }
        Refresh();
    }
    private void StopPlayback()
    {
        timer.Stop(); session = null; PlannerMap.DemoPosition = null;
        ProviderStatus.Text = provider.SourceLabel + " · gestoppt / keine Live-Daten";
    }
    private void StopDemo(object sender, RoutedEventArgs e) { StopPlayback(); Refresh(); }
    private void OpenCompact(object sender, RoutedEventArgs e)
    {
        if (compact is not null) { compact.Activate(); return; }
        compact = new CompactWindow();
        compact.Closed += (_, _) => { compact = null; if (!closing) { Show(); Activate(); } };
        compact.UpdateRoute(current, PlannerMap.VisibleSpawns, confirmed, PlannerMap.DemoPosition);
        compact.Show(); Hide();
    }
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!MayLeave()) { e.Cancel = true; return; }
        closing = true; timer.Stop(); compact?.Close();
    }
}
