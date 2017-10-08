using NUnit.Framework;
using SteamTrade;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamTrade.Tests
{
    [TestFixture()]
    public class DotaSchemaTests
    {
        [Test()]
        public void FetchSchemaTest()
        {
            Schema.Init("EDC8EBC8F7158C8D8B77416F4E3B7D22");
        }
    }
}