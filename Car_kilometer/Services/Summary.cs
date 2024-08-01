using Android.Util;
using Car_kilometer.Models;
using Car_kilometer.NewFolder;
using MongoDB.Bson;
using Shiny.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car_kilometer.Services
{
    public class Summary
    {
        private  ObjectId id = new ObjectId("65babe06fc322e9b1a2552c3");
        
        public Statistic? Statistic = null;

        private Realm? RealmDB;
        public async Task<Statistic> GetStatisticAsync()
        {
            if (Statistic != null)
            {
                return Statistic;
            }

            await CreateRealmDB();

            var _stat = RealmDB!.Find<Statistic>(id);

            if (_stat == null)
            {
                var stat = new Statistic
                {
                    Id = id,
                    TotalSecondDurations = 0,
                    TotalDistance = 0,
                    TotalRides = 0,
                    Rides = new List<Ride>()
                };
                await CreateAsync(stat);
                Statistic = stat;
            }
            else
            {
                Statistic = _stat;
            }


            return Statistic;
        }

        private async Task CreateRealmDB()
        {
            if (null != RealmDB)
            {
                return;
            }

            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Path.Combine(Environment.GetFolderPath(folder), "my.realm");
            var config = new RealmConfiguration(path)
            {
                SchemaVersion = 2,
                IsReadOnly = false,
                MigrationCallback = (migration, oldSchemaVersion) =>
                {
                    if (oldSchemaVersion < 2)
                    {
                        var oldStatistics = migration.OldRealm.DynamicApi.All("Statistic");
                        var newStatistics = migration.NewRealm.All<Statistic>();

                        for (int i = 0; i < oldStatistics.Count(); i++)
                        {
                            var oldStatistic = oldStatistics.ElementAt(i);
                            var newStatistic = newStatistics.ElementAt(i);

                            // Copy data from old properties to new properties
                            newStatistic.TotalDistance = oldStatistic.TotalDistance;
                            newStatistic.TotalSecondDurations = oldStatistic.TotalSecondDurations;
                            newStatistic.TotalRides = oldStatistic.TotalRides;

                            // Copy Rides from old schema to new schema
                            var oldRides = oldStatistic.Rides;
                            foreach (var oldRide in oldRides)
                            {
                                var newRide = new Ride
                                {
                                    Distance = oldRide.Distance,
                                    Duration = oldRide.Duration
                                };
                                newStatistic.Rides.Add(newRide);
                            }
                        }

                        // Note: Realm doesn't support direct deletion of old schema objects.
                    }
                }
            };

            RealmDB = await Realm.GetInstanceAsync(config).ConfigureAwait(true);
        }


        public async Task UpdateAsync(TimeSpan seconds, double distance, Ride ride)
        {
            var stat = await GetStatisticAsync();

            await CreateRealmDB();

            await RealmDB!.WriteAsync(() =>
            {
                stat.TotalSecondDurations += seconds.TotalSeconds;
                stat.TotalRides += 1;
                stat.TotalDistance += distance;
                stat.Rides.Add(ride);
            });
        }
        /*public async Task UpdateDuringRideAsync(Position position, double speed)
        {
            var stat = await GetStatisticAsync();

            await CreateRealmDB();

            await RealmDB!.WriteAsync(() =>
            {
                stat.Positions.Add(position);
                stat.Speed = speed;
            });
        }*/
        private async Task CreateAsync(Statistic statistic)
        {
            await CreateRealmDB();

            await RealmDB!.WriteAsync(() =>
            {
                RealmDB.Add(statistic);
            });
        }
    }


}
