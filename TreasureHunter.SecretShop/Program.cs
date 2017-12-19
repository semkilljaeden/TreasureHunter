using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Routing;
using Akka.Util.Internal;
using log4net;
using TreasureHunter.Contract.AkkaMessageObject;
using TreasureHunter.Service;


namespace TreasureHunter.SecretShop
{
    class Program
    {
        public static void Main()
        {
            var disableJsonWarning = ConfigurationFactory.ParseString(@"
                    akka {
                            suppress-json-serializer-warning = on
                         }");
            var system = ActorSystem.Create("TreasureHunter", disableJsonWarning);
            var list = new List<IActorRef>();
            var commander = system.ActorOf(CommandActor.Props(list), "Commander");
            var account = system.ActorOf(AccountMonitor.Props(commander), "Account");
            account.Tell(new ScheduleMessage());
            do
            {
                Console.Write("botmgr > ");
                string inputText = Console.ReadLine();

                if (!string.IsNullOrEmpty(inputText))
                    commander.Tell(new UserCommandMessage()
                    {
                        Text = inputText
                    });

            } while (true);
        }
    }
}
