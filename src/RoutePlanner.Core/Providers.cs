namespace RoutePlanner.Core;

public enum DemoEventKind { Started, Position, Finished }
public sealed record DemoEvent(DemoEventKind Kind, int ElapsedSeconds, MapPoint? Position);
public interface IOfflineEventProvider
{
    string SourceLabel { get; }
    IReadOnlyList<DemoEvent> CreateEvents(RoutePlan route);
}

public sealed class OfflineDemoProvider : IOfflineEventProvider
{
    public string SourceLabel => "DEMO · simuliert · keine Fortnite-Verbindung";
    public IReadOnlyList<DemoEvent> CreateEvents(RoutePlan route)
    {
        Validation.Route(route);
        if (!DemoData.IsCurrent(route) || route.Points.Count == 0)
            throw new InvalidDataException("Demo benötigt eine Route auf der aktuellen Demo-Karte.");
        List<DemoEvent> events = [new(DemoEventKind.Started, 0, null)];
        for (int i = 0; i < route.Points.Count; i++)
            events.Add(new(DemoEventKind.Position, (i + 1) * 30, route.Points[i]));
        events.Add(new(DemoEventKind.Finished, route.Points.Count * 30 + 10, null));
        return events;
    }
}

public sealed class DemoSession(RoutePlan route)
{
    private readonly RoutePlan snapshot = route.Copy();
    private readonly DateTimeOffset startedAt = DateTimeOffset.UtcNow;
    private DemoEventKind? previous;
    private int elapsed;
    private int stops;
    private MapPoint? lastPosition;
    private double distance;
    public MapPoint? Position { get; private set; }
    public bool Finished => previous == DemoEventKind.Finished;
    public DemoSummary? Apply(DemoEvent next)
    {
        if (Finished || next.ElapsedSeconds < elapsed || next.ElapsedSeconds > 86400)
            throw new InvalidDataException("Ungültige Demo-Ereignisfolge.");
        if (previous is null && next.Kind != DemoEventKind.Started
            || previous is not null && next.Kind == DemoEventKind.Started)
            throw new InvalidDataException("Demo-Start fehlt oder ist doppelt.");
        if (!Enum.IsDefined(next.Kind)) throw new InvalidDataException("Unbekanntes Demo-Ereignis.");
        if (next.Kind == DemoEventKind.Position)
        {
            if (next.Position is not { } p || !Geometry.IsOnMap(p) || stops >= 500)
                throw new InvalidDataException("Ungültige Demo-Position.");
            if (lastPosition is { } last) distance += Geometry.Distance(last, p);
            Position = lastPosition = p; stops++;
        }
        else if (next.Position is not null) throw new InvalidDataException("Unerwartete Position.");
        previous = next.Kind; elapsed = next.ElapsedSeconds;
        return Finished ? new(Guid.NewGuid(), snapshot.Id, snapshot.Name, snapshot.MapVersion,
            startedAt, elapsed, distance, stops) : null;
    }
}
