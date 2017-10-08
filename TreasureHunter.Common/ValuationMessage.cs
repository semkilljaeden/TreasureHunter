using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamTrade;

namespace TreasureHunter.Common
{
    public class ValuationMessage
    {
        public List<Schema.Item> MyItemList { get; set; }
        public List<Schema.Item> TheirItemList { get; set; }
        public double Price { get; set; }

    }
}
