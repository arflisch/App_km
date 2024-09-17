using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car_kilometer.NewFolder
{
    public partial class Ride : IRealmObject
    {
        public Ride()
        {
            Id = ObjectId.GenerateNewId();
            Description = string.Empty;
            Distance = 0;
            Duration = 0;
        }

        public Ride(string description, double distance, TimeSpan duration)
        {
            Id = ObjectId.GenerateNewId();
            Description = description;
            Distance = distance;
            Duration = duration.TotalSeconds;
        }

        [PrimaryKey]
        [MapTo("_id")]
        public ObjectId Id { get; set; }
        public string Description { get; set; }
        public double Distance { get; set; }
        public double Duration { get; set; }
    }

}

