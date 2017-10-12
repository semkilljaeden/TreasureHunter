using System;
using SteamKit2;
using System.Collections.Generic;
using System.Linq;
using TreasureHunter.SteamTrade;
using TreasureHunter;
using TreasureHunter.Common.TransactionObjects;
using TreasureHunter.Transaction;
using TreasureHunter.SteamTrade.TradeOffer;

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
                    Log.Info($"Trade offer {offer.TradeOfferId} from {Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)} has been completed!");
                    SendChatMessage("Trade completed, thank you!");
                    break;
                case TradeOfferState.TradeOfferStateActive:
                    if (!offer.IsOurOffer)
                    {
                        OnNewTradeOffer(offer);
                    }
                    break;
                case TradeOfferState.TradeOfferStateNeedsConfirmation:
                    Log.Info($"Trade offer {offer.TradeOfferId} from {Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)} Needs Confirmation");
                    break;
                case TradeOfferState.TradeOfferStateInEscrow:
                    //Trade is still active but incomplete
                    Log.Info($"Trade offer {offer.TradeOfferId} from {Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)} in Escow");
                    OnTradeInEscow(offer);
                    break;
                case TradeOfferState.TradeOfferStateCountered:
                    Log.Info($"Trade offer {offer.TradeOfferId} was countered");
                    break;                
                default:
                    Log.Info($"Trade offer {offer.TradeOfferId} failed because of {offer.OfferState}");
                    break;
            }
        }


        private Tuple<List<Schema.Item>, List<Schema.Item>> GetItems(TradeOffer offer)
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
            
            return new Tuple<List<Schema.Item>, List<Schema.Item>>(myItemsWithSchema, theirItemsWithSchema);
        }

        private void OnTradeInEscow(TradeOffer offer)
        {
            
        }
        private void OnNewTradeOffer(TradeOffer offer)
        {
            var itemsTuple = GetItems(offer);
            var ourItemString = Environment.NewLine + string.Join(Environment.NewLine + "              ",
                                    itemsTuple.Item1.Select(i => i.ToString()));
            var theirItemString = Environment.NewLine + string.Join(Environment.NewLine + "              ",
                                      itemsTuple.Item2.Select(i => i.ToString()));
            Log.Info($"New Order Received from {Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)} OrderId = " + offer.TradeOfferId);
            Log.Info("They want " + theirItemString);
            Log.Info("I will get " + ourItemString);
            Log.Info("Update the Offer...");           
            double price = Bot.Valuate(itemsTuple.Item1, itemsTuple.Item2);
            var pending = Bot
                .UpdateTradeOffer(new TradeOfferTransaction(offer, TradeOfferTransactionState.New, price));
            Log.Info($"{pending.Id} " +
                     $"from {Bot.SteamFriends.GetFriendPersonaName(pending.Offer.PartnerSteamId)} " +
                     $"State = {pending.State}, " +
                     $"Price = {pending.Price}" +
                     $"PaidAmmount = {pending.PaidAmmount}");
            switch (pending.State)
            {
                case TradeOfferTransactionState.New:
                    Log.Info("New Trade Found");
                    SendChatMessage($"Please pay SGD ${pending.Price} to {Transaction.Transaction.PaymentMethod} for Order {pending.Id}");
                    Bot.EnqueueForPayment(pending);
                    break;
                case TradeOfferTransactionState.Paid:
                case TradeOfferTransactionState.Completed:
                    Log.Info($"Existing Trade, Paid completed {pending.Id}");
                    Bot.PaymentNotifySelf(pending);
                    break;
                case TradeOfferTransactionState.PartialPaid:
                    Log.Info($"Existing Trade, Partial Paid, Price = {pending.Price}, PaidAmmount = {pending.PaidAmmount} {pending.Id}");
                    SendChatMessage(
                        $"Please pay SGD ${pending.Price - pending.PaidAmmount} to {Transaction.Transaction.PaymentMethod} for Order {pending.Id}");
                    break;
                case TradeOfferTransactionState.UnPaid:
                    Log.Info($"Existing Trade, UnPaid, Price = {pending.Price}, PaidAmmount = {pending.PaidAmmount} {pending.Id}");
                    break;
                case TradeOfferTransactionState.Expired:
                    Log.Info($"Existing Trade, Trade Expired");
                    SendChatMessage(
                        $"Trade Expired for Order {pending.Id}");
                    offer.Decline();
                    Bot.UpdateTradeOffer(new TradeOfferTransaction(pending, TradeOfferTransactionState.Declined,
                        pending.PaidAmmount));
                    break;
                case TradeOfferTransactionState.Declined:
                    Log.Info($"Existing Trade, Trade Declined");
                    SendChatMessage(
                        $"Trade Declined by Bot for Order {pending.Id}");
                    offer.Decline();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

    }
 
}

