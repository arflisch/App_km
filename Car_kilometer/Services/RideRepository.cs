namespace Car_kilometer.Services;

public enum RideChange { Added, Updated, Deleted }

public sealed class RideChangedEventArgs(RideChange change, ObjectId id, RideItem? ride) : EventArgs
{
    public RideChange Change { get; } = change;
    public ObjectId Id { get; } = id;

    /// <summary>The ride after the change (null when it was deleted).</summary>
    public RideItem? Ride { get; } = ride;
}

/// <summary>
/// Stores the rides in the Realm database.
/// Every operation opens a short-lived Realm on a background thread, so the UI thread never waits on the disk,
/// and callers only ever get immutable <see cref="RideItem"/> copies. The list is cached in memory after the first load.
/// </summary>
public sealed class RideRepository
{
    // The single Statistic object that owns every ride (id kept from the first version of the app).
    static readonly ObjectId StatisticId = new("65babe06fc322e9b1a2552c3");

    readonly RealmConfiguration _config;
    readonly SemaphoreSlim _gate = new(1, 1);
    IReadOnlyList<RideItem>? _rides;

    public RideRepository()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _config = new RealmConfiguration(Path.Combine(folder, "my.realm"))
        {
            // 13: Ride.Guide and Ride.RoadTypes (added automatically by Realm, nothing to migrate)
            SchemaVersion = 13,
            MigrationCallback = MigrateFromOldSchemas,
        };
    }

    /// <summary>Raised on the main thread after a ride was added, edited or deleted.</summary>
    public event EventHandler<RideChangedEventArgs>? Changed;

    /// <summary>All the rides, newest first.</summary>
    public async Task<IReadOnlyList<RideItem>> GetRidesAsync()
    {
        if (_rides is { } cached)
            return cached;

        return await RunAsync(realm => _rides ??= Snapshot(realm));
    }

    public async Task<RideItem> AddAsync(Ride ride)
    {
        var added = RideItem.From(ride);
        await RunAsync(realm =>
        {
            var statistic = GetOrCreateStatistic(realm);
            realm.Write(() =>
            {
                statistic.Rides.Add(ride);
                statistic.TotalRides += 1;
                statistic.TotalDistance += ride.Distance;
                statistic.TotalSecondDurations += ride.Duration;
            });
            _rides = Snapshot(realm);
            return true;
        });
        RaiseChanged(new RideChangedEventArgs(RideChange.Added, added.Id, added));
        return added;
    }

    public async Task UpdateAsync(ObjectId id, string description, string weatherCondition, string guide, RoadType roadTypes)
    {
        var updated = await RunAsync(realm =>
        {
            var ride = realm.Find<Ride>(id);
            if (ride is null)
                return null;

            realm.Write(() =>
            {
                ride.Description = description;
                ride.WeatherCondition = weatherCondition;
                ride.Guide = guide;
                ride.RoadTypes = (int)roadTypes;
            });
            _rides = Snapshot(realm);
            return RideItem.From(ride);
        });

        if (updated is not null)
            RaiseChanged(new RideChangedEventArgs(RideChange.Updated, id, updated));
    }

    public async Task DeleteAsync(ObjectId id)
    {
        var deleted = await RunAsync(realm =>
        {
            var ride = realm.Find<Ride>(id);
            if (ride is null)
                return false;

            var statistic = GetOrCreateStatistic(realm);
            realm.Write(() =>
            {
                statistic.TotalRides = Math.Max(0, statistic.TotalRides - 1);
                statistic.TotalDistance = Math.Max(0, statistic.TotalDistance - ride.Distance);
                statistic.TotalSecondDurations = Math.Max(0, statistic.TotalSecondDurations - ride.Duration);
                realm.Remove(ride); // also removes it from statistic.Rides
            });
            _rides = Snapshot(realm);
            return true;
        });

        if (deleted)
            RaiseChanged(new RideChangedEventArgs(RideChange.Deleted, id, null));
    }

    Task<T> RunAsync<T>(Func<Realm, T> work) => Task.Run(async () =>
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            using var realm = Realm.GetInstance(_config);
            return work(realm);
        }
        finally
        {
            _gate.Release();
        }
    });

    void RaiseChanged(RideChangedEventArgs args) =>
        MainThread.BeginInvokeOnMainThread(() => Changed?.Invoke(this, args));

    static IReadOnlyList<RideItem> Snapshot(Realm realm) =>
        GetOrCreateStatistic(realm).Rides
            .AsEnumerable()
            .Select(RideItem.From)
            .OrderByDescending(ride => ride.Date)
            .ToArray();

    static Statistic GetOrCreateStatistic(Realm realm) =>
        realm.Find<Statistic>(StatisticId)
        ?? realm.Write(() => realm.Add(new Statistic { Id = StatisticId }));

    /// <summary>Migration of databases created by the very first versions of the app (schema &lt; 12).</summary>
    static void MigrateFromOldSchemas(Migration migration, ulong oldSchemaVersion)
    {
        if (oldSchemaVersion >= 12)
            return;

        var oldStatistics = migration.OldRealm.DynamicApi.All("Statistic");
        var newStatistics = migration.NewRealm.All<Statistic>();

        for (int i = 0; i < oldStatistics.Count(); i++)
        {
            var oldStatistic = oldStatistics.ElementAt(i);
            var newStatistic = newStatistics.ElementAt(i);

            newStatistic.TotalDistance = oldStatistic.DynamicApi.Get<double>("TotalDistance");
            newStatistic.TotalSecondDurations = oldStatistic.DynamicApi.Get<double>("TotalSecondDurations");
            newStatistic.TotalRides = oldStatistic.DynamicApi.Get<int>("TotalRides");

            foreach (var oldRide in oldStatistic.DynamicApi.GetList<IRealmObjectBase>("Rides"))
            {
                newStatistic.Rides.Add(new Ride
                {
                    Distance = oldRide.DynamicApi.Get<double>("Distance"),
                    Duration = oldRide.DynamicApi.Get<double>("Duration"),
                    Description = "Default Description",
                    Date = DateTime.UtcNow,
                });
            }
        }
    }
}
