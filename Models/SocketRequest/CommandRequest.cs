namespace SimpleKeybindProxy.Models.SocketRequest
{
    public class CommandRequest
    {
        public string Id { get; set; }
        public string? RequesterName { get; set; }

        public string Command { get; set; }

    }
}
