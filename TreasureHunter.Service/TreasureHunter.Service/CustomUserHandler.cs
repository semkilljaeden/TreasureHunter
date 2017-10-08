using System;
using SteamKit2;
using System.Collections.Generic;
using System.Linq;
using SteamTrade;
using SteamTrade.TradeOffer;
using SteamTrade.TradeWebAPI;
using TreasureHunter;

namespace TreasureHunter.Service
{
    public class CustomUserHandler : UserHandler
    {

        public CustomUserHandler(BotActor bot, SteamID sid) : base(bot, sid) {}

        public override bool OnGroupAdd()
        {
            return false;
        }

        public override bool OnFriendAdd () 
        {
            this.Bot.SteamFriends.AddFriend(OtherSID);
            Log.Info("Added Friend - " + OtherSID.AccountID);
            return true;
        }

        public override void OnLoginCompleted()
        {
            this.Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
        }

        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {
            Log.Info(Bot.SteamFriends.GetFriendPersonaName(sender) + ": " + message);
            base.OnChatRoomMessage(chatID, sender, message);
        }

        public override void OnFriendRemove () {}
        
        public override void OnMessage (string message, EChatEntryType type) 
        {
            SendChatMessage(Bot.ChatResponse);
        }

        public override bool OnTradeRequest() 
        {
            return true;
        }
        
        public override void OnTradeError (string error) 
        {
            SendChatMessage("Oh, there was an error: {0}.", error);
            Log.Warn (error);
        }
        
        public override void OnTradeTimeout () 
        {
            SendChatMessage("Sorry, but you were AFK and the trade was canceled.");
            Log.Info ("User was kicked because he was AFK.");
        }
        
        public override void OnTradeInit() 
        {
            SendTradeMessage("Success. Please put up your items.");
        }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            // USELESS DEBUG MESSAGES -------------------------------------------------------------------------------
            SendTradeMessage("Dota2 Item Added.");
            SendTradeMessage("Name: {0}", schemaItem.Name);
            SendTradeMessage("Price: {0}", schemaItem.Price);
            SendTradeMessage("Quality: {0}", inventoryItem.Quality);
            SendTradeMessage("Level: {0}", inventoryItem.Level);
            SendTradeMessage("Craftable: {0}", (inventoryItem.IsNotCraftable ? "No" : "Yes"));
        }

        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {}
        
        public override void OnTradeMessage (string message) {}
        
        public override void OnTradeReady (bool ready)
        {
            Trade.SetReady(ready);
        }

        public override void OnTradeAwaitingConfirmation(long tradeOfferID)
        {
            Log.Warn("Trade ended awaiting confirmation");
            SendChatMessage("Please complete the confirmation to finish the trade");
        }

        public override void OnTradeOfferUpdated(TradeOffer offer)
        {
            switch (offer.OfferState)
            {
                case TradeOfferState.TradeOfferStateAccepted:
                    Log.Info($"Trade offer {offer.TradeOfferId} from {offer.PartnerSteamId.Render()} has been completed!");
                    SendChatMessage("Trade completed, thank you!");
                    break;
                case TradeOfferState.TradeOfferStateActive:
                    OnNewTradeOffer(offer);
                    break;
                case TradeOfferState.TradeOfferStateNeedsConfirmation:
                    Log.Info($"Trade offer {offer.TradeOfferId} from {offer.PartnerSteamId.Render()} Needs Confirmation");
                    break;
                case TradeOfferState.TradeOfferStateInEscrow:
                    //Trade is still active but incomplete
                    Log.Info($"Trade offer {offer.TradeOfferId} from {offer.PartnerSteamId.Render()} in Escow");
                    break;
                case TradeOfferState.TradeOfferStateCountered:
                    Log.Info($"Trade offer {offer.TradeOfferId} was countered");
                    break;
                default:
                    Log.Info($"Trade offer {offer.TradeOfferId} failed");
                    break;
            }
        }

        private void OnNewTradeOffer(TradeOffer offer)
        {
            Bot.GetInventory();
            GetOtherInventory();
            var myInventory = Bot.MyInventory;
            var myItems = offer.Items.GetMyItems();
            var theirItems = offer.Items.GetTheirItems();
            var myItemsWithSchema = myItems.Select(
                i => Schema.GetSchema().GetItem(myInventory.GetItem((ulong)i.AssetId).Defindex)).ToList();
            var theirItemsWithSchema = theirItems.Select(
                i => Schema.GetSchema().GetItem(OtherInventory.GetItem((ulong)i.AssetId).Defindex)).ToList();
            Log.Info("They want " + Environment.NewLine + myItemsWithSchema.Select(i => i.ToString()).Aggregate((a, b) => a + Environment.NewLine + b));
            Log.Info("And I will get " + Environment.NewLine + theirItemsWithSchema.Select(i => i.ToString()).Aggregate((a, b) => a + Environment.NewLine + b));
            string token = Token.GenerateToken();
            double price = Bot.Valuate(myItemsWithSchema, theirItemsWithSchema);
            Log.Info($"{offer.TradeOfferId} from {offer.PartnerSteamId.Render()} has Token = {token}, Price = {price}");
            SendChatMessage($"Please pay ${price} in Singapore Dollar and Include Token = {token} in the payment");
            Bot.EnqueueForPayment(offer, token, price);
        }
        public override void OnTradeAccept() 
        {
            if (IsAdmin)
            {
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
                try {
                    if (Trade.AcceptTrade())
                        Log.Info("Trade Accepted!");
                }
                catch {
                    Log.Warn ("The trade might have failed, but we can't be sure.");
                }
            }
        }

        private bool Validation(List<TradeOffer.TradeStatusUser.TradeAsset> myAssets, List<TradeOffer.TradeStatusUser.TradeAsset> theirAssets)
        {
            //compare items etc
            if (myAssets.Count == theirAssets.Count)
            {
                return true;
            }
            return true;
        }

    }
 
}

