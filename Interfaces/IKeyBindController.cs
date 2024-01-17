using SimpleKeybindProxy.Models.SocketRequest;
using SimpleKeybindProxy.Models.SocketResponse;

namespace SimpleKeybindProxy.Interfaces
{
    public partial interface IKeyBindController
    {
        public Task<bool> LoadKeyBindLibraryAsync();
        public Task<KeybindResponse> ProcessKeyBindRequestAsync(CommandRequest RequestedKeybindCommand);


    }
}
