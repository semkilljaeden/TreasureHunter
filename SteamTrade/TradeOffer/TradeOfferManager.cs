using System.Collections.Generic;
using System.Diagnostics;
using SteamKit2;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SteamTrade.TradeOffer
{
    public class TradeOfferManager
    {
        private readonly Dictionary<string, TradeOfferState> knownTradeOffers = new Dictionary<string, TradeOfferState>();
        private readonly OfferSession _session;
        private readonly TradeOfferWebAPI _webApi;
        private readonly Queue<Offer> _unhandledTradeOfferUpdates; 

        public DateTime LastTimeCheckedOffers { get; private set; }

        public TradeOfferManager(string apiKey, SteamWeb steamWeb)
        {
            if (apiKey == null)
                throw new ArgumentNullException(nameof(apiKey));

            LastTimeCheckedOffers = DateTime.MinValue;
            _webApi = new TradeOfferWebAPI(apiKey, steamWeb);
            _session = new OfferSession(_webApi, steamWeb);
            _unhandledTradeOfferUpdates = new Queue<Offer>();
        }

        public delegate void TradeOfferUpdatedHandler(TradeOffer offer);

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

            lock(_unhandledTradeOfferUpdates)
            {
                foreach(var offer in offers.AllOffers)
                {
                    _unhandledTradeOfferUpdates.Enqueue(offer);
                }
            }
        }

        public bool HandleNextPendingTradeOfferUpdate()
        {
            Offer nextOffer;
            lock (_unhandledTradeOfferUpdates)
            {
                if (!_unhandledTradeOfferUpdates.Any())
                {
                    return false;
                }
                nextOffer = _unhandledTradeOfferUpdates.Dequeue();
            }

            return HandleTradeOfferUpdate(nextOffer);
        }

        private bool HandleTradeOfferUpdate(Offer offer)
        {
            if(knownTradeOffers.ContainsKey(offer.TradeOfferId) && knownTradeOffers[offer.TradeOfferId] == offer.TradeOfferState)
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
            knownTradeOffers[offer.TradeOfferId] = offer.TradeOfferState;
            OnTradeOfferUpdated(new TradeOffer(_session, offer));
        }

        private uint GetUnixTimeStamp(DateTime dateTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (uint)((dateTime.ToUniversalTime() - epoch).TotalSeconds);
        }

        public TradeOffer NewOffer(SteamID other)
        {
            return new TradeOffer(_session, other);
        }

        public bool TryGetOffer(string offerId, out TradeOffer tradeOffer)
        {
            tradeOffer = null;
            var resp = _webApi.GetTradeOffer(offerId);
            if (resp != null)
            {
                if (IsOfferValid(resp.Offer))
                {
                    tradeOffer = new TradeOffer(_session, resp.Offer);
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