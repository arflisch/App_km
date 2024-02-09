using Android.Util;
using Car_kilometer.NewFolder;
using MongoDB.Bson;
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
            if (Statistic != null) return Statistic;

            await CreateRealmDB();

            var _stat = RealmDB!.Find<Statistic>(id);

            if (_stat == null)
            {
                var stat = new Statistic
                {
                    Id = id,
                    TotalSecondDurations = 0,
                    TotalRides = 0,
                    Speed = 0,
                    TotalDistanceDuringRide = 0
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
                SchemaVersion = 1,
                IsReadOnly = false,

            };

            RealmDB = await Realm.GetInstanceAsync(config).ConfigureAwait(true);
        }

        public async Task UpdateAsync(TimeSpan seconds, double distance)
        {
            var stat = await GetStatisticAsync();

            await CreateRealmDB();

            await RealmDB!.WriteAsync(() =>
            {
                stat.TotalSecondDurations += seconds.TotalSeconds;
                stat.TotalRides += 1;
                stat.TotalDistance += distance;
                stat.TotalDistanceDuringRide = 0;
                stat.Speed = 0;
            });
        }
        public async Task UpdateDuringRideAsync(double distance, double speed)
        {
            var stat = await GetStatisticAsync();

            await CreateRealmDB();

            await RealmDB!.WriteAsync(() =>
            {
                stat.TotalDistanceDuringRide += distance;
                stat.Speed = speed;
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
