
#if ANDROID
using AndroidX.Core.App;
#endif

using Shiny.Locations;
using Car_kilometer.Models;
using Microsoft.Extensions.Logging;

namespace Car_kilometer.Services
{
    public partial class MyGpsDelegate : GpsDelegate
    {
        private Position previousPosition;
        private Summary _summary;
        private SharedDto _sharedDto;
        
        public MyGpsDelegate(ILogger<MyGpsDelegate> logger) : base(logger)
        {
            // like all other shiny delegates, dependency injection works here
            // treat this as a singleton
            _summary = MauiProgram.ServiceProvider.GetRequiredService<Summary>();
            _sharedDto = MauiProgram.ServiceProvider.GetRequiredService<SharedDto>();
            MinimumDistance = Shiny.Distance.FromMeters(10);
            MinimumTime = TimeSpan.FromSeconds(10);
        }
        private readonly object _lock = new object();

        

        protected override async Task OnGpsReading(GpsReading reading)
        {
            // do something with the reading
            Position currentPosition = reading.Position;
           
            if (previousPosition != null)
            {
                // Calculer la distance entre les coordonnées actuelles et précédentes
                //double distance = Location.CalculateDistance(previousPosition.Latitude, previousPosition.Longitude, currentPosition, DistanceUnits.Kilometers);
                int distance = (int)previousPosition.GetDistanceTo(currentPosition).TotalMeters;
                int speed = (int)reading.Speed;

                lock (_lock)
                {
                    // Mettre à jour les valeurs partagées de manière sûre avec lock
                    _sharedDto.Speed = speed;
                    _sharedDto.TotalDistanceDuringRide += distance;
                }
                //await _summary.UpdateDuringRideAsync(currentPosition, speed * 3.6);

                // Faire quelque chose avec la distance (par exemple, mettre à jour votre interface utilisateur)
            }
            previousPosition = currentPosition;
        }
    }

#if ANDROID
    public partial class MyGpsDelegate : Shiny.IAndroidForegroundServiceDelegate
    {
        public void Configure(NotificationCompat.Builder builder)
        {
            builder
               .SetContentTitle("MyApp")
               .SetContentText("My App is following you!! images")
               .SetSmallIcon(Resource.Mipmap.appicon);
        }

    }
#endif
}
