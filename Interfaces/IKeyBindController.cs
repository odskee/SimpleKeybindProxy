using static SimpleKeybindProxy.Controllers.KeyBindController;

namespace SimpleKeybindProxy.Interfaces
{
    public partial interface IKeyBindController
    {
        public Task<bool> LoadKeyBindLibraryAsync();
        public Task<bool> ProcessKeyBindRequestAsync(string RequestedBindName, KeypressType KeypressRequestType);


    }
}
