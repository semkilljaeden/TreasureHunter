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
        public Guid Id { get; private set; }
        public TradeOffer Offer { get; private set; }
        public string TradeOfferId { get; private set; }
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
            Id = Guid.NewGuid();
            TradeOfferId = offer.TradeOfferId;
        }
        /// <summary>
        /// Update Transaction State
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="state"></param>
        public TradeOfferTransaction(TradeOfferTransaction transaction, TradeOfferTransactionState state) :
            this(transaction.Offer, state, transaction.Price)
        {
            Id = transaction.Id;
        }
        /// <summary>
        /// Update Offer
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="offer"></param>
        public TradeOfferTransaction(TradeOfferTransaction transaction, TradeOffer offer) :
            this(transaction.Offer, transaction.State, transaction.Price)
        {
            OfferState = transaction.OfferState;
            Id = transaction.Id;
            TradeOfferId = offer.TradeOfferId;
            Offer = offer;
        }

        /// <summary>
        /// Retreive Transaction by Offer
        /// </summary>
        /// <param name="tradeOfferId"></param>
        public TradeOfferTransaction(string tradeOfferId)
        {
            OfferState = default(TradeOfferState);
            Offer = null;
            Id = Guid.Empty;
            TradeOfferId = tradeOfferId;
        }
        /// <summary>
        /// Retreive Transaction by ID
        /// </summary>
        /// <param name="id"></param>
        public TradeOfferTransaction(Guid id)
        {
            OfferState = default(TradeOfferState);
            Offer = null;
            Id = id;
            TradeOfferId = null;
        }
    }
}
