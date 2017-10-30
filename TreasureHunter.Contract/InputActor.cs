using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using TreasureHunter.Contract.AkkaMessageObject;

namespace TreasureHunter.Contract
{
    public class InputActor : ReceiveActor
    {
        protected readonly ConcurrentQueue<string> ThreadCommunicator;
        private readonly IActorRef _commandActor;
        private readonly IActorRef _mySelf;
        public string WaitForInput(string message)
        {
            string input;
            int seconds = 20;
            _commandActor.Tell(new ActorCommandMessage()
            {
                Text = message + $" You have {seconds} seconds to enter the value"
            }, _mySelf);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (!ThreadCommunicator.TryDequeue(out input) && stopwatch.Elapsed < TimeSpan.FromSeconds(seconds))
            {

            }
            return input;
        }

        public InputActor(IActorRef commandActor)
        {
            _commandActor = commandActor;
            ThreadCommunicator = new ConcurrentQueue<string>();
            _mySelf = Self;
        }
    }
}
