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
        public TradeOfferTransaction(TradeOfferTransaction transaction, TradeOfferTransactionState state) 
        {
            Id = transaction.Id;
            PaidAmmount = transaction.PaidAmmount;
            OfferState = transaction.OfferState;
            Offer = transaction.Offer;
            State = state;
            Price = transaction.Price;
            TradeOfferId = transaction.TradeOfferId;
        }
        /// <summary>
        /// Update Offer
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="offer"></param>
        public TradeOfferTransaction(TradeOfferTransaction transaction, TradeOffer offer)
        {
            Id = transaction.Id;
            PaidAmmount = transaction.PaidAmmount;
            OfferState = offer.OfferState;
            Offer = offer;
            Price = transaction.Price;
            TradeOfferId = offer.TradeOfferId;
        }

        /// <summary>
        /// Retreive Transaction by Offer
        /// </summary>
        /// <param name="tradeOfferId"></param>
        public TradeOfferTransaction(string tradeOfferId)
        {
            Id = Guid.Empty;
            PaidAmmount = default(double);
            OfferState = default(TradeOfferState);
            Offer = null;
            Price = default(double);
            TradeOfferId = tradeOfferId;
            State = default(TradeOfferTransactionState);
        }
        /// <summary>
        /// Retreive Transaction by ID
        /// </summary>
        /// <param name="id"></param>
        public TradeOfferTransaction(Guid id)
        {
            Id = id;
            PaidAmmount = default(double);
            OfferState = default(TradeOfferState);
            Offer = null;
            Price = default(double);
            TradeOfferId = null;
            State = default(TradeOfferTransactionState);
        }
    }
}
