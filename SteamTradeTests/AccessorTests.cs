using NUnit.Framework;
using TreasureHunter.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using Akka.Actor;
using Akka.TestKit.NUnit3;
using Newtonsoft.Json;
using TreasureHunter.Contract.AkkaMessageObject;
using TreasureHunter.Bot.TransactionObjects;

namespace TreasureHunter.DataAccess.Tests
{
    [TestFixture()]
    public class AccessorTests : TestKit
    {
        [Test()]
        public void UpdateTest()
        {
            var dataAccessor = Sys.ActorOf(DataAccessActor.Props(new List<IActorRef> {TestActor}), "data");
            var json = File.ReadAllText("json/tradeOfferTransaction.json");
            var transaction = JsonConvert.DeserializeObject<TradeOfferTransaction>(json);            
            dataAccessor.Tell(new DataAccessMessage<TradeOfferTransaction>(transaction, DataAccessActionType.UpdateTradeOffer));
            ExpectMsg<DataAccessMessage<TradeOfferTransaction>>(TimeSpan.FromSeconds(50));
        }

        [Test()]
        public void RetrieveTest()
        {
            var dataAccessor = Sys.ActorOf(DataAccessActor.Props(new List<IActorRef> { TestActor }), "data");
            var json = File.ReadAllText("json/tradeOfferTransaction.json");
            var transaction = JsonConvert.DeserializeObject<TradeOfferTransaction>(json);
            transaction = new TradeOfferTransaction(transaction.TradeOfferId);
            var ob = dataAccessor.Ask(new DataAccessMessage<TradeOfferTransaction>(transaction, DataAccessActionType.Retrieve)).Result;
        }
    }
}