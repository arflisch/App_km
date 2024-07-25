#if ANDROID
using AndroidX.Core.App;
using Microsoft.Maui.Devices.Sensors;

#endif

using Shiny.Locations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Car_kilometer.NewFolder;
using Car_kilometer.Models;
using AndroidX.Browser.Trusted.Sharing;

namespace Car_kilometer.Services
{
    public partial class MyGpsDelegate : IGpsDelegate
    {
        private Position previousPosition;
        private Summary _summary;
        private SharedDto _sharedDto;
        public MyGpsDelegate()
        {
            // like all other shiny delegates, dependency injection works here
            // treat this as a singleton
            _summary = MauiProgram.ServiceProvider.GetRequiredService<Summary>();
            _sharedDto = MauiProgram.ServiceProvider.GetRequiredService<SharedDto>();
        }
        private readonly object _lock = new object();

        public Task OnReading(GpsReading reading)
        {
            // do something with the reading
            Position currentPosition = reading.Position;

            if (previousPosition != null)
            {
                // Calculer la distance entre les coordonnées actuelles et précédentes
                //double distance = Location.CalculateDistance(previousPosition.Latitude, previousPosition.Longitude, currentPosition, DistanceUnits.Kilometers);
                double distance = previousPosition.GetDistanceTo(currentPosition).TotalMeters;
                double speed = reading.Speed;

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
            return Task.CompletedTask;
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
