using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace TreasureHunter.DataAccess
{
    class Content
    {
        public string Id { get; set; }
        public Type ContentType { get; set; }
        public ActorPath BotPath { get; set; }
        public dynamic Object { get; set; }

        public string GetDocumentId()
        {
            return BotPath + "_" + ContentType + "_" + Id;
        }
    }
}
