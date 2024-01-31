using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car_kilometer.NewFolder
{
    partial class Ride : IRealmObject
    {
        public Ride(double distance, TimeSpan duration)
        {
            Id = ObjectId.GenerateNewId();
            Distance = distance;
            Duration = duration.TotalSeconds;
        }
        [PrimaryKey]
        [MapTo("_id")]
        public ObjectId Id { get; set; } 
        public double Distance { get; set; } 
        public double Duration { get; set; } 
    }
}
