using TreasureHunter.SteamTrade.TradeOffer;

namespace TreasureHunter.Contract.AkkaMessageObject
{
    public class PaymentMessage
    {
        public string Token { get; set; }
        public TradeOffer Offer { get; set; }

        public double Price { get; set; }
        public double PaidAmmount { get; set; } = 0;
        public bool IsPaid = false;
    }
}
