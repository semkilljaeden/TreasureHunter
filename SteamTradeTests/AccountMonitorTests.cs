using NUnit.Framework;
using TreasureHunter.SecretShop;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreasureHunter.SecretShop.Tests
{
    [TestFixture()]
    public class AccountMonitorTests
    {
        [Test()]
        public void AccountMonitorTest()
        {
            if (!System.IO.Directory.Exists(ConfigurationManager.AppSettings["TempFolderPath"]))
            {
                System.IO.Directory.CreateDirectory(ConfigurationManager.AppSettings["TempFolderPath"]);
            }
            var m = new AccountMonitor();
            m.Run();
        }
    }
}