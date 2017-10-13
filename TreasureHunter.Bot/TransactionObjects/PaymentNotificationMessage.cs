using System;

namespace TreasureHunter.Bot.TransactionObjects
{
    public class PaymentNotificationMessage
    {
        public TradeOfferTransaction Transaction { get; private set; }

        public PaymentNotificationMessage(TradeOfferTransaction transaction)
        {
            Transaction = transaction;
        }
    }
}
