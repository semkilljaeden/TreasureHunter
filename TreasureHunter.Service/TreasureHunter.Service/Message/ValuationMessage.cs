using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamTrade;

namespace TreasureHunter.Service.Message
{
    class ValuationMessage
    {
        public List<Schema.Item> MyItemList { get; set; }
        public List<Schema.Item> TheirItemList { get; set; }
    }
}
