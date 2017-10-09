using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Akka.Util.Internal;
using log4net;
using SteamTrade;

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
            }
            _system = ActorSystem.Create("TreasureHunter");
            var paymentActor = _system.ActorOf(PaymentActor.Props());
            var valuationActor = _system.ActorOf(ValuationActor.Props());
            var routees = config.Bots.Select(bot => _system.ActorOf(BotActor.Props(bot, config.ApiKey, (x, y) => new CustomUserHandler(x, y), paymentActor, valuationActor), bot.DisplayName)).ToList();
            Schema.Init(config.ApiKey);
            Commander commander = new Commander(routees);
            do
            {
                Console.Write("botmgr > ");
                string inputText = Console.ReadLine();

                if (!String.IsNullOrEmpty(inputText))
                    commander.CommandInterpreter(inputText);

            } while (true);
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
