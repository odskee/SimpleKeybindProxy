namespace SimpleKeybindProxy.Models.SocketResponse.Commands
{
    public class SocketSendResponse
    {
        public string ToId { get; set; }
        public string DestinationName { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
