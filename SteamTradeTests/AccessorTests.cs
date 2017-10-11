using NUnit.Framework;
using TreasureHunter.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace TreasureHunter.DataAccess.Tests
{
    [TestFixture()]
    public class AccessorTests
    {
        [Test()]
        public void RunTest()
        {
            try
            {
                var section = ConfigurationManager.GetSection("Couchbase");
                new Accessor().Init();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}