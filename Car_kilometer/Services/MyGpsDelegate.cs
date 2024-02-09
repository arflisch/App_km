
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

namespace Car_kilometer.Services
{
    public partial class MyGpsDelegate : IGpsDelegate
    {
        private Position previousPosition;
        private Summary _summary = MauiProgram.ServiceProvider.GetRequiredService<Summary>();
        public MyGpsDelegate()
        {
            // like all other shiny delegates, dependency injection works here
            // treat this as a singleton
        }


        public async Task OnReading(GpsReading reading)
        {
            // do something with the reading
            Position currentPosition = reading.Position;
            double TotalDistance = 0;

            if (previousPosition != null)
            {
                // Calculer la distance entre les coordonnées actuelles et précédentes
                //double distance = Location.CalculateDistance(previousPosition.Latitude, previousPosition.Longitude, currentPosition, DistanceUnits.Kilometers);
                double distance = previousPosition.GetDistanceTo(currentPosition).TotalKilometers;
                TotalDistance += distance;

                double speed = reading.Speed;

                await _summary.UpdateDuringRideAsync(TotalDistance, speed * 3.6);

                // Faire quelque chose avec la distance (par exemple, mettre à jour votre interface utilisateur)
                UpdateDistance(TotalDistance);
            }

            previousPosition = currentPosition;

            
        }

        private void UpdateDistance(double distance)
        {
            // Mettez à jour votre interface utilisateur ou faites autre chose avec la distance
            Trace.WriteLine($"Distance parcourue : {distance} m");
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
