using SimpleKeybindProxy.Models.SocketRequest.Commands;
using SimpleKeybindProxy.Models.SocketResponse.Commands;

namespace SimpleKeybindProxy.Interfaces
{
    public partial interface IKeyBindController
    {
        public Task<bool> LoadKeyBindLibraryAsync();
        public Task<KeybindResponse> ProcessKeyBindRequestAsync(KeybindRequest RequestedKeybindCommand);


    }
}
