using System;
using System.Collections.Generic;
using Akka.Actor;
using log4net;
using TreasureHunter.Contract.AkkaMessageObject;

namespace TreasureHunter.Transaction
{
    public class PaymentActor : ReceiveActor
    {
        /// <summary>
        /// The instance of the Logger for the bot.
        /// </summary>
        public static readonly ILog Log = LogManager.GetLogger(typeof(PaymentActor));
        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new PaymentActor());
        }

        public PaymentActor()
        {
            Receive<PaymentMessage>(msg => Enqueue(msg));
            Receive<CommandMessage>(msg => RunCommand(msg));
        }

        private void RunCommand(CommandMessage msg)
        {
        }
        private void Enqueue(PaymentMessage msg)
        {
        }
    }
}
