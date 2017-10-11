using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using log4net;
using TreasureHunter.Common;
using TreasureHunter.SteamTrade.TradeOffer;

namespace TreasureHunter.Service
{
    class ValuationActor : ReceiveActor
    {
        /// <summary>
        /// The instance of the Logger for the bot.
        /// </summary>
        public static readonly ILog Log = LogManager.GetLogger(typeof(ValuationActor));
        private Dictionary<string, TradeOffer> _offerDictionary;
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
            msg.Price = 66666;
            Sender.Tell(msg);
        }
    }
}
