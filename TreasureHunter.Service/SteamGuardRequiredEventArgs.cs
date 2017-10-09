using System;

namespace TreasureHunter.Service
{
    public class SteamGuardRequiredEventArgs : EventArgs
    {
        /// <summary>
        /// Set this to return the Steam Guard code to the bot.
        /// </summary>
        public string SteamGuard { get; set; }
    }
}
