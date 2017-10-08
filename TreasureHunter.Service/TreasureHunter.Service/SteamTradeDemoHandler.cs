using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using SteamTrade.TradeOffer;

namespace TreasureHunter.Service
{
    public class SteamTradeDemoHandler : UserHandler
    {
        // NEW ------------------------------------------------------------------
        private readonly GenericInventory _mySteamInventory;
        private readonly GenericInventory _otherSteamInventory;

        // ----------------------------------------------------------------------

        public SteamTradeDemoHandler(BotActor bot, SteamID sid) : base(bot, sid)
        {
            _mySteamInventory = new GenericInventory(SteamWeb);
            _otherSteamInventory = new GenericInventory(SteamWeb);
        }

        public override bool OnGroupAdd()
        {
            return false;
        }

        public override bool OnFriendAdd () 
        {
            return true;
        }

        public override void OnLoginCompleted() {}

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
            // NEW -------------------------------------------------------------------------------
            List<long> contextId = new List<long>();

            /*************************************************************************************
             * 
             * SteamInventory AppId = 753 
             * 
             *  Context Id      Description
             *      1           Gifts (Games), must be public on steam profile in order to work.
             *      6           Trading Cards, Emoticons & Backgrounds. 
             *  
             ************************************************************************************/

            contextId.Add(1);
            contextId.Add(6);

            _mySteamInventory.load(753, contextId, Bot.SteamClient.SteamID);
            _otherSteamInventory.load(753, contextId, OtherSID);

            if (!_mySteamInventory.isLoaded | !_otherSteamInventory.isLoaded)
            {
                SendTradeMessage("Couldn't open an inventory, type 'errors' for more info.");
            }

            SendTradeMessage("Type 'test' to start.");
            // -----------------------------------------------------------------------------------
        }
        
        public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {
            // USELESS DEBUG MESSAGES -------------------------------------------------------------------------------
            SendTradeMessage("Object AppID: {0}", inventoryItem.AppId);
            SendTradeMessage("Object ContextId: {0}", inventoryItem.ContextId);

            SendTradeMessage("Dota2 Item Added.");
            SendTradeMessage("Name: {0}", schemaItem.Name);
            SendTradeMessage("Quality: {0}", inventoryItem.Quality);
            SendTradeMessage("Level: {0}", inventoryItem.Level);
            SendTradeMessage("Craftable: {0}", (inventoryItem.IsNotCraftable ? "No" : "Yes"));
        }
        
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {}
        
        public override void OnTradeMessage (string message) {
            switch (message.ToLower())
            {
                case "errors":
                    if (_otherSteamInventory.errors.Count > 0)
                    {
                        SendTradeMessage("User Errors:");
                        foreach (string error in _otherSteamInventory.errors)
                        {
                            SendTradeMessage(" * {0}", error);
                        }
                    }

                    if (_mySteamInventory.errors.Count > 0)
                    {
                        SendTradeMessage("Bot Errors:");
                        foreach (string error in _mySteamInventory.errors)
                        {
                            SendTradeMessage(" * {0}", error);
                        }
                    }
                break;
               

                case "remove":
                    foreach (var item in _mySteamInventory.items)
                    {
                        Trade.RemoveItem(item.Value.assetid, item.Value.appid, item.Value.contextid);
                    }
                break;
            }
        }
        
        public override void OnTradeReady (bool ready) 
        {
            //Because SetReady must use its own version, it's important
            //we poll the trade to make sure everything is up-to-date.
            Trade.Poll();
            if (!ready)
            {
                Trade.SetReady (false);
            }
            else
            {
                if(Validate () | IsAdmin)
                {
                    Trade.SetReady (true);
                }
            }
        }

        public override void OnTradeOfferUpdated(TradeOffer offer)
        {
            if(offer.OfferState == TradeOfferState.TradeOfferStateAccepted)
            {
                Log.Info("Trade Complete.");
            }
        }

        public override void OnTradeAwaitingConfirmation(long tradeOfferID)
        {
            Log.Warn("Trade ended awaiting confirmation");
            SendChatMessage("Please complete the confirmation to finish the trade");
        }

        public override void OnTradeAccept() 
        {
            if (Validate() | IsAdmin)
            {
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
                try {
                    Trade.AcceptTrade();
                }
                catch {
                    Log.Warn ("The trade might have failed, but we can't be sure.");
                }

                Log.Info ("Trade Complete!");
            }
        }

        public bool Validate ()
        {            
            List<string> errors = new List<string> ();
            errors.Add("This demo is meant to show you how to handle SteamInventory Items. Trade cannot be completed, unless you're an Admin.");

            // send the errors
            if (errors.Count != 0)
                SendTradeMessage("There were errors in your trade: ");

            foreach (string error in errors)
            {
                SendTradeMessage(error);
            }
            
            return errors.Count == 0;
        }
        
    }
 
}

