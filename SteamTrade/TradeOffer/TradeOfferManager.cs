using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using SteamKit2;
using TreasureHunter.SteamTrade;

namespace TreasureHunter.SteamTrade.TradeOffer
{
    public class TradeOfferManager
    {
        private readonly Dictionary<string, TradeOfferState> _knownTradeOffers = new Dictionary<string, TradeOfferState>();
        public OfferSession Session { get; private set; }
        private readonly TradeOfferWebAPI _webApi;
        private readonly ConcurrentQueue<Offer> _unhandledTradeOfferUpdates;
        public DateTime LastTimeCheckedOffers { get; private set; }
        public TradeOfferManager(string apiKey, SteamWeb steamWeb, DateTime lastTimeCheckedOffers)
        {
            if (apiKey == null)
                throw new ArgumentNullException(nameof(apiKey));
            LastTimeCheckedOffers = lastTimeCheckedOffers;
            _webApi = new TradeOfferWebAPI(apiKey, steamWeb);
            Session = new OfferSession(_webApi, steamWeb);
            _unhandledTradeOfferUpdates = new ConcurrentQueue<Offer>();
        }

        public delegate void TradeOfferUpdatedHandler(TreasureHunter.SteamTrade.TradeOffer.TradeOffer offer);

        /// <summary>
        /// Occurs when a new trade offer has been made by the other user
        /// </summary>
        public event TradeOfferUpdatedHandler OnTradeOfferUpdated;

        public void EnqueueUpdatedOffers()
        {
            DateTime startTime = DateTime.Now;

            var offersResponse = (LastTimeCheckedOffers == DateTime.MinValue
                ? _webApi.GetAllTradeOffers()
                : _webApi.GetAllTradeOffers(GetUnixTimeStamp(LastTimeCheckedOffers).ToString()));
            AddTradeOffersToQueue(offersResponse);

            LastTimeCheckedOffers = startTime - TimeSpan.FromMinutes(5); //Lazy way to make sure we don't miss any trade offers due to slightly differing clocks
        }

        private void AddTradeOffersToQueue(OffersResponse offers)
        {
            if (offers?.AllOffers == null)
                return;

            foreach (var offer in offers.AllOffers)
            {
                _unhandledTradeOfferUpdates.Enqueue(offer);
            }
        }

        public bool HandleNextPendingTradeOfferUpdate()
        {
            Offer nextOffer;
            return _unhandledTradeOfferUpdates
                       .TryDequeue(out nextOffer) && HandleTradeOfferUpdate(nextOffer);
        }

        private bool HandleTradeOfferUpdate(Offer offer)
        {
            if(_knownTradeOffers.ContainsKey(offer.TradeOfferId) && _knownTradeOffers[offer.TradeOfferId] == offer.TradeOfferState)
            {
                return false;
            }

            //make sure the api loaded correctly sometimes the items are missing
            if(IsOfferValid(offer))
            {
                SendOfferToHandler(offer);
            }
            else
            {
                var resp = _webApi.GetTradeOffer(offer.TradeOfferId);
                if(IsOfferValid(resp.Offer))
                {
                    SendOfferToHandler(resp.Offer);
                }
                else
                {
                    Debug.WriteLine("Offer returned from steam api is not valid : " + resp.Offer.TradeOfferId);
                    return false;
                }
            }
            return true;
        }

        private bool IsOfferValid(Offer offer)
        {
            bool hasItemsToGive = offer.ItemsToGive != null && offer.ItemsToGive.Count != 0;
            bool hasItemsToReceive = offer.ItemsToReceive != null && offer.ItemsToReceive.Count != 0;
            return hasItemsToGive || hasItemsToReceive;
        }

        private void SendOfferToHandler(Offer offer)
        {
            _knownTradeOffers[offer.TradeOfferId] = offer.TradeOfferState;
            if (OnTradeOfferUpdated == null)
            {
                throw new NullReferenceException(nameof(OnTradeOfferUpdated));
            }
            OnTradeOfferUpdated(new TradeOffer(Session, offer));
        }

        private uint GetUnixTimeStamp(DateTime dateTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (uint)((dateTime.ToUniversalTime() - epoch).TotalSeconds);
        }

        public TradeOffer NewOffer(SteamID other)
        {
            return new TradeOffer(Session, other);
        }

        public bool TryGetOffer(string offerId, out TradeOffer tradeOffer)
        {
            tradeOffer = null;
            var resp = _webApi.GetTradeOffer(offerId);
            if (resp != null)
            {
                if (IsOfferValid(resp.Offer))
                {
                    tradeOffer = new TreasureHunter.SteamTrade.TradeOffer.TradeOffer(Session, resp.Offer);
                    return true;
                }
                else
                {
                    Debug.WriteLine("Offer returned from steam api is not valid : " + resp.Offer.TradeOfferId);
                }
            }
            return false;
        }
    }
}