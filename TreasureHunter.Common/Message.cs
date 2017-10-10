namespace TreasureHunter.Common
{
    public class Message
    {
        public enum MessageType { Start, Exec, Input, ReleaseItem }
        public MessageType Type { get; set; }
        public string MessageText { get; set; }
    }
}
