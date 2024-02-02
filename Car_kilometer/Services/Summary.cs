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
        public async Task<Statistic> GetStatisticAsync() 
        {
            if (Statistic != null) return Statistic;

            var realm = await Realm.GetInstanceAsync();

            var _stat = realm.Find<Statistic>(id);

            if (_stat == null)
            {
                var stat = new Statistic();
                stat.Id = id;
                stat.TotalSecondDurations = 0;
                stat.TotalRides = 0;
                await CreateAsync(stat);
                Statistic = stat;
            }
            else
            {
                Statistic = _stat;
            }

            return Statistic;
        }

        public async Task UpdateAsync(TimeSpan seconds)
        {
            var stat = await GetStatisticAsync();

            var realm = await Realm.GetInstanceAsync();

            await realm.WriteAsync(() =>
            {
                stat.TotalSecondDurations += seconds.TotalSeconds;
                stat.TotalRides += 1;
            });
        }
        private async Task CreateAsync(Statistic statistic)
        {
            var realm = await Realm.GetInstanceAsync();

            await realm.WriteAsync(() =>
            {
                realm.Add(statistic);
            });
        }
    }


}
