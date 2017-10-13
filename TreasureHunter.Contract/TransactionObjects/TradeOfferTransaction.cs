using Newtonsoft.Json;
using System;
using TreasureHunter.Contract.AkkaMessageObject;
using TreasureHunter.SteamTrade.TradeOffer;

namespace TreasureHunter.Contract.TransactionObjects
{ 
    public enum TradeOfferTransactionState
    {
        New,
        Paid,
        PartialPaid,
        Completed,
        Expired,
    }
    public class TradeOfferTransaction
    {
        [JsonProperty]
        public Guid Id { get; private set; }
        [JsonProperty]
        public TradeOffer Offer { get; private set; }
        [JsonProperty]
        public string TradeOfferId { get; private set; }
        [JsonProperty]
        public TradeOfferTransactionState State { get; private set; }
        [JsonProperty]
        public TradeOfferState OfferState { get; private set; }
        [JsonProperty]
        public double Price { get; private set; }
        [JsonProperty]
        public double PaidAmmount { get; private set; }
        [JsonProperty]
        public DateTime TimeStamp { get; private set; }
        [JsonProperty]
        public string Buyer { get; private set; }
        /// <summary>
        /// Json Deserialize Constructor
        /// </summary>
        [JsonConstructor]
        public TradeOfferTransaction()
        {
            
        }

        /// <summary>
        /// New Transaction
        /// </summary>
        /// <param name="offer"></param>
        /// <param name="state"></param>
        /// <param name="price"></param>
        /// <param name="paidAmmount"></param>       
        public TradeOfferTransaction(TradeOffer offer, TradeOfferTransactionState state, double price, double paidAmmount = 0.0)
        {
            OfferState = offer.OfferState;
            Offer = offer;
            State = state;
            Price = price;
            PaidAmmount = paidAmmount;
            Id = Guid.NewGuid();
            TradeOfferId = offer.TradeOfferId;
            TimeStamp = DateTime.UtcNow;
            Buyer = null;
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
            Buyer = null;
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
            Buyer = null;
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
            Buyer = null;
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
            Buyer = null;
        }

        /// <summary>
        /// Update transaction with payment
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="msg"></param>
        public TradeOfferTransaction(TradeOfferTransaction transaction, PaymentMessage msg)
        {
            Id = transaction.Id;
            PaidAmmount = msg.PaidAmmount;
            OfferState = transaction.OfferState;
            Offer = transaction.Offer;
            State = transaction.State;
            Price = transaction.Price;
            TradeOfferId = transaction.TradeOfferId;
            Buyer = msg.Buyer;
        }

        /// <summary>
        /// Payment without saved ID
        /// </summary>
        /// <param name="msg"></param>
        public TradeOfferTransaction(PaymentMessage msg)
        {
            Id = msg.TransactionId;
            PaidAmmount = msg.PaidAmmount;
            OfferState = default(TradeOfferState);
            Offer = null;
            Price = default(double);
            TradeOfferId = null;
            State = TradeOfferTransactionState.Paid;
            Buyer = msg.Buyer;
        }
    }
}
