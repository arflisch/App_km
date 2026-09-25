namespace Car_kilometer.Models;

/// <summary>A finished recording that has not been saved yet.</summary>
public sealed record RideDraft(
    double DistanceKm,
    TimeSpan Duration,
    DateTimeOffset StartedAt,
    LatLon? Start = null,
    LatLon? End = null);

public readonly record struct LatLon(double Latitude, double Longitude);
