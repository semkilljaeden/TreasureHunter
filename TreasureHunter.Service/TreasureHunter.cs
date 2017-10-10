using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Akka.Util.Internal;
using log4net;
using TreasureHunter.SteamTrade;
using TreasureHunter.Common;
using TreasureHunter.SteamTrade;

namespace TreasureHunter.Service
{
    public class TreasureHunter
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(TreasureHunter));
        private static ActorSystem _system;
        public void Init()
        {
            var config = LoadConfig();
            if (config == null)
            {
                Log.Error("No config json, program exit");
                return;
            }
            _system = ActorSystem.Create("TreasureHunter");
            var actors = new List<IActorRef>();
            var paymentActor = _system.ActorOf(PaymentActor.Props(), "Payment");
            var valuationActor = _system.ActorOf(ValuationActor.Props(), "Valuation");
            var commander = _system.ActorOf(CommandActor.Props(actors), "Commander");
            var routees = config.Bots.Select(bot => _system.ActorOf(BotActor.Props(bot, config.ApiKey, (x, y) => new CustomUserHandler(x, y), paymentActor, valuationActor, commander), bot.DisplayName)).ToList();
            Schema.Init(config.ApiKey);
            actors.AddRange(routees);
            actors.Add(paymentActor);
            actors.Add(valuationActor);
            commander.Tell(new ScheduleMessage());
        }

        private Configuration LoadConfig()
        {
            Configuration configObject = null;
            if (!System.IO.File.Exists("settings.json"))
            {
                Console.WriteLine("No settings.json file found.");
                return null;
            }
            try
            {
                configObject = Configuration.LoadConfiguration("settings.json");
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                // handle basic json formatting screwups
                Console.WriteLine("settings.json file is corrupt or improperly formatted.");
                return configObject;
            }
            return configObject;
        }
    }
}
