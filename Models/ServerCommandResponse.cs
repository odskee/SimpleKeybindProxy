using SimpleKeybindProxy.Models.SocketRequest;
using SimpleKeybindProxy.Models.SocketResponse;

namespace SimpleKeybindProxy.Models
{
    public class ServerCommandResponse
    {
        public CommandRequest Command { get; set; }
        public bool CommandSuccess { get; set; }
        public string Message { get; set; }


        public KeybindResponse? BindCommandResponse { get; set; }
    }
}
