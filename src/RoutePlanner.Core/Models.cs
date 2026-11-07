using System.ComponentModel;

namespace RoutePlanner.Core;

public readonly record struct MapPoint(double X, double Y);
public enum RadiusMode { Corridor, Waypoints }

public sealed record RoutePlan : INotifyPropertyChanged
{
    public Guid Id { get; init; } = Guid.NewGuid();
    private string name = "Neue Route";
    public string Name
    {
        get => name;
        set
        {
            if (name == value) return;
            name = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    // An editable route is an entity: its selection identity must not change with its name or UI event subscriptions.
    public bool Equals(RoutePlan? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public string MapVersion { get; init; } = MapSpace.WorkspaceId;
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

public sealed record SpawnPoint(string Id, MapPoint Position, bool ManuallyConfirmed = false, string? Source = null);
public sealed record RouteActivity(Guid Id, Guid RouteId, string RouteName, string MapVersion,
    DateTimeOffset StartedAt, int DurationSeconds, double PlannedDistance, int VisitedPoints, int TotalPoints, bool Completed);

public sealed record MapAsset
{
    public string Id { get; init; } = MapSpace.WorkspaceId;
    public string Name { get; init; } = "Freie Arbeitsfläche";
    public string Source { get; init; } = "Eigene Einträge";
    public List<SpawnPoint> Spawns { get; init; } = [];
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasImage => Id != MapSpace.WorkspaceId;
}

public sealed record LibraryDocument
{
    public int SchemaVersion { get; init; } = 3;
    public List<RoutePlan> Routes { get; init; } = [];
    public List<MapAsset> Maps { get; init; } = [];
    public List<RouteActivity> Activities { get; init; } = [];
}

public static class MapSpace
{
    public const string WorkspaceId = "WORKSPACE-3000-V1";
    public const double MapSize = 3000;
    public static bool IsLegacyExample(RoutePlan route) => route.MapVersion.StartsWith("DEMO-", StringComparison.Ordinal);
}
