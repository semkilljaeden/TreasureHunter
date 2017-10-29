using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreasureHunter.SecretShop
{
    class Program
    {
        public static void Main()
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
