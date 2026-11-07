namespace RoutePlanner.Core;

public readonly record struct MapPoint(double X, double Y);
public enum RadiusMode { Corridor, Waypoints }

public sealed record RoutePlan
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "Neue Route";
    public string MapVersion { get; init; } = DemoData.MapVersion;
    public double Radius { get; set; } = 110;
    public RadiusMode RadiusMode { get; set; }
    public List<MapPoint> Points { get; init; } = [];
    public RoutePlan Copy(bool newIdentity = false) => new()
    {
        Id = newIdentity ? Guid.NewGuid() : Id,
        Name = newIdentity ? Name[..Math.Min(Name.Length, 100)] + " – Kopie" : Name,
        MapVersion = MapVersion, Radius = Radius, RadiusMode = RadiusMode, Points = [.. Points]
    };
}

public sealed record SpawnPoint(string Id, MapPoint Position);
public sealed record DemoSummary(Guid Id, Guid RouteId, string RouteName, string MapVersion,
    DateTimeOffset StartedAt, int DurationSeconds, double Distance, int SimulatedStops);

public sealed record LibraryDocument
{
    public int SchemaVersion { get; init; } = 2;
    public List<RoutePlan> Routes { get; init; } = [];
    public List<DemoSummary> DemoHistory { get; init; } = [];
}

public static class DemoData
{
    public const string MapVersion = "DEMO-NEUTRAL-2026-01";
    public const double MapSize = 3000;
    // Integer arithmetic makes this fixture identical across runtimes.
    public static IReadOnlyList<SpawnPoint> Spawns { get; } = Enumerable.Range(0, 600)
        .Select(i => new SpawnPoint($"D{i:000}", new MapPoint(
            100 + (i * 947 + 73) % 2800, 100 + (i * 613 + 181) % 2800))).ToArray();

    public static RoutePlan Example() => new()
    {
        Name = "Nordpfad · Demo",
        Points = [new(600, 2400), new(900, 1900), new(1450, 1600), new(1700, 950), new(2400, 650)]
    };
    public static bool IsCurrent(RoutePlan route) => route.MapVersion == MapVersion;
}
