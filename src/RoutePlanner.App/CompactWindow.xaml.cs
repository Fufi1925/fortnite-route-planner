using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RoutePlanner.Core;
using Geometry = RoutePlanner.Core.Geometry;

namespace RoutePlanner.App;
public partial class CompactWindow : Window
{
    public event Action? AdvanceRequested;
    public event Action? StopRequested;
    public CompactWindow() { InitializeComponent(); Loaded += (_,_) => PlaceTopRight(); }
    private void PlaceTopRight()
    {
        var area = SystemParameters.WorkArea;
        Width = Math.Min(Width,area.Width); Height = Math.Min(Height,area.Height);
        Left = Math.Max(area.Left,area.Right-Width-16); Top = area.Top+16;
    }
    public void UpdateRoute(RoutePlan route,string mapVersion,ImageSource? image,IReadOnlyList<SpawnPoint> spawns,
        HashSet<string> confirmed,int visited,bool running,string progress,bool showFanNotice = false)
    {
        FanNotice.Text = PublicMapContract.FanNotice; FanNotice.Visibility = showFanNotice ? Visibility.Visible : Visibility.Collapsed;
        RouteTitle.Text = route.Name;
        Metrics.Text = $"{route.Points.Count} Wegpunkte · {Geometry.Length(route.Points):0} KE geplante Strecke";
        CompactMap.Route = route; CompactMap.ActiveMapVersion = mapVersion; CompactMap.MapImage = image;
        CompactMap.VisibleSpawns = spawns; CompactMap.Confirmed = confirmed; CompactMap.VisitedPoints = visited; CompactMap.InvalidateVisual();
        ProgressLabel.Text = running ? progress : "Keine aktive Route · manuelle Planung";
        NextPointButton.IsEnabled = running;
    }
    private void AdvancePoint(object sender,RoutedEventArgs e) => AdvanceRequested?.Invoke();
    private void StopRoute(object sender,RoutedEventArgs e) => StopRequested?.Invoke();
    private void TopmostChanged(object sender,RoutedEventArgs e) => Topmost = ((CheckBox)sender).IsChecked == true;
    private void ReturnToPlanner(object sender,RoutedEventArgs e) => Close();
}
