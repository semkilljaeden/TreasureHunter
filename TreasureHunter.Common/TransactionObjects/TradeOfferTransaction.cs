using System;
using TreasureHunter.SteamTrade.TradeOffer;

namespace TreasureHunter.Common.TransactionObjects
{ 
    public enum TradeOfferTransactionState
    {
        New,
        Paid,
        PartialPaid,
        UnPaid,
        Expired,
        Completed,
        Declined
    }
    public class TradeOfferTransaction
    {
        public Guid Id { get; private set; }
        public TradeOffer Offer { get; private set; }
        public TradeOfferTransactionState State { get; private set; }
        public TradeOfferState OfferState { get; private set; }
        public double Price { get; private set; }
        public double PaidAmmount { get; private set; }
        public TradeOfferTransaction(TradeOffer offer, TradeOfferTransactionState state, double price)
        {
            OfferState = offer.OfferState;
            Offer = offer;
            State = state;
            Price = price;
            PaidAmmount = 0.0;
            Id = Guid.NewGuid();
        }

        public TradeOfferTransaction(TradeOfferTransaction transaction, TradeOfferTransactionState state, double paid) :
            this(transaction.Offer, transaction.State, transaction.Price)
        {
            OfferState = transaction.OfferState;
            PaidAmmount = paid;
            State = state;
            Id = transaction.Id;
        }
    }
}
