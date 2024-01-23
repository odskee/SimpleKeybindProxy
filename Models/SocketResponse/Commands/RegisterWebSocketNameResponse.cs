namespace SimpleKeybindProxy.Models.SocketResponse.Commands
{
    public class RegisterWebSocketNameResponse
    {
        public string OldName { get; set; }
        public string NewName { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }

    }
}
