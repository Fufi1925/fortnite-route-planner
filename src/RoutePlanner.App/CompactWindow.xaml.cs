using System.Windows;
using System.Windows.Controls;
using RoutePlanner.Core;

namespace RoutePlanner.App;
public partial class CompactWindow : Window
{
    public CompactWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PlaceTopRight();
    }
    // WPF work area uses device-independent units, including primary taskbar exclusion.
    // Dragging the standard OS title bar moves this ordinary window to any other monitor.
    private void PlaceTopRight()
    {
        var area = SystemParameters.WorkArea;
        Width = Math.Min(Width, area.Width); Height = Math.Min(Height, area.Height);
        Left = Math.Max(area.Left, area.Right-Width-16); Top = area.Top+16;
    }
    public void UpdateRoute(RoutePlan route, IReadOnlyList<SpawnPoint> spawns, HashSet<string> confirmed, MapPoint? position)
    {
        RouteTitle.Text = route.Name;
        Metrics.Text = DemoData.IsCurrent(route)
            ? $"{route.Points.Count} Punkte · {Geometry.Length(route.Points):0} KE · {spawns.Count} mögliche Spawns\n{route.MapVersion}"
            : "VERALTETE / FREMDE KARTE · Darstellung gesperrt\n" + route.MapVersion;
        CompactMap.Route = route; CompactMap.VisibleSpawns = spawns; CompactMap.Confirmed = confirmed;
        CompactMap.DemoPosition = position; CompactMap.InvalidateVisual();
    }
    private void TopmostChanged(object sender, RoutedEventArgs e) => Topmost = ((CheckBox)sender).IsChecked == true;
    private void ReturnToPlanner(object sender, RoutedEventArgs e) => Close();
    private void ResetPosition(object sender, RoutedEventArgs e) => PlaceTopRight();
}
