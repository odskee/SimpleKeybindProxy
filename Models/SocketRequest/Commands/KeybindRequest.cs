namespace SimpleKeybindProxy.Models.SocketRequest.Commands
{
    public class KeybindRequest : CommandRequest
    {
        public string BindName { get; set; }
        public string PressType { get; set; }
    }
}
