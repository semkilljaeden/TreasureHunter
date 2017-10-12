using System.Collections.Generic;
using TreasureHunter.SteamTrade;

namespace TreasureHunter.Contract.AkkaMessageObject
{
    public class ValuationMessage
    {
        public List<Schema.Item> MyItemList { get; set; }
        public List<Schema.Item> TheirItemList { get; set; }
        public double Price { get; set; }

    }
}
