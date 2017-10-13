using System;
using TreasureHunter.SteamTrade.TradeOffer;

namespace TreasureHunter.Contract.AkkaMessageObject
{
    public class PaymentMessage
    {
        public Guid TransactionId { get; private set; }
        public double PaidAmmount { get; private set; }
        public string Buyer { get; private set; }
        public PaymentMessage(Guid transactionId, double paidAmmount, string buyer)
        {
            TransactionId = transactionId;
            PaidAmmount = PaidAmmount;
            Buyer = buyer;
        }
    }
}
