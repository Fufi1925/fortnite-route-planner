using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using RoutePlanner.Core;
using Validation = RoutePlanner.Core.Validation;

namespace RoutePlanner.App;

public sealed record WaypointRow(string Label, string Coordinates);
public partial class MainWindow : Window
{
    private readonly LocalStore store;
    private readonly MapImages images;
    private readonly PublicFortniteMapClient publicMapClient = new();
    private readonly CancellationTokenSource downloads = new();
    private readonly bool autoLoadPublicMap;
    private bool mapDownloadRunning;
    private DateTimeOffset lastPublicRequest;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private LibraryDocument library = new();
    private RoutePlan current = new();
    private MapAsset? activeMap;
    private BitmapSource? activeImage;
    private SpawnIndex index = new([]);
    private ManualRouteSession? session;
    private RouteActivity? pendingActivity;
    private int visited;
    private bool ready, updating, dirty, storageBlocked, closing;
    private CompactWindow? compact;
    private WebMapWindow? webMap;
    public MainWindow() : this(null) { }
    public MainWindow(string? dataDirectory)
    {
        store = new(dataDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Routenatelier"));
        images = new(store.DirectoryPath);
        autoLoadPublicMap = dataDirectory is null;
        InitializeComponent();
        try
        {
            library = store.Load();
            current = library.Routes.FirstOrDefault(r => !MapSpace.IsLegacyExample(r))?.Copy()
                ?? new RoutePlan { MapVersion = library.Maps.LastOrDefault(m => m.IsPublicFortniteMap)?.Id ?? MapSpace.WorkspaceId };
        }
        catch (Exception ex) when (IsDataError(ex))
        {
            storageBlocked = true;
            Dialogs.Show(this, "Die Bibliothek konnte nicht gelesen werden und wird nicht überschrieben. Sichere die Datei, bevor du lokale Daten löschst.\n\n" + store.LibraryPath + "\n" + ex.Message, "Bibliothek nicht verfügbar");
        }
        PlannerMap.RouteEdited += () => { dirty = true; visited = 0; Refresh(); };
        PlannerMap.PointSelected += i => PointsBox.SelectedIndex = i;
        PlannerMap.MarkerRequested += AddManualMarker;
        PlannerMap.AreaSelected += SetSearchArea;
        PlannerMap.SpawnClicked += ToggleMarker;
        timer.Tick += (_, _) => RefreshProgress();
        Closing += OnClosing;
        Loaded += async (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Width = Math.Max(MinWidth, Math.Min(Width, area.Width)); Height = Math.Max(MinHeight, Math.Min(Height, area.Height));
            Left = Math.Max(area.Left, area.Left+(area.Width-Width)/2); Top = Math.Max(area.Top, area.Top+(area.Height-Height)/2);
            if (autoLoadPublicMap && !initialMapAttempted && library.Routes.Count == 0)
            {
                initialMapAttempted = true;
                await LoadPublicMapAsync(showErrors:false);
            }
        };
        ready = true; LoadActiveMap(); Refresh();
    }
    private bool initialMapAttempted;
    private static bool IsDataError(Exception ex) => ex is InvalidDataException or IOException or UnauthorizedAccessException
        or ArgumentException or NotSupportedException or FormatException or System.Security.SecurityException;
    private bool Run(Action action)
    {
        try { action(); return true; }
        catch (Exception ex) when (IsDataError(ex))
        {
            Status("Fehler: " + ex.Message);
            Dialogs.Show(compact?.IsVisible == true ? compact : this, ex.Message, "Aktion nicht ausgeführt");
            return false;
        }
    }
    private void Status(string value) => StatusLabel.Text = value;
    private void Commit(LibraryDocument next)
    {
        if (storageBlocked) throw new InvalidDataException("Bibliothek gesperrt. Beschädigte Datei erst sichern und erneut laden oder gezielt löschen.");
        store.Save(next); library = next;
    }
    private List<RouteActivity> ActivitiesWithPending() => pendingActivity is null ? [.. library.Activities]
        : library.Activities.TakeLast(199).Append(pendingActivity).ToList();
    private bool TrySave()
    {
        bool saved = Run(() =>
        {
            Validation.Route(current);
            Commit(library with { Routes = library.Routes.Where(r => r.Id != current.Id).Append(current.Copy()).ToList(), Activities = ActivitiesWithPending() });
            dirty = false; pendingActivity = null;
        });
        if (saved) { Refresh(); Status("Gespeichert · nur auf diesem Gerät"); }
        return saved;
    }
    private bool MayLeave()
    {
        if (session is not null)
        {
            if (Dialogs.Show(this, "Den aktiven Routengang beenden? Dein manuell bestätigter Fortschritt wird gespeichert.", "Aktive Route", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return false;
            EndSession();
        }
        if (!dirty && pendingActivity is null) return true;
        var answer = Dialogs.Show(this, "Änderungen an dieser Route speichern?", "Ungespeicherte Änderungen", MessageBoxButton.YesNoCancel);
        return answer == MessageBoxResult.No || answer == MessageBoxResult.Yes && TrySave();
    }
    private void LoadActiveMap()
    {
        activeImage = null;
        activeMap = library.Maps.FirstOrDefault(m => m.Id == current.MapVersion)
            ?? (current.MapVersion == MapSpace.WorkspaceId ? new MapAsset() : null);
        if (activeMap?.HasImage == true)
        {
            try { activeImage = images.Load(activeMap); }
            catch (Exception ex) when (IsDataError(ex)) { activeMap = null; Status("Kartenbild nicht verfügbar: " + ex.Message); }
        }
        index = new(activeMap?.Spawns ?? [], activeMap?.Id ?? MapSpace.WorkspaceId);
    }
    private void Refresh()
    {
        if (!ready) return;
        updating = true;
        int selection = PlannerMap.SelectedPoint;
        var choices = library.Routes.Where(r => !MapSpace.IsLegacyExample(r)).Select(r => r.Id == current.Id ? current : r).ToList();
        if (choices.All(r => r.Id != current.Id)) choices.Add(current);
        RoutesBox.ItemsSource = choices; RoutesBox.SelectedItem = current;
        RouteCount.Text = library.Routes.Count(r => !MapSpace.IsLegacyExample(r)).ToString(CultureInfo.CurrentCulture);
        NameBox.Text = current.Name; NameBox.IsEnabled = session is null;
        RadiusSlider.Value = current.Radius; RadiusSlider.IsEnabled = session is null && current.Area is null;
        ModeBox.SelectedIndex = (int)current.RadiusMode; ModeBox.IsEnabled = session is null && current.Area is null;
        RadiusLabel.Text = current.Area is not null ? "BEREICH" : UnitsBox.SelectedIndex == 1 ? $"{current.Radius/30:0.0} %" : $"{current.Radius:0.#} KE";
        PointsBox.ItemsSource = current.Points.Select((p,i) => new WaypointRow(
            $"{i+1:00}   {(i == 0 ? "Start" : i == current.Points.Count-1 ? "Ziel" : "Wegpunkt")}", $"{p.X:0.#}  /  {p.Y:0.#} KE")).ToArray();
        PointsBox.SelectedIndex = Math.Clamp(selection,-1,current.Points.Count-1);
        PointCountLabel.Text = current.Points.Count.ToString(CultureInfo.CurrentCulture);
        EmptyPointsHint.Visibility = current.Points.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyMapHint.Visibility = current.Points.Count == 0 && activeImage is null ? Visibility.Visible : Visibility.Collapsed;
        PlannerMap.Route = current; PlannerMap.MapImage = activeImage;
        PlannerMap.ActiveMapVersion = activeMap?.Id ?? MapSpace.WorkspaceId;
        PlannerMap.Editable = CanEdit(); PlannerMap.PlaceMarkers = MarkerTool.IsChecked == true;
        AllMarkers.IsEnabled = current.Area is null;
        if (current.Area is not null) AllMarkers.IsChecked = false;
        PlannerMap.VisibleSpawns = current.Area is null && AllMarkers.IsChecked == true ? activeMap?.Spawns ?? [] : index.Query(current);
        PlannerMap.Confirmed = (activeMap?.Spawns ?? []).Where(s => s.ManuallyConfirmed).Select(s => s.Id).ToHashSet();
        PlannerMap.VisitedPoints = session?.VisitedPoints ?? visited; PlannerMap.InvalidateVisual();
        MapVersionLabel.Text = activeMap?.Name ?? "Zugehörige Karte öffnen";
        MapSourceLabel.Text = activeMap is null ? "Kartenkennung unbekannt · Darstellung gesperrt"
            : activeMap.IsPublicFortniteMap ? $"Fortnite-API · Abruf {activeMap.RetrievedAtUtc?.ToLocalTime():dd.MM.yyyy HH:mm}"
            : activeMap.HasImage ? activeMap.Source : "Keine Karte geladen · eigene Koordinaten";
        MapSourceLabel.ToolTip = activeMap?.IsPublicFortniteMap == true ? $"Bilddatum des Servers: {activeMap.ImageModifiedAtUtc?.ToLocalTime():dd.MM.yyyy HH:mm}. Der Anbieter kann Änderungen verzögert liefern." : null;
        ChestStatusLabel.Visibility = activeMap?.IsPublicFortniteMap == true ? Visibility.Visible : Visibility.Collapsed;
        ChestStatusLabel.Text = activeMap?.Spawns.Count > 0 ? "Truhen: eigene/importierte Orte · Vollständigkeit ungeprüft" : "Truhenpositionen: kein Datensatz angebunden";
        FanNotice.Text = PublicMapContract.FanNotice;
        FanNotice.Visibility = activeMap?.IsPublicFortniteMap == true ? Visibility.Visible : Visibility.Collapsed;
        RegionInfo.Text = current.Area is { } selected ? $"{selected.Right-selected.Left:0} × {selected.Bottom-selected.Top:0} KE · Radiusfilter pausiert" : "Bereich per Ziehen auf der Karte festlegen";
        ClearRegionButton.IsEnabled = current.Area is not null && session is null;
        SelectRegionButton.IsEnabled = CanEdit();
        SelectRegionButton.Content = PlannerMap.SelectArea ? "Auf Karte ziehen …" : "Bereich auswählen";
        PublicMapButton.IsEnabled = !mapDownloadRunning && session is null;
        RouteMetrics.Text = $"{Geometry.Length(current.Points):0} KE geplante Strecke  ·  {PlannerMap.VisibleSpawns.Count} Orte " + (current.Area is not null ? "im Bereich" : AllMarkers.IsChecked == true ? "insgesamt" : "im Radius");
        HistoryBox.ItemsSource = ActivitiesWithPending().AsEnumerable().Reverse().Select(a =>
            $"{a.RouteName}   ·   {a.StartedAt.ToLocalTime():dd.MM.yyyy HH:mm}   ·   {a.DurationSeconds/60:00}:{a.DurationSeconds%60:00} min   ·   {a.VisitedPoints}/{a.TotalPoints} manuell bestätigt   ·   {(a.Completed ? "abgeschlossen" : "beendet")}").ToArray();
        EmptyHistoryHint.Visibility = library.Activities.Count == 0 && pendingActivity is null ? Visibility.Visible : Visibility.Collapsed;
        Title = $"{(dirty ? "* " : "")}{current.Name} · Route Planner · Lokal";
        StartRouteButton.Content = session is null ? "Route starten  →" : "Aktive Route  →";
        StartRouteButton.IsEnabled = session is not null || current.Points.Count > 0 && activeMap is not null;
        updating = false; UpdatePointFields(); RefreshProgress();
    }
    private void RefreshProgress()
    {
        string progress = session is null ? "" : $"{session.VisitedPoints}/{session.TotalPoints} bestätigt  ·  {session.ElapsedSeconds/60:00}:{session.ElapsedSeconds%60:00}";
        SessionStatus.Text = progress;
        compact?.UpdateRoute(current, activeMap?.Id ?? MapSpace.WorkspaceId, activeImage, PlannerMap.VisibleSpawns,
            PlannerMap.Confirmed, session?.VisitedPoints ?? visited, session is not null, progress,activeMap?.IsPublicFortniteMap == true);
    }
    private bool CanEdit() => session is null && activeMap is not null && !MapSpace.IsLegacyExample(current);
    private void SwitchTo(RoutePlan route, bool unsaved)
    {
        current = route; dirty = unsaved; visited = 0; PlannerMap.SelectedPoint = -1; PlannerMap.SelectArea = false;
        LoadActiveMap(); PlannerMap.Fit(); Refresh();
        if (activeMap is null) Status("Die passende Karte fehlt. Route bleibt gespeichert und exportierbar; öffne ihr Kartenbild zum Bearbeiten.");
    }
    private void SelectRoute(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || updating || RoutesBox.SelectedItem is not RoutePlan route || route.Id == current.Id) return;
        // Finish the ComboBox selection transaction before rebuilding its item source.
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (closing || RoutesBox.SelectedItem is not RoutePlan selected || selected.Id == current.Id) return;
            if (MayLeave()) SwitchTo(selected.Copy(),false); else Refresh();
        }));
    }
    private void NewRoute(object sender, RoutedEventArgs e)
    { if (MayLeave()) SwitchTo(new() { MapVersion = activeMap?.Id ?? MapSpace.WorkspaceId },false); }
    private void DuplicateRoute(object sender, RoutedEventArgs e)
    { if (MayLeave()) SwitchTo(current.Copy(true),true); }
    private void DeleteRoute(object sender, RoutedEventArgs e)
    {
        if (!MayLeave()) return;
        if (Dialogs.Show(this,$"Route „{current.Name}“ dauerhaft löschen?","Route löschen",MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        if (Run(() => Commit(library with { Routes = library.Routes.Where(r => r.Id != current.Id).ToList() })))
            SwitchTo(library.Routes.FirstOrDefault(r => !MapSpace.IsLegacyExample(r))?.Copy() ?? new(),false);
    }
    private void SaveRoute(object sender, RoutedEventArgs e) => TrySave();
    private void ReloadRoute(object sender, RoutedEventArgs e)
    {
        if (!MayLeave()) return;
        Run(() =>
        {
            var loaded = store.Load(); library = loaded; storageBlocked = false;
            SwitchTo(loaded.Routes.FirstOrDefault(r => r.Id == current.Id)?.Copy()
                ?? loaded.Routes.FirstOrDefault(r => !MapSpace.IsLegacyExample(r))?.Copy() ?? new(),false);
            Status("Bibliothek vom Gerät geladen");
        });
    }
    private void RenameRoute(object sender, TextChangedEventArgs e)
    { if (ready && !updating) { current.Name = NameBox.Text; dirty = true; Title = "* " + current.Name + " · Route Planner · Lokal"; } }
    private void ChangeRadius(object sender, RoutedPropertyChangedEventArgs<double> e)
    { if (ready && !updating) { current.Radius = Math.Round(RadiusSlider.Value,2); dirty = true; Refresh(); } }
    private void ChangeUnits(object sender, SelectionChangedEventArgs e) { if (ready && !updating) Refresh(); }
    private void ChangeMode(object sender, SelectionChangedEventArgs e)
    { if (ready && !updating) { current.RadiusMode = (RadiusMode)ModeBox.SelectedIndex; dirty = true; Refresh(); } }
    private void SelectPoint(object sender, SelectionChangedEventArgs e)
    { if (ready && !updating) { PlannerMap.SelectedPoint = PointsBox.SelectedIndex; PlannerMap.InvalidateVisual(); UpdatePointFields(); } }
    private void UpdatePointFields()
    {
        int i = PointsBox.SelectedIndex;
        if (i < 0) { PointX.Text = "1500"; PointY.Text = "1500"; return; }
        PointX.Text = current.Points[i].X.ToString("0.##",CultureInfo.CurrentCulture);
        PointY.Text = current.Points[i].Y.ToString("0.##",CultureInfo.CurrentCulture);
    }
    private MapPoint? ReadCoordinates()
    {
        if (!double.TryParse(PointX.Text,NumberStyles.Float,CultureInfo.CurrentCulture,out double x)
            || !double.TryParse(PointY.Text,NumberStyles.Float,CultureInfo.CurrentCulture,out double y) || !Geometry.IsOnMap(new(x,y)))
        { Status("Koordinaten: endliche Zahlen zwischen 0 und 3000 eingeben"); return null; }
        return new(x,y);
    }
    private void AddPoint(object sender, RoutedEventArgs e)
    {
        if (!CanEdit() || current.Points.Count >= 500 || ReadCoordinates() is not { } p) return;
        current.Points.Add(p); PlannerMap.SelectedPoint = current.Points.Count-1; dirty = true; visited = 0; Refresh();
    }
    private void SetPoint(object sender, RoutedEventArgs e)
    {
        int i = PointsBox.SelectedIndex;
        if (!CanEdit() || i < 0 || ReadCoordinates() is not { } p) return;
        current.Points[i] = p; dirty = true; visited = 0; Refresh();
    }
    private void RemovePoint(object sender, RoutedEventArgs e)
    {
        int i = PointsBox.SelectedIndex; if (!CanEdit() || i < 0) return;
        current.Points.RemoveAt(i); PlannerMap.SelectedPoint = Math.Min(i,current.Points.Count-1); dirty = true; visited = 0; Refresh();
    }
    private void Reorder(int offset)
    {
        int i = PointsBox.SelectedIndex, next = i+offset;
        if (!CanEdit() || i < 0 || next < 0 || next >= current.Points.Count) return;
        (current.Points[i],current.Points[next]) = (current.Points[next],current.Points[i]);
        PlannerMap.SelectedPoint = next; dirty = true; visited = 0; Refresh();
    }
    private void MoveUp(object sender, RoutedEventArgs e) => Reorder(-1);
    private void MoveDown(object sender, RoutedEventArgs e) => Reorder(1);
    private void ImportRoute(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Routen (*.json)|*.json" };
        if (dialog.ShowDialog(this) != true) return;
        Run(() => ImportRouteFile(dialog.FileName));
    }
    public void ImportRouteFile(string path)
    {
        var document = RouteJson.Parse(RouteJson.ReadBounded(path));
        if (document.Routes.Count != 1 || document.Activities.Count != 0 || document.Maps.Count != 0)
            throw new InvalidDataException("Routenimport benötigt genau eine Route ohne Karten oder Verlauf.");
        if (MayLeave()) SwitchTo(document.Routes[0].Copy() with { Id = Guid.NewGuid() },true);
    }
    private void ExportRoute(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Routen (*.json)|*.json", FileName = "route.json", AddExtension = true };
        if (dialog.ShowDialog(this) == true && Run(() => ExportRouteFile(dialog.FileName))) Status("Route exportiert");
    }
    public void ExportRouteFile(string path) => RouteJson.WriteAtomic(path,RouteJson.Serialize(new() { Routes = [current.Copy()] }));
    private void OpenMap(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Eigene / berechtigt nutzbare Karte (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg" };
        if (dialog.ShowDialog(this) == true) Run(() => OpenMapFile(dialog.FileName));
    }
    public void OpenMapFile(string path)
    {
        if (!MayLeave()) return;
        var imported = images.Import(path);
        var existing = library.Maps.FirstOrDefault(m => m.Id == imported.Asset.Id);
        var asset = existing ?? imported.Asset;
        Commit(library with { Maps = library.Maps.Where(m => m.Id != asset.Id).Append(asset).ToList() });
        var route = current.MapVersion == asset.Id ? current : new RoutePlan { MapVersion = asset.Id };
        SwitchTo(route,false); Status("Eigene Karte geöffnet · normierte Koordinaten, keine Zusage über aktuelle Spiel-Spawns");
    }
    private void UpdateMap(MapAsset next)
    {
        Commit(library with { Maps = library.Maps.Where(m => m.Id != next.Id).Append(next).ToList() });
        activeMap = next; index = new(next.Spawns,next.Id); Refresh();
    }
    public void AddManualMarker(MapPoint point)
    {
        if (!CanEdit() || activeMap is null || !Geometry.IsOnMap(point)) return;
        if (Run(() => UpdateMap(activeMap with { Spawns = activeMap.Spawns.Append(new SpawnPoint(Guid.NewGuid().ToString("N"),point,true,"Eigene Eingabe")).ToList() })))
            Status("Eigener Ort gespeichert · keine automatische Erkennung");
    }
    private void ToggleMarker(string id)
    {
        if (!CanEdit() || activeMap is null) return;
        Run(() => UpdateMap(activeMap with { Spawns = activeMap.Spawns.Select(p => p.Id == id ? p with { ManuallyConfirmed = !p.ManuallyConfirmed } : p).ToList() }));
    }
    private void ImportMarkers(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Eigene / lizenzierte Ortsdaten (*.json)|*.json" };
        if (dialog.ShowDialog(this) == true) Run(() => ImportMarkerFile(dialog.FileName));
    }
    public void ImportMarkerFile(string path)
    {
        if (!CanEdit() || activeMap is null) throw new InvalidDataException("Zuerst die passende Karte öffnen.");
        var data = RouteJson.Parse(RouteJson.ReadBounded(path));
        if (data.Routes.Count != 0 || data.Activities.Count != 0 || data.Maps.Count != 1 || data.Maps[0].Id != activeMap.Id)
            throw new InvalidDataException("Ortsdatei benötigt genau die aktuelle Kartenkennung, ohne Routen oder Verlauf.");
        var points = data.Maps[0].Spawns.Select(p => new SpawnPoint(Guid.NewGuid().ToString("N"),p.Position,false,data.Maps[0].Source)).ToList();
        UpdateMap(activeMap with { Spawns = activeMap.Spawns.Concat(points).ToList() });
        Status($"{points.Count} statische Orte importiert · Quelle: {data.Maps[0].Source}");
    }
    private void ChangeMarkerTool(object sender, RoutedEventArgs e)
    { if (ready && !updating) { if (MarkerTool.IsChecked == true) { PlannerMap.SelectArea = false; if (current.Area is null) AllMarkers.IsChecked = true; } Refresh(); } }
    private void ChangeAllMarkers(object sender, RoutedEventArgs e) { if (ready && !updating) Refresh(); }
    private void SelectRegion(object sender,RoutedEventArgs e)
    {
        if (!CanEdit()) return;
        PlannerMap.SelectArea = !PlannerMap.SelectArea;
        if (PlannerMap.SelectArea) MarkerTool.IsChecked = false;
        Refresh(); Status(PlannerMap.SelectArea ? "Linke Maustaste halten und den gewünschten Suchbereich aufziehen" : "Bereichsauswahl beendet");
    }
    public void SetSearchArea(SearchArea? area)
    {
        if (!CanEdit()) return;
        Validation.RouteGeometry(current with { Area = area });
        current.Area = area; dirty = true; PlannerMap.SelectArea = false;
        Refresh(); Status(area is null ? "Suchbereich entfernt · Radiusfilter wieder aktiv" : "Suchbereich gesetzt · vorhandene Orte werden darin gefiltert");
    }
    private void ClearRegion(object sender,RoutedEventArgs e) => SetSearchArea(null);
    private void OpenWebMap(object sender,RoutedEventArgs e)
    {
        if (webMap is not null) { webMap.WindowState = WindowState.Normal; webMap.Activate(); return; }
        webMap = new WebMapWindow(store.DirectoryPath) { Owner = this };
        webMap.Closed += (_, _) => webMap = null;
        webMap.Show();
    }
    private async void OpenPublicMap(object sender,RoutedEventArgs e) => await LoadPublicMapAsync();
    public async Task<bool> LoadPublicMapAsync(bool showErrors = true)
    {
        if (mapDownloadRunning || session is not null || !MayLeave()) return false;
        if (DateTimeOffset.UtcNow-lastPublicRequest < TimeSpan.FromMinutes(1))
        { Status("Karte gerade abgerufen · bitte eine Minute bis zum nächsten Abruf warten"); return false; }
        mapDownloadRunning = true; lastPublicRequest = DateTimeOffset.UtcNow; Refresh(); Status("Öffentliche Fortnite-Kartenabbildung wird geladen …");
        try
        {
            var snapshot = await publicMapClient.DownloadAsync(downloads.Token);
            string temporary = Path.Combine(store.DirectoryPath,"download-"+Guid.NewGuid().ToString("N")+".png");
            try
            {
                RouteJson.WriteAtomic(temporary,snapshot.ImageBytes);
                var imported = images.Import(temporary);
                var previous = library.Maps.FirstOrDefault(m => m.Id == imported.Asset.Id);
                var asset = (previous ?? imported.Asset) with { Name = "Fortnite-Karte", Source = "Fortnite-API · öffentliches Kartenbild",
                    SourceUrl = PublicMapContract.ApiUrl, RetrievedAtUtc = snapshot.RetrievedAtUtc, ImageModifiedAtUtc = snapshot.ImageModifiedAtUtc };
                Commit(library with { Maps = library.Maps.Where(m => m.Id != asset.Id).Append(asset).ToList() });
                SwitchTo(current.MapVersion == asset.Id ? current : new RoutePlan { MapVersion = asset.Id },false);
                Status("Kartenabbildung online geladen · keine Live-Partie und keine Truhenkoordinaten vom Anbieter");
                return true;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        catch (OperationCanceledException) when (downloads.IsCancellationRequested) { return false; }
        catch (Exception ex) when (IsDataError(ex))
        {
            Status("Kartenabruf: "+ex.Message);
            if (showErrors && !closing) Dialogs.Show(this,ex.Message,"Karte nicht aktualisiert");
            return false;
        }
        finally { mapDownloadRunning = false; if (!closing) Refresh(); }
    }
    private void StartRoute(object sender, RoutedEventArgs e)
    {
        if (session is not null) { OpenCompact(sender,e); return; }
        if (!CanEdit() || current.Points.Count == 0 || !TrySave()) return;
        session = new(current); visited = 0; timer.Start(); Refresh(); OpenCompact(sender,e);
    }
    private void AdvanceSession()
    {
        if (session is null) return;
        var activity = session.Advance(); visited = session.VisitedPoints;
        if (activity is not null) RecordActivity(activity);
        Refresh();
    }
    private void EndSession()
    { if (session is not null) { visited = session.VisitedPoints; RecordActivity(session.Finish()); Refresh(); } }
    private void RecordActivity(RouteActivity activity)
    {
        session = null; timer.Stop(); pendingActivity = activity;
        if (Run(() => Commit(library with { Activities = ActivitiesWithPending() }))) pendingActivity = null;
        Status(pendingActivity is null ? "Manueller Routengang gespeichert" : "Verlauf noch ungespeichert · mit Speichern erneut versuchen");
    }
    private void OpenCompact(object sender, RoutedEventArgs e)
    {
        if (compact is not null) { compact.Activate(); return; }
        compact = new CompactWindow(); compact.AdvanceRequested += AdvanceSession; compact.StopRequested += EndSession;
        compact.Closed += (_,_) => { compact = null; if (!closing) { Show(); Activate(); } };
        RefreshProgress(); compact.Show(); Hide();
    }
    private void ClearData(object sender, RoutedEventArgs e)
    {
        if (Dialogs.Show(this,"Alle lokalen Routen, Karten, Orte und Verläufe sowie ungespeicherte Änderungen löschen? Exportierte Dateien bleiben erhalten.","Lokale Daten löschen",MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        if (Run(() => { images.DeleteKnown(library.Maps); store.Delete(); }))
        { session = null; pendingActivity = null; timer.Stop(); storageBlocked = false; library = new(); SwitchTo(new(),false); Status("Lokale Daten gelöscht"); }
    }
    private void ShowHistory(object sender, RoutedEventArgs e) { PlannerPanel.Visibility = Visibility.Collapsed; HistoryPanel.Visibility = Visibility.Visible; }
    private void HideHistory(object sender, RoutedEventArgs e) { PlannerPanel.Visibility = Visibility.Visible; HistoryPanel.Visibility = Visibility.Collapsed; }
    private void FitMap(object sender, RoutedEventArgs e) => PlannerMap.Fit();
    private void ZoomIn(object sender, RoutedEventArgs e) => PlannerMap.ZoomBy(1.2);
    private void ZoomOut(object sender, RoutedEventArgs e) => PlannerMap.ZoomBy(1/1.2);
    private void MinimizeWindow(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeWindow(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseWindow(object sender, RoutedEventArgs e) => Close();
    private void OnClosing(object? sender, CancelEventArgs e)
    { if (!MayLeave()) { e.Cancel = true; return; } closing = true; downloads.Cancel(); publicMapClient.Dispose(); timer.Stop(); compact?.Close(); }
}
