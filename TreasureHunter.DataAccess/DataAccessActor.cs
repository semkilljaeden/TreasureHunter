using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Client.Providers;
using Couchbase.Core;
using Newtonsoft.Json;
using TreasureHunter;
using TreasureHunter.SteamTrade.TradeOffer;
using log4net;

namespace TreasureHunter.DataAccess
{
    public class DataAccessActor : ReceiveActor
    {
        private readonly List<IActorRef> _routees;
        private readonly IBucket _bucket;
        private static readonly ILog Log = LogManager.GetLogger(typeof(DataAccessActor));
        public DataAccessActor(List<IActorRef> routees)
        {
            _routees = routees;
            var section = ConfigurationManager.GetSection("Couchbase");
            ClusterHelper.Initialize(new ClientConfiguration((CouchbaseClientSection)section));
            _bucket = ClusterHelper.GetBucket("TreasureHunter");
            Receive<TradeOffer>(msg => PersistTradeOffer(msg));
        }
        private void PersistTradeOffer(TradeOffer trade)
        {
            var json = JsonConvert.SerializeObject(trade);
            var content = new Content()
            {
                BotPath = Sender.Path,
                ContentType = typeof(TradeOffer),
                Object = json,
                Id = trade.TradeOfferId
            };
            var doc = new Document<dynamic>
            {
                Id = content.GetDocumentId(),
                Content = content
            };
            var result = _bucket.Insert(doc);
            if (result.Success)
            {
                Log.Info("Trade Offer Persisted");
            }
            else
            {
                Log.Error("Error in persisting TradeOffer");
            }
        }
        public static Props Props(List<IActorRef> routees)
        {
            return Akka.Actor.Props.Create(() => new DataAccessActor(routees));
        }
    }
}
