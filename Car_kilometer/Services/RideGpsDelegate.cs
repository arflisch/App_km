using Shiny.Locations;

namespace Car_kilometer.Services;

/// <summary>Receives the GPS readings from Shiny, also while the app is in the background, and hands them to the tracker.</summary>
public partial class RideGpsDelegate(RideTracker tracker) : IGpsDelegate
{
    public Task OnReading(GpsReading reading)
    {
        tracker.OnReading(reading);
        return Task.CompletedTask;
    }
}

#if ANDROID
// The notification Android shows while the ride is recorded in the background.
public partial class RideGpsDelegate : Shiny.IAndroidForegroundServiceDelegate
{
    public void Configure(AndroidX.Core.App.NotificationCompat.Builder builder)
    {
        builder.SetContentTitle(AppResources.NotificationTitle);
        builder.SetContentText(AppResources.NotificationText);
        builder.SetSmallIcon(Resource.Drawable.notification);
        builder.SetColor(Android.Graphics.Color.ParseColor("#4F46E5"));
    }
}
#endif
