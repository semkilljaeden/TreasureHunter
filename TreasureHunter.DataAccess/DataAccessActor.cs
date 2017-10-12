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
using TreasureHunter.Contract.AkkaMessageObject;
using TreasureHunter.Contract.TransactionObjects;

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
            Receive<DataAccessMessage<TradeOfferTransaction>>(msg => PersistTradeOffer(msg));
        }

        private string GetId(TradeOfferTransaction transaction)
        {
            return Sender.Path + "_" + transaction.Id;
        }
        private void UpdateTradeOffer(TradeOfferTransaction transaction)
        {
            var result = _bucket.GetDocument<Document<List<TradeOfferTransaction>>>(GetId(transaction));
            var transactionList = result.Content?.Content ?? new List<TradeOfferTransaction>();
            transactionList.Add(transaction);
            var doc = new Document<dynamic>
            {
                Id = GetId(transaction),
                Content = transactionList,
            };
            var r = _bucket.Insert(doc);
            if (r.Success)
            {
                Log.Info("Trade Offer Persisted");
            }
            else
            {
                Log.Error("Error in persisting TradeOffer");
            }
        }

        private TradeOfferTransaction Retrieve(TradeOfferTransaction transaction)
        {
            if (transaction.Id != null)
            {
                var result = _bucket.Query<Document<List<TradeOfferTransaction>>>($"select * from `TreasureHunter` where ttradeOfferId = '{transaction.Id}'");
                if (result.Success)
                {
                    return result.GetEnumerator().Current?.Content.Last();
                }
                else
                {
                    Log.Error("Cannot find");
                    return null;
                }
            }
            return null;
        }

        private void PersistTradeOffer(DataAccessMessage<TradeOfferTransaction> doc)
        {
            switch (doc.ActionType)
            {
                case DataAccessActionType.UpdateTradeOffer:
                    UpdateTradeOffer(doc.Content);
                    break;
                case DataAccessActionType.Retrieve:
                    Sender.Tell(new DataAccessMessage<TradeOfferTransaction>(Retrieve(doc.Content),
                        DataAccessActionType.Retrieve));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Props Props(List<IActorRef> routees)
        {
            return Akka.Actor.Props.Create(() => new DataAccessActor(routees));
        }
    }
}
