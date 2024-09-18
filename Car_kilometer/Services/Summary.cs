using Android.Util;
using Car_kilometer.Models;
using Car_kilometer.NewFolder;
using MongoDB.Bson;
using Shiny.Locations;
using Shiny.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car_kilometer.Services
{
    public class Summary
    {
        private ObjectId id = new ObjectId("65babe06fc322e9b1a2552c3");
        
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
                    TotalRides = 0
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
                SchemaVersion = 9,
                IsReadOnly = false,
                MigrationCallback = (migration, oldSchemaVersion) =>
                {
                    if (oldSchemaVersion < 9)
                    {
                        var oldStatistics = migration.OldRealm.DynamicApi.All("Statistic");
                        var newStatistics = migration.NewRealm.All<Statistic>();

                        for (int i = 0; i < oldStatistics.Count(); i++)
                        {
                            var oldStatistic = oldStatistics.ElementAt(i);
                            var newStatistic = newStatistics.ElementAt(i);

                            // Mise à jour des propriétés existantes
                            newStatistic.TotalDistance = oldStatistic.DynamicApi.Get<double>("TotalDistance");
                            newStatistic.TotalSecondDurations = oldStatistic.DynamicApi.Get<double>("TotalSecondDurations");
                            newStatistic.TotalRides = oldStatistic.DynamicApi.Get<int>("TotalRides");

                            // Migration des rides
                            var oldRides = oldStatistic.DynamicApi.GetList<IRealmObjectBase>("Rides");

                            // Migration des rides
                            foreach (var oldRide in oldRides)
                            {
                                // Extraire manuellement les propriétés nécessaires
                                var distance = oldRide.DynamicApi.Get<double>("Distance");
                                var duration = oldRide.DynamicApi.Get<double>("Duration");

                                // Créer un nouvel objet Ride dans le nouveau schéma
                                var newRide = new Ride
                                {
                                    Distance = distance,
                                    Duration = duration,
                                    Description = "Default Description",
                                    Date = DateTime.UtcNow    // Utilisation d'une valeur par défaut
                                };
                                newStatistic.Rides.Add(newRide);
                            }
                        }
                    }
                }
            };


            RealmDB = await Realm.GetInstanceAsync(config).ConfigureAwait(false);
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
