using Car_kilometer.Resources;

namespace Car_kilometer.Models;

/// <summary>Roadbook: the kinds of road driven during a ride. Stored as flags in <see cref="Ride.RoadTypes"/>.</summary>
[Flags]
public enum RoadType
{
    None = 0,
    City = 1,
    Road = 2,
    Motorway = 4,
}

public static class RoadTypeInfo
{
    public static IReadOnlyList<RoadType> Choices { get; } = [RoadType.City, RoadType.Road, RoadType.Motorway];

    public static string Label(RoadType type) => type switch
    {
        RoadType.City => AppResources.RoadCity,
        RoadType.Road => AppResources.RoadRoad,
        RoadType.Motorway => AppResources.RoadMotorway,
        _ => string.Empty,
    };

    public static string Glyph(RoadType type) => type switch
    {
        RoadType.City => Icons.City,
        RoadType.Road => Icons.Countryside,
        _ => Icons.Motorway,
    };

    /// <summary>"City, Motorway"</summary>
    public static string Describe(RoadType types) =>
        string.Join(", ", Choices.Where(choice => types.HasFlag(choice)).Select(Label));
}
