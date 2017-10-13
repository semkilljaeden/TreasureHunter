using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using TreasureHunter.Bot.TransactionObjects;
using TreasureHunter.Contract.AkkaMessageObject;
using TreasureHunter.SteamTrade;
using TreasureHunter.SteamTrade.TradeOffer;

namespace TreasureHunter.Bot
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
            var escowDuration = Bot.GetEscrowDuration(offer.TradeOfferId);
            var error = Bot.CheckIfBotCanAcceptTradeOffer(offer.TradeOfferId);
            if (error != null)
            {
                SendChatMessage("Bot Unable to Trade. Please Cancel your trade Offer");
                Log.Error("Bot Unable to Trade £º " + error);
                return;
            }
            var itemsTuple = GetItems(offer);
            var ourItemString = Environment.NewLine + string.Join(Environment.NewLine + "              ",
                                    itemsTuple.Item1.Select(i => i.ToString()));
            var theirItemString = Environment.NewLine + string.Join(Environment.NewLine + "              ",
                                      itemsTuple.Item2.Select(i => i.ToString()));
            Log.Info($"Order Update from {Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)} OrderId = " + offer.TradeOfferId);
            Log.Info("They want " + ourItemString);
            Log.Info("I will get " + theirItemString);
            double price = Bot.Valuate(itemsTuple.Item1, itemsTuple.Item2);
            var transaction = Bot
                .UpdateTradeOffer(new TradeOfferTransaction(offer.TradeOfferId), DataAccessActionType.Retrieve);
            var oldTransaction = transaction;
            if (transaction == null)
            {
                transaction = new TradeOfferTransaction(offer, TradeOfferTransactionState.New, price, Bot);
            }
            else
            {
                transaction = new TradeOfferTransaction(transaction, offer);
            }
            Log.Info(transaction);
            if (transaction.State != TradeOfferTransactionState.Paid && oldTransaction?.State == transaction.State && oldTransaction?.OfferState == transaction.OfferState) //force update paid status
            {
                Log.Info("Incoming TradeOffer is not updated, Skip");
                return;
            }
            switch (offer.OfferState)
            {
                case TradeOfferState.TradeOfferStateAccepted:
                    Log.Info($"Trade offer {offer.TradeOfferId} from {Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)} has been completed!");
                    transaction = new TradeOfferTransaction(transaction, TradeOfferTransactionState.Completed);
                    SendChatMessage("Trade completed, thank you!");
                    Log.Info("Update the Offer...");
                    Bot.UpdateTradeOffer(transaction, DataAccessActionType.UpdateTradeOffer);
                    return;
                case TradeOfferState.TradeOfferStateActive:
                    Log.Info("Update the Offer...");
                    Bot
                        .UpdateTradeOffer(transaction, DataAccessActionType.UpdateTradeOffer);
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
                                SendChatMessage($"Please pay SGD ${transaction.Price} to {"Kiljaeden"} for Order {transaction.Id}");
                                break;
                            case TradeOfferTransactionState.Paid:
                            case TradeOfferTransactionState.Completed:
                                Log.Info($"Existing Trade, Paid completed {transaction.Id}");
                                Bot.PaymentNotifySelf(transaction);
                                break;
                            case TradeOfferTransactionState.PartialPaid:
                                Log.Info($"Existing Trade, Partial Paid, Price = {transaction.Price}, PaidAmmount = {transaction.PaidAmmount} {transaction.Id}");
                                SendChatMessage(
                                    $"Please pay SGD ${transaction.Price - transaction.PaidAmmount} to {"Kiljaeden"} for Order {transaction.Id}");
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
                    Log.Info("Update the Offer...");
                    Bot
                        .UpdateTradeOffer(transaction, DataAccessActionType.UpdateTradeOffer);
                    Log.Info($"Trade offer {offer.TradeOfferId} from {Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)} Needs Confirmation - " + Environment.NewLine
                            + "Bot:" + (offer.Items.GetMyItems().Count > 0) + Environment.NewLine
                            + "{Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)}: " + (offer.Items.GetTheirItems().Count > 0));                    
                    switch (transaction.State)
                    {
                        case TradeOfferTransactionState.Completed:                          
                            if (offer.Items.GetTheirItems().Count > 0)
                            {
                                SendChatMessage($"Please Confirm Trade");
                            }
                            break;
                        case TradeOfferTransactionState.Paid:
                            if (offer.Items.GetTheirItems().Count > 0)
                            {
                                SendChatMessage($"Please Confirm Trade");
                            }
                            Bot.PaymentNotifySelf(transaction);
                            break;
                        case TradeOfferTransactionState.New:                        
                        case TradeOfferTransactionState.PartialPaid:
                            Log.Error("Trade reachs confirmation stage but transaction is at " + transaction.State);
                            break;
                    }
                    break;
                case TradeOfferState.TradeOfferStateInEscrow:
                    Log.Info("Update the Offer...");
                    Bot
                        .UpdateTradeOffer(transaction, DataAccessActionType.UpdateTradeOffer);
                    Log.Info($"Trade offer {offer.TradeOfferId} from {Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)} in Escow");                    
                    switch (transaction.State)
                    {
                        case TradeOfferTransactionState.Completed:
                            Log.Info($"Trade offer {offer.TradeOfferId} from {Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)} Needs {(offer.IsOurOffer ? "bot" : offer.TradeOfferId)} Confirmation");
                            string @object = offer.IsOurOffer ? "Bot's" : "Your";
                            Log.Info($"Existing Trade, Paid completed {transaction.Id}¡£ Due to {@object} Account Limitation, the Trade is on-hold by steam");
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
                    Log.Info("Update the Offer...");
                    Bot
                        .UpdateTradeOffer(transaction, DataAccessActionType.UpdateTradeOffer);
                    Log.Info($"Trade offer {offer.TradeOfferId} was countered by " + (offer.IsOurOffer ? "bot" : Bot.SteamFriends.GetFriendPersonaName(offer.PartnerSteamId)));
                    break;
                case TradeOfferState.TradeOfferStateInvalid:
                case TradeOfferState.TradeOfferStateExpired:
                case TradeOfferState.TradeOfferStateCanceled:
                case TradeOfferState.TradeOfferStateDeclined:
                case TradeOfferState.TradeOfferStateInvalidItems:
                case TradeOfferState.TradeOfferStateCanceledBySecondFactor:
                case TradeOfferState.TradeOfferStateUnknown:
                    Log.Info("Update the Offer...");
                    Bot
                        .UpdateTradeOffer(transaction, DataAccessActionType.UpdateTradeOffer);
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
            Log.Info("Update the Offer...");
            Bot
                .UpdateTradeOffer(transaction, DataAccessActionType.UpdateTradeOffer);
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

