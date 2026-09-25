using Car_kilometer.Resources;

namespace Car_kilometer.Models;

/// <summary>Weather picked when a ride is saved. The names are what is stored in <see cref="Ride.WeatherCondition"/>.</summary>
public enum Weather
{
    Unknown,
    Sunny,
    Cloudy,
    Rainy,
    Windy,
    Snowy,
    Night,
}

public static class WeatherInfo
{
    public static IReadOnlyList<Weather> Choices { get; } =
        [Weather.Sunny, Weather.Cloudy, Weather.Rainy, Weather.Windy, Weather.Snowy, Weather.Night];

    public static Weather Parse(string? stored) =>
        Enum.TryParse<Weather>(stored, ignoreCase: true, out var weather) && Enum.IsDefined(weather) ? weather : Weather.Unknown;

    public static string ToStored(Weather weather) => weather == Weather.Unknown ? string.Empty : weather.ToString();

    public static string Label(Weather weather) => weather switch
    {
        Weather.Sunny => AppResources.WeatherSunny,
        Weather.Cloudy => AppResources.WeatherCloudy,
        Weather.Rainy => AppResources.WeatherRainy,
        Weather.Windy => AppResources.WeatherWindy,
        Weather.Snowy => AppResources.WeatherSnowy,
        Weather.Night => AppResources.WeatherNight,
        _ => "–",
    };

    public static string Glyph(Weather weather) => weather switch
    {
        Weather.Sunny => Icons.Sunny,
        Weather.Cloudy => Icons.Cloud,
        Weather.Rainy => Icons.Rainy,
        Weather.Windy => Icons.Air,
        Weather.Snowy => Icons.Snow,
        Weather.Night => Icons.Night,
        _ => Icons.Route,
    };
}
