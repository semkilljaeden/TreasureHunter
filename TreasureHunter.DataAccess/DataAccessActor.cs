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
using TreasureHunter.Bot.TransactionObjects;

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

        private TradeOfferTransaction UpdateTradeOffer(TradeOfferTransaction transaction)
        {
            try
            {           
                var doc = new Document<dynamic>
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = transaction,
                };
                var r = _bucket.Upsert(doc);
                if (r.Success)
                {
                    Log.Info("Trade Offer Persisted");
                }
                else
                {
                    Log.Error("Error in persisting TradeOffer");
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            return transaction;
        }

        private TradeOfferTransaction Retrieve(TradeOfferTransaction transaction)
        {
            List<TradeOfferTransaction> resultList = null;
            try
            {
                if (transaction.Id != Guid.Empty)
                {
                    var result =
                        _bucket.Query<TradeOfferTransaction>(
                            $"select id, offer, offerState, paidAmmount, price, state, timeStamp, tradeOfferId, buyer, botPath from `TreasureHunter` where id = '{transaction.Id}';");
                    if (result.Success)
                    {
                        resultList = result.Rows;
                    }
                    else
                    {
                        Log.Error("Cannot find");
                        return null;
                    }
                }
                else if (transaction.TradeOfferId != null)
                {
                    var result = _bucket.Query<TradeOfferTransaction>($"select id, offer, offerState, paidAmmount, price, state, timeStamp, tradeOfferId, buyer， botPath from `TreasureHunter` where tradeOfferId = '{transaction.TradeOfferId}';");
                    if (result.Success)
                    {
                        resultList = result.Rows;
                    }
                    else
                    {
                        Log.Error("Cannot find");
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e); ;
                return null;
            }
            return resultList?.OrderBy(t => t.TimeStamp)?.LastOrDefault();
        }

        private void PersistTradeOffer(DataAccessMessage<TradeOfferTransaction> doc)
        {
            switch (doc.ActionType)
            {
                case DataAccessActionType.UpdateTradeOffer:
                    Sender.Tell(new DataAccessMessage<TradeOfferTransaction>(UpdateTradeOffer(doc.Content),
                        DataAccessActionType.UpdateTradeOffer));
                    break;
                case DataAccessActionType.Retrieve:
                    Sender.Tell(new DataAccessMessage<TradeOfferTransaction>(Retrieve(doc.Content),
                        DataAccessActionType.Retrieve));
                    break;
            }
        }

        public static Props Props(List<IActorRef> routees)
        {
            return Akka.Actor.Props.Create(() => new DataAccessActor(routees));
        }
    }
}
