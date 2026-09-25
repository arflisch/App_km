namespace Car_kilometer.Models;

/// <summary>
/// Root object of the database: a single instance that owns every ride.
/// The class and property names are the database schema: renaming them would lose the existing rides.
/// </summary>
public partial class Statistic : IRealmObject
{
    [PrimaryKey]
    [MapTo("_id")]
    public ObjectId Id { get; set; }
    public double TotalDistance { get; set; }
    public double TotalSecondDurations { get; set; }
    public int TotalRides { get; set; }
    public IList<Ride> Rides { get; }
}
