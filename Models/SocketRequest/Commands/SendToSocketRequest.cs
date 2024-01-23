namespace SimpleKeybindProxy.Models.SocketRequest.Commands
{
    public class SendToSocketRequest : CommandRequest
    {
        public string DestinationName { get; set; }
        public object Message { get; set; }
    }

}
