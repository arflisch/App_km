namespace Car_kilometer.Models;

/// <summary>
/// A recorded ride, as stored in Realm.
/// The class and property names are the database schema: renaming them would lose the existing rides.
/// </summary>
public partial class Ride : IRealmObject
{
    public Ride()
    {
        Id = ObjectId.GenerateNewId();
        Description = string.Empty;
        Date = DateTimeOffset.Now;
        WeatherCondition = string.Empty;
    }

    public Ride(string description, double distance, TimeSpan duration, DateTimeOffset date, string weatherCondition)
    {
        Id = ObjectId.GenerateNewId();
        Description = description;
        Distance = distance;
        Duration = duration.TotalSeconds;
        Date = date;
        WeatherCondition = weatherCondition;
    }

    [PrimaryKey]
    [MapTo("_id")]
    public ObjectId Id { get; set; }

    public string Description { get; set; }

    /// <summary>Distance in kilometers.</summary>
    public double Distance { get; set; }

    /// <summary>Duration in seconds.</summary>
    public double Duration { get; set; }

    public DateTimeOffset Date { get; set; }

    /// <summary>One of the <see cref="Weather"/> names ("Sunny", "Cloudy"…), or empty.</summary>
    public string WeatherCondition { get; set; }
}
