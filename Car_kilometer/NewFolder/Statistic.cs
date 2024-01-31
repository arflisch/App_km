using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car_kilometer.NewFolder
{
    public partial class Statistic : IRealmObject
    {
        [PrimaryKey]
        [MapTo("_id")]
        public ObjectId Id { get; set; }
        public double TotalDistance { get; set; }
        public double TotalSecondDurations { get; set; }
        public int TotalRides { get; set; }
    }
}
