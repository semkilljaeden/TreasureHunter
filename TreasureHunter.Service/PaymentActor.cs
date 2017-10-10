using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using log4net;
using SteamTrade.TradeOffer;
using TreasureHunter.Common;

namespace TreasureHunter.Service
{
    public class PaymentActor : ReceiveActor
    {
        /// <summary>
        /// The instance of the Logger for the bot.
        /// </summary>
        public static readonly ILog Log = LogManager.GetLogger(typeof(PaymentActor));
        private readonly Dictionary<string, Tuple<IActorRef, PaymentMessage>> _offerDictionary;
        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new PaymentActor());
        }

        public PaymentActor()
        {
            _offerDictionary = new Dictionary<string, Tuple<IActorRef, PaymentMessage>>();
            Receive<PaymentMessage>(msg => Enqueue(msg));
            Receive<Message>(msg => RunCommand(msg));
        }

        private void RunCommand(Message msg)
        {
            switch (msg.Type)
            {
                    case Message.MessageType.ReleaseItem:
                        Tuple<IActorRef, PaymentMessage> tuple;
                        if (_offerDictionary.TryGetValue(msg.MessageText, out tuple))
                        {
                            PaymentMessage pMsg = tuple.Item2;
                            pMsg.IsPaid = true;
                            tuple.Item1.Tell(pMsg);
                        }
                        else
                        {
                            Log.Error("Cannot Find Token from Trade Awaiting Payment List");
                        }
                        break;
            }
        }
        private void Enqueue(PaymentMessage msg)
        {
            _offerDictionary[msg.Token] = new Tuple<IActorRef, PaymentMessage>(Sender, msg);
        }
    }
}
