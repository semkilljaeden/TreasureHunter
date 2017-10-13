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
            return Sender.Path + "_" + transaction.Id.ToString();
        }
        private TradeOfferTransaction UpdateTradeOffer(TradeOfferTransaction transaction)
        {
            try
            {
                var document = _bucket.GetDocument<List<TradeOfferTransaction>>(GetId(transaction));                
                var transactionList = document?.Content ?? new List<TradeOfferTransaction>();
                transactionList.Add(transaction);
                var doc = new Document<dynamic>
                {
                    Id = GetId(transaction),
                    Content = transactionList,
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
                    var result = _bucket.GetDocument<List<TradeOfferTransaction>>(GetId(transaction));
                    if (result.Success)
                    {
                        resultList = result.Content;
                    }
                    else
                    {
                        Log.Error("Cannot find");
                        return null;
                    }
                }
                else if (transaction.TradeOfferId != null)
                {
                    var result = _bucket.Query<TradeOfferTransaction>($"select i.id, i.offer, i.offerState, i.paidAmmount, i.price, i.state, i.timeStamp, i.tradeOfferId from `TreasureHunter`as list unnest list as i where i.tradeOfferId = '{transaction.TradeOfferId}';");
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
                        DataAccessActionType.Retrieve));
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
