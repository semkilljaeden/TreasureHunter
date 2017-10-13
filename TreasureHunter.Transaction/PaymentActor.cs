using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using log4net;
using TreasureHunter.Contract.AkkaMessageObject;
using TreasureHunter.Bot.TransactionObjects;

namespace TreasureHunter.Transaction
{
    public class PaymentActor : ReceiveActor
    {
        /// <summary>
        /// The instance of the Logger for the bot.
        /// </summary>
        public static readonly ILog Log = LogManager.GetLogger(typeof(PaymentActor));
        private readonly List<IActorRef> _bots;
        private readonly IActorRef _dataAccess;
        public static Props Props(IActorRef dataAccess, List<IActorRef> bots)
        {
            return Akka.Actor.Props.Create(() => new PaymentActor(dataAccess, bots));
        }

        public PaymentActor(IActorRef dataAccess, List<IActorRef> bots)
        {
            _dataAccess = dataAccess;
            _bots = bots;
            Receive<PaymentMessage>(msg => Pay(msg));
        }

        private void Pay(PaymentMessage msg)
        {
            var transaction = new TradeOfferTransaction(msg.TransactionId);
            try
            {
                var doc = _dataAccess.Ask<DataAccessMessage<TradeOfferTransaction>>(new DataAccessMessage<TradeOfferTransaction>(transaction, DataAccessActionType.Retrieve)).Result;
                transaction = doc.Content;
                if (transaction != null)
                {
                    transaction = new TradeOfferTransaction(transaction, msg);
                }
                else
                {
                    transaction = new TradeOfferTransaction(msg);
                    Log.Warn($"No transaction id = {msg.TransactionId} found in the Database! Needs support");
                }
                var result = _dataAccess.Ask<DataAccessMessage<TradeOfferTransaction>>(new DataAccessMessage<TradeOfferTransaction>(transaction, DataAccessActionType.UpdateTradeOffer));
                if (transaction.State == TradeOfferTransactionState.Completed)
                {
                    Log.Info($"Transaction {transaction.Id} has been completed already, user paid extra ammount Price = {transaction.Price}, Paid ammount = {transaction.PaidAmmount}");
                }
                _bots.FirstOrDefault(b => b.Path.Name == transaction.BotPath)?.Tell(new PaymentNotificationMessage(transaction));
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

        }
    }
}
