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
        public async Task<Statistic?> GetStatisticAsync() 
        {
            var realm = await Realm.GetInstanceAsync();
            
            return realm.Find<Statistic>(id);
        }

        public async Task UpdateAsync(TimeSpan seconds)
        {
            var stat = await GetStatisticAsync();

            if (null == stat)
            {
                stat = new Statistic();
                stat.Id = id;
                stat.TotalSecondDurations = seconds.TotalSeconds;
                stat.TotalRides = 1;
                await CreateAsync(stat);
                return;
            }

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
