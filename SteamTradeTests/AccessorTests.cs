using NUnit.Framework;
using TreasureHunter.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreasureHunter.DataAccess.Tests
{
    [TestFixture()]
    public class AccessorTests
    {
        [Test()]
        public void RunTest()
        {
            new Accessor().Init();
        }
    }
}