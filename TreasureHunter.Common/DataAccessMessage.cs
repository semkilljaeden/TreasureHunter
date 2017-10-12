using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreasureHunter.Common
{
    public enum DataAccessType { SaveTradeOffer, CheckPendingTradeOffer }
    public class DataAccessMessage<T>
    {

        private DataAccessType _type;
        private T _content;
        public DataAccessMessage(T content, DataAccessType type)
        {
            _content = content;
            _type = type;
        }
    }
}
