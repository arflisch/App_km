using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car_kilometer.NewFolder
{
    class Statistic
    {
        public List<Ride> rides = new List<Ride>();
        public double TotalDistance { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }
}
