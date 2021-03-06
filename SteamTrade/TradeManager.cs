using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using SteamKit2;
using TreasureHunter.SteamTrade.Exceptions;

namespace TreasureHunter.SteamTrade
{
    public class TradeManager
    {
        private const int MaxGapTimeDefault = 15;
        private const int MaxTradeTimeDefault = 180;
        private const int TradePollingIntervalDefault = 800;
        private readonly string _apiKey;
        private readonly SteamWeb _steamWeb;
        private DateTime _tradeStartTime;
        private DateTime _lastOtherActionTime;
        private DateTime _lastTimeoutMessage;
        private Task<Inventory> _myInventoryTask;
        private Task<Inventory> _otherInventoryTask;
        private readonly ILog Log = LogManager.GetLogger(typeof(TradeManager));
        /// <summary>
        /// Initializes a new instance of the <see cref="SteamTrade.TradeManager"/> class.
        /// </summary>
        /// <param name='apiKey'>
        /// The Steam Web API key. Cannot be null.
        /// </param>
        /// <param name="steamWeb">
        /// The SteamWeb instances for this bot
        /// </param>
        public TradeManager (string apiKey, SteamWeb steamWeb)
        {
            if (apiKey == null)
                throw new ArgumentNullException (nameof(apiKey));

            if (steamWeb == null)
                throw new ArgumentNullException (nameof(steamWeb));

            SetTradeTimeLimits (MaxTradeTimeDefault, MaxGapTimeDefault, TradePollingIntervalDefault);

            _apiKey = apiKey;
            _steamWeb = steamWeb;
        }

        #region Public Properties

        /// <summary>
        /// Gets or the maximum trading time the bot will take in seconds.
        /// </summary>
        /// <value>
        /// The maximum trade time.
        /// </value>
        public int MaxTradeTimeSec
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or the maxmium amount of time the bot will wait between actions. 
        /// </summary>
        /// <value>
        /// The maximum action gap.
        /// </value>
        public int MaxActionGapSec
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Gets the Trade polling interval in milliseconds.
        /// </summary>
        public int TradePollingInterval
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the inventory of the bot.
        /// </summary>
        /// <value>
        /// The bot's inventory fetched via Steam Web API.
        /// </value>
        public Inventory MyInventory
        {
            get
            {
                if(_myInventoryTask == null)
                    return null;

                _myInventoryTask.Wait();
                return _myInventoryTask.Result;
        }
        }

        /// <summary>
        /// Gets the inventory of the other trade partner.
        /// </summary>
        /// <value>
        /// The other trade partner's inventory fetched via Steam Web API.
        /// </value>
        public Inventory OtherInventory
        {
            get
            {
                if(_otherInventoryTask == null)
                    return null;

                _otherInventoryTask.Wait();
                return _otherInventoryTask.Result;
        }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the trade thread running.
        /// </summary>
        /// <value>
        /// <c>true</c> if the trade thread running; otherwise, <c>false</c>.
        /// </value>
        public bool IsTradeThreadRunning
        {
            get;
            internal set;
        }

        #endregion Public Properties

        #region Public Events

        /// <summary>
        /// Occurs when the trade times out because either the user didn't complete an
        /// action in a set amount of time, or they took too long with the whole trade.
        /// </summary>
        public EventHandler OnTimeout;

        #endregion Public Events

        #region Public Methods

        /// <summary>
        /// Sets the trade time limits.
        /// </summary>
        /// <param name='maxTradeTime'>
        /// Max trade time in seconds.
        /// </param>
        /// <param name='maxActionGap'>
        /// Max gap between user action in seconds.
        /// </param>
        /// <param name='pollingInterval'>The trade polling interval in milliseconds.</param>
        public void SetTradeTimeLimits (int maxTradeTime, int maxActionGap, int pollingInterval)
        {
            MaxTradeTimeSec = maxTradeTime;
            MaxActionGapSec = maxActionGap;
            TradePollingInterval = pollingInterval;
        }

        /// <summary>
        /// Creates a trade object and returns it for use. 
        /// Call <see cref="InitializeTrade"/> before using this method.
        /// </summary>
        /// <returns>
        /// The trade object to use to interact with the Steam trade.
        /// </returns>
        /// <param name='me'>
        /// The <see cref="SteamID"/> of the bot.
        /// </param>
        /// <param name='other'>
        /// The <see cref="SteamID"/> of the other trade partner.
        /// </param>
        /// <remarks>
        /// If the needed inventories are <c>null</c> then they will be fetched.
        /// </remarks>
        public Trade CreateTrade (SteamID  me, SteamID other)
        {
            if (_otherInventoryTask == null || _myInventoryTask == null)
                InitializeTrade (me, other);

            var t = new Trade (me, other, _steamWeb, _myInventoryTask, _otherInventoryTask);

            t.OnClose += delegate
            {
                IsTradeThreadRunning = false;
            };

            return t;
        }

        /// <summary>
        /// Stops the trade thread.
        /// </summary>
        /// <remarks>
        /// Also, nulls out the inventory objects so they have to be fetched
        /// again if a new trade is started.
        /// </remarks>            
        public void StopTrade ()
        {
            // TODO: something to check that trade was the Trade returned from CreateTrade
            _otherInventoryTask = null;
            _myInventoryTask = null;

            IsTradeThreadRunning = false;
        }

        /// <summary>
        /// Fetchs the inventories of both the bot and the other user as well as the TF2 item schema.
        /// </summary>
        /// <param name='me'>
        /// The <see cref="SteamID"/> of the bot.
        /// </param>
        /// <param name='other'>
        /// The <see cref="SteamID"/> of the other trade partner.
        /// </param>
        /// <remarks>
        /// This should be done anytime a new user is traded with or the inventories are out of date. It should
        /// be done sometime before calling <see cref="CreateTrade"/>.
        /// </remarks>
        public void InitializeTrade (SteamID me, SteamID other)
        {
            // fetch other player's inventory from the Steam API.
            _otherInventoryTask = Task.Factory.StartNew(() => Inventory.FetchInventory(other.ConvertToUInt64(), _apiKey, _steamWeb));

            if (OtherInventory == null)
            {
                throw new InventoryFetchException (other);
            }
            
            // fetch our inventory from the Steam API.
            _myInventoryTask = Task.Factory.StartNew(() => Inventory.FetchInventory(me.ConvertToUInt64(), _apiKey, _steamWeb));
            
            // check that the schema was already successfully fetched
            if (Trade.CurrentSchema == null)
                Trade.CurrentSchema = Schema.Init (_apiKey);

            if (Trade.CurrentSchema == null)
                throw new TradeException ("Could not download the latest item schema.");
        }

        #endregion Public Methods

        /// <summary>
        /// Starts the actual trade-polling thread.
        /// </summary>
        public void StartTradeThread (Trade trade)
        {
            // initialize data to use in thread
            _tradeStartTime = DateTime.Now;
            _lastOtherActionTime = DateTime.Now;
            _lastTimeoutMessage = DateTime.Now.AddSeconds(-1000);

            var pollThread = new Thread (() =>
            {
                IsTradeThreadRunning = true;

                Log.Debug("Trade thread starting.");
                
                // main thread loop for polling
                try
                {
                    while(IsTradeThreadRunning)
                    {
                        bool action = trade.Poll();

                        if(action)
                            _lastOtherActionTime = DateTime.Now;

                        if (trade.HasTradeEnded || CheckTradeTimeout(trade))
                        {
                            IsTradeThreadRunning = false;
                            break;
                        }

                        Thread.Sleep(TradePollingInterval);
                    }
                }
                catch(Exception ex)
                {
                    // TODO: find a new way to do this w/o the trade events
                    //if (OnError != null)
                    //    OnError("Error Polling Trade: " + e);

                    // ok then we should stop polling...
                    IsTradeThreadRunning = false;
                    Log.Debug("[TRADEMANAGER] general error caught: " + ex);
                    trade.FireOnErrorEvent("Unknown error occurred: " + ex.ToString());
                }
                finally
                {
                    Log.Debug("Trade thread shutting down.");
                    try //Yikes, that's a lot of nested 'try's.  Is there some way to clean this up?
                    {
                        if(trade.IsTradeAwaitingConfirmation)
                            trade.FireOnAwaitingConfirmation();
                    }
                    catch(Exception ex)
                    {
                        trade.FireOnErrorEvent("Unknown error occurred during OnTradeAwaitingConfirmation: " + ex.ToString());
                    }
                    finally
                    {
                        try
                        {
                            //Make sure OnClose is always fired after OnSuccess, even if OnSuccess throws an exception
                            //(which it NEVER should, but...)
                            trade.FireOnCloseEvent();
                        }
                        catch (Exception e)
                        {
                            DebugError("Error occurred during trade.OnClose()! " + e);
                            throw;
                        }
                    }
                }
            });

            pollThread.Start();
        }

        private bool CheckTradeTimeout (Trade trade)
        {
            // User has accepted the trade. Disregard time out.
            if (trade.OtherUserAccepted)
                return false;

            var now = DateTime.Now;

            DateTime actionTimeout = _lastOtherActionTime.AddSeconds (MaxActionGapSec);
            int untilActionTimeout = (int)Math.Round ((actionTimeout - now).TotalSeconds);

            Log.Debug(String.Format ("{0} {1}", actionTimeout, untilActionTimeout));

            DateTime tradeTimeout = _tradeStartTime.AddSeconds (MaxTradeTimeSec);
            int untilTradeTimeout = (int)Math.Round ((tradeTimeout - now).TotalSeconds);

            double secsSinceLastTimeoutMessage = (now - _lastTimeoutMessage).TotalSeconds;

            if (untilActionTimeout <= 0 || untilTradeTimeout <= 0)
            {
                Log.Debug("timed out...");

                if (OnTimeout != null)
                {
                    OnTimeout (this, null);
                }

                trade.CancelTrade ();

                return true;
            }
            else if (untilActionTimeout <= 20 && secsSinceLastTimeoutMessage >= 10)
            {
                try
                {
                    trade.SendMessage("Are You AFK? The trade will be canceled in " + untilActionTimeout + " seconds if you don't do something.");
                }
                catch { }
                _lastTimeoutMessage = now;
            }
            return false;
        }

        private static void DebugError(string output)
        {
            System.Console.WriteLine(output);
        }
    }
}

