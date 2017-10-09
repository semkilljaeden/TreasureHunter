using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using SteamTrade.TradeOffer;
using TreasureHunter.Common;

namespace TreasureHunter.Service
{
    public class PaymentActor : ReceiveActor
    {
        private readonly Dictionary<string, PaymentMessage> _offerDictionary;
        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new PaymentActor());
        }

        public PaymentActor()
        {
            _offerDictionary = new Dictionary<string, PaymentMessage>();
            Receive<PaymentMessage>(msg => Enqueue(msg));
        }

        private void Enqueue(PaymentMessage msg)
        {
            _offerDictionary[msg.Token] = msg;
        }
    }
}
