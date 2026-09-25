namespace Car_kilometer.Models;

/// <summary>
/// Immutable copy of a <see cref="Ride"/>. Unlike Realm objects it can be used on any thread,
/// which lets the pages and the PDF report work without touching the database.
/// </summary>
public sealed record RideItem(
    ObjectId Id,
    string Description,
    double DistanceKm,
    TimeSpan Duration,
    DateTimeOffset Date,
    string WeatherCondition)
{
    public static RideItem From(Ride ride) => new(
        ride.Id,
        ride.Description ?? string.Empty,
        ride.Distance,
        TimeSpan.FromSeconds(ride.Duration),
        ride.Date,
        ride.WeatherCondition ?? string.Empty);

    public DateTime LocalDate => Date.LocalDateTime;

    public Weather Weather => WeatherInfo.Parse(WeatherCondition);

    public double AverageSpeedKmh => Duration.TotalHours > 0 ? DistanceKm / Duration.TotalHours : 0;

    // Display values bound by the ride cards
    public string DisplayDescription => string.IsNullOrWhiteSpace(Description) ? AppResources.NoDescription : Description;
    public string DistanceText => Format.Km(DistanceKm);
    public string DetailsText => $"{Format.ShortDate(LocalDate)} · {Format.Time(LocalDate)} · {Format.Duration(Duration)}";
    public string SpeedText => Format.Speed(AverageSpeedKmh);
    public string WeatherGlyph => WeatherInfo.Glyph(Weather);
}
