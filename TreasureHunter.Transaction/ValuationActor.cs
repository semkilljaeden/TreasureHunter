using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using log4net;
using TreasureHunter.Contract.AkkaMessageObject;
using TreasureHunter.SteamTrade.TradeOffer;

namespace TreasureHunter.Transaction
{
    public class ValuationActor : ReceiveActor
    {
        /// <summary>
        /// The instance of the Logger for the bot.
        /// </summary>
        public static readonly ILog Log = LogManager.GetLogger(typeof(ValuationActor));
        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new ValuationActor());
        }

        public ValuationActor()
        {
            Receive<ValuationMessage>(msg => Valuate(msg));
        }

        private void Valuate(ValuationMessage msg)
        {
            var theirItemValue = msg.TheirItemList.Count() * 100;
            var myItemValue = msg.MyItemList.Count() * 100;
            msg.Price = myItemValue - theirItemValue;
            Sender.Tell(msg);
        }
    }
}
