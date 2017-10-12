namespace TreasureHunter.Contract.AkkaMessageObject
{
    public enum DataAccessActionType
    {
        UpdateTradeOffer,
        Retrieve,
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
