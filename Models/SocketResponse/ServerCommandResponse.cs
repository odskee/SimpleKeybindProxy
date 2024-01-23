namespace SimpleKeybindProxy.Models.SocketResponse
{
    public class ServerCommandResponse
    {
        public string Id { get; set; }
        public object Command { get; set; }
        public bool CommandSuccess { get; set; }
        public string Message { get; set; }

        public object? CommandResponse { get; set; }
    }
}
