using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreasureHunter.Service
{
    public static class Token
    {
        public static string GenerateToken()
        {
            string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            return token;
        }
    }
}
