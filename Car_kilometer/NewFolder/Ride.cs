using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car_kilometer.NewFolder
{
    class Ride
    {
        public double Distance { get; set; } 
        public TimeSpan Duration { get; set; }

        public Ride(double distance, TimeSpan duration)
        {
            Distance = distance;
            Duration = duration;
        }
    }
}
