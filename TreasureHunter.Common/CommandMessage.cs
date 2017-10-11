namespace TreasureHunter.Common
{
    public class CommandMessage
    {
        public enum MessageType { Start, Exec, Input, ReleaseItem }
        public MessageType Type { get; set; }
        public string MessageText { get; set; }
    }
}
