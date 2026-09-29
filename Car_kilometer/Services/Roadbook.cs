namespace Car_kilometer.Services;

/// <summary>How far the learner driver is on the way to the kilometres required before the practical exam.</summary>
public sealed record RoadbookProgress(IReadOnlyList<RideItem> Rides, double Km, double GoalKm, TimeSpan Time, DateTime Start)
{
    public double Ratio => GoalKm > 0 ? Math.Clamp(Km / GoalKm, 0, 1) : 0;
    public bool IsComplete => Km >= GoalKm;
    public double RemainingKm => Math.Max(0, GoalKm - Km);
}

public static class Roadbook
{
    /// <summary>The rides that count for the roadbook (from its start date on), oldest first as in the paper roadbook.</summary>
    public static RoadbookProgress Progress(IEnumerable<RideItem> rides, UserSettings settings)
    {
        var start = settings.RoadbookStart.Date;
        var counted = rides
            .Where(ride => ride.LocalDate.Date >= start)
            .OrderBy(ride => ride.Date)
            .ToArray();

        return new RoadbookProgress(
            counted,
            counted.Sum(ride => ride.DistanceKm),
            settings.RoadbookGoalKm,
            TimeSpan.FromTicks(counted.Sum(ride => ride.Duration.Ticks)),
            start);
    }
}
