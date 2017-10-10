using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using log4net;
using TreasureHunter.Common;
namespace TreasureHunter.Service
{
    class CommandActor : ReceiveActor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CommandActor));
        private readonly CommandSet _p;
        private string _start = String.Empty;
        private string _stop = String.Empty;
        private bool _showHelp;
        private bool _clearConsole;
        private readonly List<IActorRef> _routees;
        public CommandActor(List<IActorRef> routees)
        {
            _routees = routees;
            Receive<UserCommandMessage>(msg => CommandInterpreter(msg.Text));
            Receive<ActorCommandMessage>(msg => HandleActorCommand(msg.Text));
            _p = new CommandSet
            {
                new BotManagerOption("stop", "stop (X) where X = the username or index of the configured bot",
                    s => _stop = s),
                new BotManagerOption("start", "start (X) where X = the username or index of the configured bot",
                    StartBot),
                new BotManagerOption("help", "shows this help text", ShowHelp),
                new BotManagerOption("show",
                    "show (x) where x is one of the following: index, \"bots\", or empty",
                    ShowCommand),
                new BotManagerOption("clear", "clears this console", s => _clearConsole = s != null),
                new BotManagerOption("auth", "auth (X)=(Y) where X = the username or index of the configured bot and Y = the steamguard code",
                    AuthSet),
                new BotManagerOption("exec",
                    "exec (X) (Y) where X = the username or index of the bot and Y = your custom command to execute",
                    ExecCommand),
                new BotManagerOption("release",
                    "input (X) (Y) where X = the username or index of the bot and Y = your input",
                    ReleaseItem),

            };
        }

        public void Init()
        {
            do
            {
                Console.Write("botmgr > ");
                string inputText = Console.ReadLine();

                if (!String.IsNullOrEmpty(inputText))
                    commander.Tell(new UserCommandMessage()
                    {
                        Text = inputText
                    });

            } while (true);
        }
        public void HandleActorCommand(string msg)
        {
            Log.Info(msg);
            var reply = Console.ReadLine();
            Sender.Tell(new ActorCommandMessage()
            {
                Reply = reply
            });
        }

        public static Props Props(List<IActorRef> routees)
        {
            return Akka.Actor.Props.Create(() => new CommandActor(routees));
        }

        #region Command Handler

        void ReleaseItem(string auth)
        {
            var actor = _routees.FirstOrDefault(r => r.Path.Name == "Payment");
            if (actor == null)
            {
                Log.Error($"Cannot find Payment Service");
                return;
            }
            actor?.Tell(new CommandMessage()
            {
                Type = CommandMessage.MessageType.ReleaseItem,
                MessageText = auth
            });
            Log.Info($"Release Trade with Token {auth}");

        }
        void ShowHelp(string auth)
        {
            Console.WriteLine("");
            _p.WriteOptionDescriptions(Console.Out);
        }

        void StartBot(string auth)
        {
            if (string.IsNullOrEmpty(auth))
            {
                _routees.ForEach(r => r.Tell(new CommandMessage()
                {
                    Type = CommandMessage.MessageType.Start,
                }));
            }
            else
            {
                var routee = _routees.FirstOrDefault(r => r.Path.Name == auth);
                if (routee == null)
                {
                    Console.WriteLine("Wrong bot name");
                    return;
                }
                routee.Tell(new CommandMessage()
                {
                    Type = CommandMessage.MessageType.Start,
                });
            }

        }

        void AuthSet(string auth)
        {
            string[] xy = auth.Split('=');

            //TBI
        }


        /// <summary>
        /// This interprets the given command string.
        /// </summary>
        /// <param name="command">The entire command string.</param>
        public void CommandInterpreter(string command)
        {
            _showHelp = false;
            _start = null;
            _stop = null;

            _p.Parse(command);

            if (_showHelp)
            {
                Console.WriteLine("");
                _p.WriteOptionDescriptions(Console.Out);
            }
        }

        private void ShowCommand(string param)
        {
            param = param.Trim();
        }

        private void ExecCommand(string cmd)
        {
            cmd = cmd.Trim();

            var cs = cmd.Split(' ');

            try
            {
                if (cs.Length < 2)
                {
                    Log.Error("Error: No command given to be executed.");
                    return;
                }

                // Take the rest of the input as is
                var command = cmd.Remove(0, cs[0].Length + 1);

                _routees.FirstOrDefault(r => r.Path.Name == cs[0])?.Tell(new CommandMessage()
                {
                    Type = CommandMessage.MessageType.Exec,
                    MessageText = command
                });
            }
            catch (Exception e)
            {
                // Print error
                Log.Error("Error: Bot " + cs[0] + " not found.");
                throw;
            }

        }


        #endregion


        #region Nested Options classes
        // these are very much like the NDesk.Options but without the
        // maturity, features or need for command seprators like "-" or "/"

        private class BotManagerOption
        {
            public string Name { get; set; }
            public string Help { get; set; }
            public Action<string> Func { get; set; }

            public BotManagerOption(string name, string help, Action<string> func)
            {
                Name = name;
                Help = help;
                Func = func;
            }
        }

        private class CommandSet : KeyedCollection<string, BotManagerOption>
        {
            protected override string GetKeyForItem(BotManagerOption item)
            {
                return item.Name;
            }

            public void Parse(string commandLine)
            {
                var c = commandLine.Trim();

                var cs = c.Split(' ');

                foreach (var option in this)
                {
                    if (cs[0].Equals(option.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (cs.Length > 2)
                        {
                            option.Func(c.Remove(0, cs[0].Length + 1));
                        }
                        else if (cs.Length > 1)
                        {
                            option.Func(cs[1]);
                        }
                        else
                        {
                            option.Func(String.Empty);
                        }
                    }
                }
            }

            public void WriteOptionDescriptions(TextWriter o)
            {
                foreach (BotManagerOption p in this)
                {
                    o.Write('\t');
                    o.WriteLine(p.Name + '\t' + p.Help);
                }
            }
        }

        #endregion Nested Options classes
    }
}
