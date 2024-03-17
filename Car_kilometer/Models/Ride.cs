using MongoDB.Bson;

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

