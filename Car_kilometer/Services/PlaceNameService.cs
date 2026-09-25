using Microsoft.Extensions.Logging;

namespace Car_kilometer.Services;

/// <summary>Suggests a ride description from the start and end positions, e.g. "Liège – Namur".</summary>
public sealed class PlaceNameService(ILogger<PlaceNameService> logger)
{
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(6);

    public async Task<string?> SuggestDescriptionAsync(LatLon? start, LatLon? end)
    {
        if (start is null && end is null)
            return null;

        try
        {
            var names = await Task.WhenAll(GetPlaceAsync(start), GetPlaceAsync(end)).WaitAsync(Timeout);
            var (from, to) = (names[0], names[1]);

            if (string.IsNullOrEmpty(from))
                return to;
            if (string.IsNullOrEmpty(to) || string.Equals(from, to, StringComparison.CurrentCultureIgnoreCase))
                return from;
            return $"{from} – {to}";
        }
        catch (Exception ex)
        {
            // No network, no geocoder on the device… the user simply types the description.
            logger.LogInformation(ex, "Reverse geocoding failed");
            return null;
        }
    }

    static async Task<string?> GetPlaceAsync(LatLon? point)
    {
        if (point is not { } p)
            return null;

        var placemarks = await Geocoding.Default.GetPlacemarksAsync(p.Latitude, p.Longitude);
        var placemark = placemarks?.FirstOrDefault();
        return placemark?.Locality ?? placemark?.SubAdminArea ?? placemark?.AdminArea;
    }
}
