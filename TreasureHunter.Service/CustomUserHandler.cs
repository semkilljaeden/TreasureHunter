using System;
using SteamKit2;
using System.Collections.Generic;
using System.Linq;
using TreasureHunter.Contract.AkkaMessageObject;
using TreasureHunter.Contract.TransactionObjects;
using TreasureHunter.SteamTrade;
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
            var escowDuration = Bot.GetEscrowDuration(offer.PartnerSteamId, null);
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
            var transaction = Bot
                .UpdateTradeOffer(new TradeOfferTransaction(offer.TradeOfferId), DataAccessActionType.Retrieve);
            if (transaction.Id == Guid.Empty)
            {
                transaction = new TradeOfferTransaction(offer, TradeOfferTransactionState.New, price);
            }
            else
            {
                transaction = new TradeOfferTransaction(transaction, offer);
            }
            Log.Info($"{transaction.Id} " + Environment.NewLine + 
                     $"from {Bot.SteamFriends.GetFriendPersonaName(transaction.Offer.PartnerSteamId)} " + Environment.NewLine +
                     $"State = {transaction.State}, " + Environment.NewLine +
                     $"Price = {transaction.Price}" + Environment.NewLine +
                     $"PaidAmmount = {transaction.PaidAmmount}" + Environment.NewLine + 
                     $"Transaction State = {transaction.State}");
            if (offer.OfferState != transaction.OfferState)
            {
                Log.Info("New TradeOffer State:" + offer.OfferState);
                Log.Info("TradeOffer in Database State:" + transaction.OfferState);
            }
            Bot
                .UpdateTradeOffer(transaction, DataAccessActionType.UpdateTradeOffer);
            switch (offer.OfferState)
            {
                case TradeOfferState.TradeOfferStateAccepted:
                    Log.Info($"Trade offer {offer.TradeOfferId} from {Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)} has been completed!");
                    SendChatMessage("Trade completed, thank you!");
                    break;
                case TradeOfferState.TradeOfferStateActive:
                    if (!offer.IsOurOffer)
                    {
                        switch (transaction.State)
                        {
                            case TradeOfferTransactionState.New:
                                Log.Info("New Trade Found");
                                if (escowDuration.DaysMyEscrow > 0)
                                {
                                    SendChatMessage(
                                        $"WARNING!!!!! This Bot is under Trade escowDuration and the items you buying will be hold by steam for {escowDuration.DaysMyEscrow}, if you don't want to proceed please cancel the tradeoffer and do not pay");
                                }
                                if (escowDuration.DaysTheirEscrow > 0)
                                {
                                    SendChatMessage(
                                        $"WARNING!!!!! You are under Trade escowDuration and the items you buying will be hold by steam for {escowDuration.DaysMyEscrow}, if you don't want to proceed please cancel the tradeoffer and do not pay");
                                }
                                SendChatMessage($"Please pay SGD ${transaction.Price} to {Transaction.Transaction.PaymentMethod} for Order {transaction.Id}");
                                break;
                            case TradeOfferTransactionState.Paid:
                            case TradeOfferTransactionState.Completed:
                                Log.Info($"Existing Trade, Paid completed {transaction.Id}");
                                Bot.PaymentNotifySelf(transaction);
                                break;
                            case TradeOfferTransactionState.PartialPaid:
                                Log.Info($"Existing Trade, Partial Paid, Price = {transaction.Price}, PaidAmmount = {transaction.PaidAmmount} {transaction.Id}");
                                SendChatMessage(
                                    $"Please pay SGD ${transaction.Price - transaction.PaidAmmount} to {Transaction.Transaction.PaymentMethod} for Order {transaction.Id}");
                                break;
                            case TradeOfferTransactionState.Expired:
                                Log.Info($"Existing Trade, Transaction Expired");
                                SendChatMessage(
                                    $"Payment Period Expired for Order {transaction.Id}");
                                offer.Decline();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    break;
                case TradeOfferState.TradeOfferStateNeedsConfirmation:
                    Log.Info($"Trade offer {offer.TradeOfferId} from {Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)} Needs {(offer.IsOurOffer ? "bot" : offer.TradeOfferId)} Confirmation");                    
                    switch (transaction.State)
                    {
                        case TradeOfferTransactionState.Completed:                          
                            if (!offer.IsOurOffer)
                            {
                                SendChatMessage($"Please Confirm Trade");
                            }
                            break;
                        case TradeOfferTransactionState.New:
                        case TradeOfferTransactionState.Paid:
                        case TradeOfferTransactionState.PartialPaid:
                            Log.Error("Trade reachs confirmation stage but transaction is at " + transaction.State);
                            break;
                    }
                    break;
                case TradeOfferState.TradeOfferStateInEscrow:
                    Log.Info($"Trade offer {offer.TradeOfferId} from {Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)} in Escow");                    
                    switch (transaction.State)
                    {
                        case TradeOfferTransactionState.Completed:
                            Log.Info($"Trade offer {offer.TradeOfferId} from {Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)} Needs {(offer.IsOurOffer ? "bot" : offer.TradeOfferId)} Confirmation");
                            string @object = offer.IsOurOffer ? "Bot's" : "Your";
                            Log.Info($"Existing Trade, Paid completed {transaction.Id}�� Due to {@object} Account Limitation, the Trade is on-hold by steam");
                            SendChatMessage($"Due to {@object} Account Limitation, the Trade is on-hold by steam");
                            break;
                        case TradeOfferTransactionState.New:
                        case TradeOfferTransactionState.Paid:
                        case TradeOfferTransactionState.PartialPaid:
                            Log.Error("Trade reachs escow stage but transaction is at " + transaction.State);
                            break;
                    }
                    
                    break;
                case TradeOfferState.TradeOfferStateCountered:
                    Log.Info($"Trade offer {offer.TradeOfferId} was countered by " + (offer.IsOurOffer ? "bot" : Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)));
                    break;
                case TradeOfferState.TradeOfferStateInvalid:
                case TradeOfferState.TradeOfferStateExpired:
                case TradeOfferState.TradeOfferStateCanceled:
                case TradeOfferState.TradeOfferStateDeclined:
                case TradeOfferState.TradeOfferStateInvalidItems:
                case TradeOfferState.TradeOfferStateCanceledBySecondFactor:
                case TradeOfferState.TradeOfferStateUnknown:
                    Log.Info($"Trade offer {offer.TradeOfferId} failed because of {offer.OfferState}");                   
                    switch (transaction.State)
                    {
                        case TradeOfferTransactionState.Completed:
                            Log.Info($"Existing Trade, Paid completed {transaction.Id}");
                            SendChatMessage($"Due to Unknown failure, we have received your payment but the item is not traded, Please contact our support");
                            break;
                        case TradeOfferTransactionState.PartialPaid:
                            Log.Info($"Existing Trade, Paid partially ammount = {transaction.PaidAmmount} for {transaction.Id}");
                            SendChatMessage($"Due to Unknown failure, we have received your partial payment but the item is not traded, Please contact our support");
                            break;
                        case TradeOfferTransactionState.New:
                        case TradeOfferTransactionState.Paid:
                            Log.Error("Trade reachs escow stage but transaction is at " + transaction.State);
                            break;
                    }
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

