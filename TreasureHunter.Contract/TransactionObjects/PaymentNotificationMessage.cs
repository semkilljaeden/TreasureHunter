using System;

namespace TreasureHunter.Contract.TransactionObjects
{
    public class PaymentNotificationMessage
    {
        public string TradeOfferId { get; private set; }

        public PaymentNotificationMessage(string id)
        {
            TradeOfferId = id;
        }
    }
}
