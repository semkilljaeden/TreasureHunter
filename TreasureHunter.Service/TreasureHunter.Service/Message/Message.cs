namespace TreasureHunter.Service.Message
{
    public class Message
    {
        public enum MessageType { Start, Exec, Input }
        public MessageType Type { get; set; }
        public string MessageText { get; set; }
    }
}
