using System.ComponentModel;

namespace RoutePlanner.Core;

public readonly record struct MapPoint(double X, double Y);
public enum RadiusMode { Corridor, Waypoints }
public sealed record SearchArea(double Left, double Top, double Right, double Bottom)
{
    public bool Contains(MapPoint point) => Geometry.IsOnMap(point) && point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
    public static SearchArea FromCorners(MapPoint a, MapPoint b) => new(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));
}

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
    public SearchArea? Area { get; set; }
    public List<MapPoint> Points { get; init; } = [];
    public RoutePlan Copy(bool newIdentity = false) => new()
    {
        Id = newIdentity ? Guid.NewGuid() : Id,
        Name = newIdentity ? Name[..Math.Min(Name.Length, 100)] + " – Kopie" : Name,
        MapVersion = MapVersion, Radius = Radius, RadiusMode = RadiusMode, Area = Area, Points = [.. Points]
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
    public string? SourceUrl { get; init; }
    public DateTimeOffset? RetrievedAtUtc { get; init; }
    public DateTimeOffset? ImageModifiedAtUtc { get; init; }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasImage => Id != MapSpace.WorkspaceId;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsPublicFortniteMap => SourceUrl == PublicMapContract.ApiUrl;
}

public sealed record LibraryDocument
{
    public int SchemaVersion { get; init; } = 4;
    public List<RoutePlan> Routes { get; init; } = [];
    public List<MapAsset> Maps { get; init; } = [];
    public List<RouteActivity> Activities { get; init; } = [];
}

public static class PublicMapContract
{
    public const string ApiUrl = "https://fortnite-api.com/v1/map?language=de";
    public const string ImageUrl = "https://fortnite-api.com/images/map.png";
    public const string FanNotice = "Portions of the materials used are trademarks and/or copyrighted works of Epic Games, Inc. All rights reserved by Epic. This material is not official and is not endorsed by Epic.";
}

public static class MapSpace
{
    public const string WorkspaceId = "WORKSPACE-3000-V1";
    public const double MapSize = 3000;
    public static bool IsLegacyExample(RoutePlan route) => route.MapVersion.StartsWith("DEMO-", StringComparison.Ordinal);
}
