namespace RoutePlanner.Core;

// Records explicit actions and wall-clock time; does not infer game movement or loot.
public sealed class ManualRouteSession
{
    private readonly RoutePlan route;
    private readonly Func<DateTimeOffset> clock;
    private readonly DateTimeOffset started;
    private bool ended;
    public int VisitedPoints { get; private set; }
    public int TotalPoints => route.Points.Count;
    public int ElapsedSeconds => (int)Math.Clamp((clock()-started).TotalSeconds, 0, 86400);
    public ManualRouteSession(RoutePlan route, Func<DateTimeOffset>? clock = null)
    {
        Validation.Route(route);
        if (route.Points.Count == 0 || MapSpace.IsLegacyExample(route))
            throw new InvalidDataException("Eine eigene Route mit mindestens einem Wegpunkt ist erforderlich.");
        this.route = route.Copy(); this.clock = clock ?? (() => DateTimeOffset.UtcNow); started = this.clock();
    }
    public RouteActivity? Advance()
    {
        if (ended) throw new InvalidDataException("Routengang bereits beendet.");
        VisitedPoints++;
        return VisitedPoints == TotalPoints ? Finish() : null;
    }
    public RouteActivity Finish()
    {
        if (ended) throw new InvalidDataException("Routengang bereits beendet.");
        ended = true;
        return new(Guid.NewGuid(), route.Id, route.Name, route.MapVersion, started, ElapsedSeconds,
            Geometry.Length(route.Points), VisitedPoints, TotalPoints, VisitedPoints == TotalPoints);
    }
}
