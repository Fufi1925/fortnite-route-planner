using System.Windows;
using System.Windows.Media;
using RoutePlanner.Core;
using Geometry = RoutePlanner.Core.Geometry;

namespace RoutePlanner.App;
public partial class CompactWindow : Window
{
    public event Action? AdvanceRequested;
    public event Action? StopRequested;
    public CompactWindow() : this(false) { }
    public CompactWindow(bool pinForRoute)
    {
        InitializeComponent(); Topmost = pinForRoute;
        Loaded += (_,_) => PlaceTopRight();
    }
    private void PlaceTopRight()
    {
        var area = SystemParameters.WorkArea;
        Width = Math.Min(Width,area.Width); Height = Math.Min(Height,area.Height);
        Left = Math.Max(area.Left,area.Right-Width-16); Top = area.Top+16;
    }
    public void UpdateRoute(RoutePlan route,string mapVersion,ImageSource? image,
        int visited,bool running,string progress,bool showFanNotice = false,string launchStatus = "")
    {
        FanNotice.Text = PublicMapContract.FanNotice; FanNotice.Visibility = showFanNotice ? Visibility.Visible : Visibility.Collapsed;
        RouteTitle.Text = route.Name;
        Metrics.Text = $"{route.Points.Count} Wegpunkte · {Geometry.Length(route.Points):0} KE geplante Strecke";
        CompactMap.Route = route; CompactMap.ActiveMapVersion = mapVersion; CompactMap.MapImage = image;
        // Only the route is shown in the compact window; no chest/loot positions.
        CompactMap.VisibleSpawns = []; CompactMap.Confirmed = []; CompactMap.VisitedPoints = visited; CompactMap.InvalidateVisual();
        ProgressLabel.Text = running ? progress : "Keine aktive Route · manuelle Planung";
        EpicStatus.Text = launchStatus; EpicStatus.Visibility = string.IsNullOrEmpty(launchStatus) ? Visibility.Collapsed : Visibility.Visible;
        NextPointButton.IsEnabled = running;
    }
    private void AdvancePoint(object sender,RoutedEventArgs e) => AdvanceRequested?.Invoke();
    private void StopRoute(object sender,RoutedEventArgs e) => StopRequested?.Invoke();
    private void ShowDisplayHelp(object sender,RoutedEventArgs e) => Dialogs.Show(this,
        "Dieses Routenfenster wird beim Routenstart automatisch oben rechts und im Vordergrund geöffnet.\n\n"+
        "Für die Anzeige über einem Spiel Fensterbetrieb oder randloses Vollbild wählen. Exklusives Vollbild kann das Desktop-Fenster verdecken.\n\n"+
        "Zum Bedienen bei Bedarf mit Alt-Tab wechseln. ‚Fenster im Vordergrund‘ lässt sich abschalten; ‚Zur Planung‘ bringt dich zurück.\n\n"+
        "Die App erkennt weder Fortnite noch deinen Fenstermodus. Eine tatsächliche In-Game-Anzeige wurde hier nicht mit Fortnite getestet.","Routenfenster anzeigen");
    private void ReturnToPlanner(object sender,RoutedEventArgs e) => Close();
}
