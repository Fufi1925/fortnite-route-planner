namespace RoutePlanner.Core;

public static class Geometry
{
    public const double Epsilon = 1e-7;
    public static bool IsOnMap(MapPoint p) => double.IsFinite(p.X) && double.IsFinite(p.Y)
        && p.X >= 0 && p.Y >= 0 && p.X <= MapSpace.MapSize && p.Y <= MapSpace.MapSize;

    public static double Distance(MapPoint a, MapPoint b) => Math.Sqrt(
        (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    public static double PointToSegment(MapPoint p, MapPoint a, MapPoint b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double lengthSquared = dx * dx + dy * dy;
        if (lengthSquared == 0) return Distance(p, a);
        double t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSquared, 0, 1);
        return Distance(p, new(a.X + t * dx, a.Y + t * dy));
    }

    public static double PointToRoute(MapPoint p, IReadOnlyList<MapPoint>? points, RadiusMode mode)
    {
        if (points is null || points.Count == 0) return double.PositiveInfinity;
        if (points.Count == 1 || mode == RadiusMode.Waypoints) return points.Min(q => Distance(p, q));
        double minimum = double.PositiveInfinity;
        for (int i = 1; i < points.Count; i++)
            minimum = Math.Min(minimum, PointToSegment(p, points[i - 1], points[i]));
        return minimum;
    }

    public static double Length(IReadOnlyList<MapPoint> points)
    {
        double length = 0;
        for (int i = 1; i < points.Count; i++) length += Distance(points[i - 1], points[i]);
        return length;
    }
}

// Fixed uniform grid; query segment bounding boxes before exact Euclidean filtering.
public sealed class SpawnIndex
{
    private const double CellSize = 150;
    private readonly Dictionary<(int X, int Y), List<SpawnPoint>> cells = [];
    private readonly string mapVersion;
    public SpawnIndex(IEnumerable<SpawnPoint> points, string mapVersion = MapSpace.WorkspaceId)
    {
        this.mapVersion = mapVersion;
        foreach (var point in points)
        {
            if (!Geometry.IsOnMap(point.Position)) throw new ArgumentException("Spawn außerhalb der Karte.");
            var key = Cell(point.Position);
            if (!cells.TryGetValue(key, out var bucket)) cells[key] = bucket = [];
            bucket.Add(point);
        }
    }
    private static (int, int) Cell(MapPoint p) => ((int)(p.X / CellSize), (int)(p.Y / CellSize));
    public IReadOnlyList<SpawnPoint> Query(RoutePlan route)
    {
        Validation.RouteGeometry(route);
        if (route.MapVersion != mapVersion) return [];
        if (route.Area is { } area)
            return cells.Values.SelectMany(bucket => bucket).Where(p => route.VisibleKinds.Contains(p.Kind) && area.Contains(p.Position)).OrderBy(p => p.Id,StringComparer.Ordinal).ToArray();
        if (route.Points.Count == 0) return [];
        HashSet<SpawnPoint> candidates = [];
        void Box(MapPoint a, MapPoint b)
        {
            int left = (int)(Math.Max(0, Math.Min(a.X, b.X) - route.Radius) / CellSize);
            int top = (int)(Math.Max(0, Math.Min(a.Y, b.Y) - route.Radius) / CellSize);
            int right = (int)(Math.Min(MapSpace.MapSize, Math.Max(a.X, b.X) + route.Radius) / CellSize);
            int bottom = (int)(Math.Min(MapSpace.MapSize, Math.Max(a.Y, b.Y) + route.Radius) / CellSize);
            for (int x = left; x <= right; x++) for (int y = top; y <= bottom; y++)
                if (cells.TryGetValue((x, y), out var bucket)) candidates.UnionWith(bucket);
        }
        if (route.RadiusMode == RadiusMode.Waypoints || route.Points.Count == 1)
            foreach (var p in route.Points) Box(p, p);
        else for (int i = 1; i < route.Points.Count; i++) Box(route.Points[i - 1], route.Points[i]);
        return candidates.Where(p => route.VisibleKinds.Contains(p.Kind) && Geometry.PointToRoute(p.Position, route.Points, route.RadiusMode)
            <= route.Radius + Geometry.Epsilon).OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
    }
}
