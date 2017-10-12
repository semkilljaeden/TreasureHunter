using System;
using TreasureHunter.SteamTrade.TradeOffer;

namespace TreasureHunter.Contract.TransactionObjects
{ 
    public enum TradeOfferTransactionState
    {
        New,
        Paid,
        PartialPaid,
        Completed,
        Expired
    }
    public class TradeOfferTransaction
    {
        public TradeOffer Offer { get; private set; }
        public string Id { get; private set; }
        public TradeOfferTransactionState State { get; private set; }
        public TradeOfferState OfferState { get; private set; }
        public double Price { get; private set; }
        public double PaidAmmount { get; private set; }
        /// <summary>
        /// New Transaction
        /// </summary>
        /// <param name="offer"></param>
        /// <param name="state"></param>
        /// <param name="price"></param>
        public TradeOfferTransaction(TradeOffer offer, TradeOfferTransactionState state, double price)
        {
            OfferState = offer.OfferState;
            Offer = offer;
            State = state;
            Price = price;
            PaidAmmount = 0.0;
            Id = offer.TradeOfferId;
        }
        /// <summary>
        /// Update Transaction State
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="state"></param>
        public TradeOfferTransaction(TradeOfferTransaction transaction, TradeOfferTransactionState state) :
            this(transaction.Offer, state, transaction.Price)
        {
            PaidAmmount = transaction.PaidAmmount;
        }
        /// <summary>
        /// Update Offer
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="offer"></param>
        public TradeOfferTransaction(TradeOfferTransaction transaction, TradeOffer offer)
        {
            OfferState = offer.OfferState;
            Id = offer.TradeOfferId;
            Offer = offer;
            PaidAmmount = transaction.PaidAmmount;
            Price = transaction.Price;
            State = transaction.State;
        }

        /// <summary>
        /// Retreive Transaction by Offer
        /// </summary>
        /// <param name="id"></param>
        public TradeOfferTransaction(string id)
        {
            OfferState = default(TradeOfferState);
            Offer = null;
            Id = id;
        }
    }
}
