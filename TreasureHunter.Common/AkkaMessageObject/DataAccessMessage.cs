using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreasureHunter.Common
{
    public enum DataAccessActionType
    {
        UpdateTradeOffer,
        UpdatePendingTradeOffer
    }
    public class DataAccessMessage<T>
    {

        public DataAccessActionType ActionType { get; private set; }
        public T Content { get; private set; }
        public DataAccessMessage(T content, DataAccessActionType actionType)
        {
            Content = content;
            ActionType = actionType;
        }
    }
}
