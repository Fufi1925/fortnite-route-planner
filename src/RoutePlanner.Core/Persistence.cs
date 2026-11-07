using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace RoutePlanner.Core;

public static class Validation
{
    public static void Route(RoutePlan? route)
    {
        if (route is null || route.Id == Guid.Empty) throw new InvalidDataException("Route oder ID fehlt.");
        Text(route.Name, 120, "Routenname");
        Text(route.MapVersion, 100, "Datenversion");
        RouteGeometry(route);
    }
    public static void RouteGeometry(RoutePlan route)
    {
        if (!double.IsFinite(route.Radius) || route.Radius < 0 || route.Radius > 1500)
            throw new InvalidDataException("Radius muss zwischen 0 und 1500 KE liegen.");
        if (!Enum.IsDefined(route.RadiusMode)) throw new InvalidDataException("Unbekannter Radiusmodus.");
        if (route.Points is null || route.Points.Count > 500 || route.Points.Any(p => !Geometry.IsOnMap(p)))
            throw new InvalidDataException("Maximal 500 Wegpunkte innerhalb 0…3000 erlaubt.");
        if (route.Area is { } area && (!Geometry.IsOnMap(new(area.Left,area.Top)) || !Geometry.IsOnMap(new(area.Right,area.Bottom))
            || area.Right <= area.Left || area.Bottom <= area.Top))
            throw new InvalidDataException("Suchbereich muss innerhalb der Karte liegen und eine positive Fläche haben.");
    }
    public static void Text(string? text, int max, string label)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > max || text.Any(char.IsControl))
            throw new InvalidDataException($"{label}: leer, zu lang oder Steuerzeichen enthalten.");
    }
    public static void Library(LibraryDocument document)
    {
        if (document.SchemaVersion != 4 || document.Routes is null || document.Maps is null || document.Activities is null
            || document.Routes.Count > 200 || document.Maps.Count > 100 || document.Activities.Count > 200)
            throw new InvalidDataException("Bibliothek: ungültige Version oder zu viele Einträge.");
        foreach (var route in document.Routes) Route(route);
        if (document.Routes.Select(r => r.Id).Distinct().Count() != document.Routes.Count)
            throw new InvalidDataException("Doppelte Routen-ID.");
        if (document.Maps.Select(m => m?.Id).Distinct().Count() != document.Maps.Count)
            throw new InvalidDataException("Doppelte Karten-ID.");
        foreach (var map in document.Maps) Map(map);
        foreach (var match in document.Activities)
        {
            if (match is null || match.Id == Guid.Empty || match.RouteId == Guid.Empty
                || match.DurationSeconds < 0 || match.DurationSeconds > 86400
                || !double.IsFinite(match.PlannedDistance) || match.PlannedDistance < 0 || match.PlannedDistance > 3_000_000
                || match.TotalPoints < 1 || match.TotalPoints > 500 || match.VisitedPoints < 0 || match.VisitedPoints > match.TotalPoints
                || match.Completed != (match.VisitedPoints == match.TotalPoints))
                throw new InvalidDataException("Ungültiger Routenverlauf.");
            Text(match.RouteName, 120, "Statistikname"); Text(match.MapVersion, 100, "Statistikversion");
        }
    }
    public static void Map(MapAsset? map)
    {
        if (map is null || map.Id is null || map.Id != MapSpace.WorkspaceId && !Regex.IsMatch(map.Id, "^MAP-[a-f0-9]{64}$"))
            throw new InvalidDataException("Ungültige Karten-ID.");
        Text(map.Name, 120, "Kartenname"); Text(map.Source, 200, "Quelle");
        if (map.SourceUrl is not null && (map.SourceUrl != PublicMapContract.ApiUrl || !map.HasImage || map.RetrievedAtUtc is null))
            throw new InvalidDataException("Ungültige öffentliche Kartenquelle.");
        if (map.Spawns is null || map.Spawns.Count > 10000) throw new InvalidDataException("Maximal 10.000 Orte pro Karte.");
        foreach (var point in map.Spawns)
        {
            if (point is null || !Geometry.IsOnMap(point.Position)) throw new InvalidDataException("Ungültiger Ort.");
            Text(point.Id, 80, "Orts-ID");
            if (point.Source is not null) Text(point.Source, 200, "Ortsquelle");
        }
        if (map.Spawns.Select(p => p.Id).Distinct().Count() != map.Spawns.Count) throw new InvalidDataException("Doppelte Orts-ID.");
    }
}

public static class RouteJson
{
    public const int MaxBytes = 2 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, MaxDepth = 20,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter<RadiusMode>(allowIntegerValues: false) }
    };
    public static byte[] Serialize(LibraryDocument document)
    {
        Validation.Library(document);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(document, Options);
        if (bytes.Length > MaxBytes) throw new InvalidDataException("Datei überschreitet 2 MiB.");
        return bytes;
    }
    public static LibraryDocument Parse(byte[] bytes)
    {
        if (bytes.Length > MaxBytes) throw new InvalidDataException("Datei überschreitet 2 MiB.");
        try
        {
            using var json = JsonDocument.Parse(bytes, new() { MaxDepth = 20 });
            RejectDuplicateKeys(json.RootElement);
            if (!json.RootElement.TryGetProperty("schemaVersion", out var version)
                || !version.TryGetInt32(out int schema) || schema is not (1 or 2 or 3 or 4))
                throw new InvalidDataException("Unbekannte oder fehlende Schemaversion.");
            // Preserve old routes but never turn their simulated history into real activities.
            var node = JsonNode.Parse(bytes)!.AsObject();
            if (schema < 3) node.Remove("demoHistory");
            if (schema < 4) node["schemaVersion"] = 4;
            var doc = node.Deserialize<LibraryDocument>(Options)
                ?? throw new InvalidDataException("Leere Bibliothek.");
            // Required route identity/version must come from the file, never from model defaults.
            foreach (var r in json.RootElement.GetProperty("routes").EnumerateArray())
            {
                foreach (var field in new[] { "id", "name", "mapVersion", "radius", "radiusMode", "points" })
                    if (!r.TryGetProperty(field, out _)) throw new InvalidDataException($"Routenfeld {field} fehlt.");
                foreach (var p in r.GetProperty("points").EnumerateArray())
                    if (!p.TryGetProperty("x", out _) || !p.TryGetProperty("y", out _))
                        throw new InvalidDataException("Wegpunkt benötigt x und y.");
                if (r.TryGetProperty("area", out var selectedArea) && selectedArea.ValueKind != JsonValueKind.Null)
                    foreach (var field in new[] { "left", "top", "right", "bottom" })
                        if (!selectedArea.TryGetProperty(field, out _)) throw new InvalidDataException($"Bereichsfeld {field} fehlt.");
            }
            if (json.RootElement.TryGetProperty("maps", out var maps))
                foreach (var map in maps.EnumerateArray())
                {
                    foreach (var field in new[] { "id", "name", "source", "spawns" })
                        if (!map.TryGetProperty(field, out _)) throw new InvalidDataException($"Kartenfeld {field} fehlt.");
                    foreach (var point in map.GetProperty("spawns").EnumerateArray())
                        if (!point.TryGetProperty("id", out _) || !point.TryGetProperty("position", out var position)
                            || !position.TryGetProperty("x", out _) || !position.TryGetProperty("y", out _))
                            throw new InvalidDataException("Ort benötigt ID und vollständige Position.");
                }
            Validation.Library(doc);
            return doc;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { throw new InvalidDataException("Ungültige Routendatei: " + ex.Message, ex); }
    }
    private static void RejectDuplicateKeys(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> keys = new(StringComparer.Ordinal);
            foreach (var p in element.EnumerateObject())
            {
                if (!keys.Add(p.Name)) throw new InvalidDataException("Doppelter JSON-Schlüssel.");
                RejectDuplicateKeys(p.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var value in element.EnumerateArray()) RejectDuplicateKeys(value);
    }
    public static byte[] ReadBounded(string path)
    {
        using var file = File.OpenRead(path);
        if (file.Length > MaxBytes) throw new InvalidDataException("Datei überschreitet 2 MiB.");
        using var memory = new MemoryStream();
        byte[] buffer = new byte[8192]; int count;
        while ((count = file.Read(buffer)) > 0)
        {
            if (memory.Length + count > MaxBytes) throw new InvalidDataException("Datei wächst über 2 MiB.");
            memory.Write(buffer, 0, count);
        }
        return memory.ToArray();
    }
    public static void WriteAtomic(string path, byte[] bytes)
    {
        string target = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(target)!;
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, ".route-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { file.Write(bytes); file.Flush(flushToDisk: true); }
            File.Move(temporary, target, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

public sealed class LocalStore(string directory)
{
    public string DirectoryPath { get; } = Path.GetFullPath(directory);
    public string LibraryPath { get; } = Path.Combine(Path.GetFullPath(directory), "library.json");
    public LibraryDocument Load() => File.Exists(LibraryPath)
        ? RouteJson.Parse(RouteJson.ReadBounded(LibraryPath)) : new();
    public void Save(LibraryDocument document) => RouteJson.WriteAtomic(LibraryPath, RouteJson.Serialize(document));
    public void Delete() { if (File.Exists(LibraryPath)) File.Delete(LibraryPath); }
}
