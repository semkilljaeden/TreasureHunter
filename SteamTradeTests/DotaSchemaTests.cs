using NUnit.Framework;
using TreasureHunter.SteamTrade;

namespace SteamTradeTests
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