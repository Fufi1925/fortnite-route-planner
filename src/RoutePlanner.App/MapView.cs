using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RoutePlanner.Core;
using Geometry = RoutePlanner.Core.Geometry;

namespace RoutePlanner.App;

public sealed class MapView : FrameworkElement
{
    public RoutePlan? Route { get; set; }
    public IReadOnlyList<SpawnPoint> VisibleSpawns { get; set; } = [];
    public HashSet<string> Confirmed { get; set; } = [];
    public MapPoint? DemoPosition { get; set; }
    public bool Editable { get; set; } = true;
    public int SelectedPoint { get; set; } = -1;
    public event Action? RouteEdited;
    public event Action<int>? PointSelected;
    public event Action<string>? SpawnClicked;
    private double zoom = 1;
    private Vector pan;
    private Point start, last;
    private int draggingPoint = -1;
    private bool panning, pendingAdd, moved;
    private double Scale => Math.Max(.001, Math.Min(ActualWidth, ActualHeight) / 3240 * zoom);
    private Vector Offset => new((ActualWidth - 3000 * Scale) / 2 + pan.X,
        (ActualHeight - 3000 * Scale) / 2 + pan.Y);
    private MapPoint World(Point p) => new((p.X - Offset.X) / Scale, (p.Y - Offset.Y) / Scale);
    private static Point P(MapPoint p) => new(p.X, p.Y);
    private static Brush Brush(string color)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
        brush.Freeze(); return brush;
    }
    private static readonly Brush Water = Brush("#141C24"), Land = Brush("#293239"),
        Highland = Brush("#323D42"), Grid = Brush("#39444B"), White = Brush("#F3F5F6"),
        Muted = Brush("#A3ADB5"), Corridor = Brush("#286E7B87"), Manual = Brush("#D2DBE0");
    public MapView()
    {
        Focusable = true; ClipToBounds = true; Cursor = Cursors.Cross;
        ToolTip = "Linksklick: Wegpunkt · Punkt ziehen: verschieben · Rechts ziehen: Karte bewegen · Mausrad: Zoom · Spawn anklicken: manuell markieren";
    }
    public void Fit() { zoom = 1; pan = default; InvalidateVisual(); }
    public void ZoomBy(double factor)
    {
        zoom = Math.Clamp(zoom * factor, .6, 8); InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(Brush("#10171D"), null, new Rect(RenderSize));
        double s = Scale;
        dc.PushTransform(new MatrixTransform(s, 0, 0, s, Offset.X, Offset.Y));
        dc.PushClip(new RectangleGeometry(new Rect(0, 0, 3000, 3000)));
        dc.DrawRectangle(Water, null, new Rect(0, 0, 3000, 3000));
        Polygon(dc, Land, [new(190, 620), new(600, 200), new(1500, 150), new(2200, 380),
            new(2750, 850), new(2800, 1800), new(2350, 2650), new(1500, 2810), new(510, 2570), new(220, 1800)]);
        Polygon(dc, Highland, [new(500, 700), new(1130, 430), new(1730, 620), new(1490, 1240), new(640, 1400)]);
        Polygon(dc, Highland, [new(1650, 1750), new(2340, 1390), new(2560, 2030), new(2140, 2490), new(1600, 2370)]);
        dc.DrawEllipse(Water, null, new Point(1800, 1300), 290, 180);
        for (int i = 0; i <= 3000; i += 300)
        {
            dc.DrawLine(new Pen(Grid, .6 / s), new(i, 0), new(i, 3000));
            dc.DrawLine(new Pen(Grid, .6 / s), new(0, i), new(3000, i));
        }
        Text(dc, "N ↑", new(140, 160), 12 / s, Muted);
        Text(dc, "N O R D F E L D", new(750, 750), 12 / s, Muted);
        Text(dc, "DEMOSEE", new(1600, 1280), 10 / s, Muted);
        Text(dc, "S Ü D H A I N", new(1700, 2150), 12 / s, Muted);
        if (Route is { } route && DemoData.IsCurrent(route))
        {
            if (route.RadiusMode == RadiusMode.Corridor && route.Points.Count > 1)
            {
                var pen = new Pen(Corridor, route.Radius * 2) { StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
                dc.DrawGeometry(null, pen, Path(route.Points));
            }
            else foreach (var point in route.Points) dc.DrawEllipse(Corridor, null, P(point), route.Radius, route.Radius);
            foreach (var spawn in VisibleSpawns)
            {
                double r = 3.5 / s;
                var center = P(spawn.Position);
                if (Confirmed.Contains(spawn.Id))
                    dc.DrawRectangle(Manual, new Pen(White, 1 / s), new Rect(center.X-r, center.Y-r, 2*r, 2*r));
                else dc.DrawEllipse(Water, new Pen(Muted, 1.3 / s), center, r, r);
            }
            if (route.Points.Count > 1) dc.DrawGeometry(null, new Pen(White, 2 / s), Path(route.Points));
            for (int i = 0; i < route.Points.Count; i++)
            {
                var point = P(route.Points[i]); double r = (i == SelectedPoint ? 12 : 10) / s;
                dc.DrawEllipse(i == 0 ? White : Water, new Pen(White, (i == SelectedPoint ? 3 : 1.5) / s), point, r, r);
                Text(dc, (i+1).ToString(CultureInfo.InvariantCulture), new(point.X-4/s, point.Y-8/s), 11/s, i==0 ? Water : White);
                if (i == route.Points.Count-1 && i > 0)
                    dc.DrawRectangle(White, null, new Rect(point.X + 10/s, point.Y - 16/s, 8/s, 5/s));
            }
        }
        if (DemoPosition is { } player)
            dc.DrawEllipse(null, new Pen(White, 3 / s), P(player), 19 / s, 19 / s);
        dc.Pop(); dc.Pop();
        Text(dc, "DEMO · NEUTRALE BEISPIELKARTE", new(16, ActualHeight-30), 11, Muted);
    }
    private static StreamGeometry Path(IReadOnlyList<MapPoint> points)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(P(points[0]), false, false);
            context.PolyLineTo(points.Skip(1).Select(P).ToArray(), true, false);
        }
        geometry.Freeze(); return geometry;
    }
    private static void Polygon(DrawingContext dc, Brush fill, Point[] points)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        { context.BeginFigure(points[0], true, true); context.PolyLineTo(points.Skip(1).ToArray(), true, false); }
        dc.DrawGeometry(fill, null, geometry);
    }
    private void Text(DrawingContext dc, string value, Point at, double size, Brush brush) => dc.DrawText(
        new FormattedText(value, CultureInfo.GetCultureInfo("de-DE"), FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip), at);

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        var at = e.GetPosition(this); var before = World(at);
        zoom = Math.Clamp(zoom * (e.Delta > 0 ? 1.15 : 1/1.15), .6, 8);
        var after = World(at); pan += new Vector((after.X-before.X)*Scale, (after.Y-before.Y)*Scale);
        InvalidateVisual(); e.Handled = true;
    }
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e); Focus(); start = last = e.GetPosition(this); moved = false;
        if (e.ChangedButton == MouseButton.Right || e.ChangedButton == MouseButton.Middle)
        { panning = true; CaptureMouse(); e.Handled = true; return; }
        if (e.ChangedButton != MouseButton.Left || !Editable || Route is null || !DemoData.IsCurrent(Route)) return;
        var at = World(start);
        draggingPoint = Route.Points.FindIndex(p => Geometry.Distance(p, at) <= 13 / Scale);
        if (draggingPoint >= 0)
        { SelectedPoint = draggingPoint; PointSelected?.Invoke(draggingPoint); }
        else
        {
            var spawn = VisibleSpawns.OrderBy(p => Geometry.Distance(p.Position, at))
                .FirstOrDefault(p => Geometry.Distance(p.Position, at) <= 8 / Scale);
            if (spawn is not null) { SpawnClicked?.Invoke(spawn.Id); InvalidateVisual(); return; }
            pendingAdd = Geometry.IsOnMap(at);
        }
        CaptureMouse(); InvalidateVisual(); e.Handled = true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e); var at = e.GetPosition(this);
        if ((at-start).Length > 3) moved = true;
        if (panning) { pan += at-last; InvalidateVisual(); }
        else if (draggingPoint >= 0 && Route is not null && moved)
        {
            var p = World(at);
            Route.Points[draggingPoint] = new(Math.Clamp(p.X, 0, 3000), Math.Clamp(p.Y, 0, 3000));
            RouteEdited?.Invoke(); InvalidateVisual();
        }
        last = at;
    }
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (pendingAdd && !moved && Route is not null && Route.Points.Count < 500)
        {
            var p = World(e.GetPosition(this));
            if (Geometry.IsOnMap(p))
            { Route.Points.Add(p); SelectedPoint = Route.Points.Count-1; RouteEdited?.Invoke(); PointSelected?.Invoke(SelectedPoint); }
        }
        pendingAdd = panning = false; draggingPoint = -1;
        ReleaseMouseCapture(); InvalidateVisual();
    }
    protected override void OnLostMouseCapture(MouseEventArgs e)
    { pendingAdd = panning = false; draggingPoint = -1; base.OnLostMouseCapture(e); }
}
