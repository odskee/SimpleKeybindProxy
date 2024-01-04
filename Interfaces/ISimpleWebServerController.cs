namespace SimpleKeybindProxy.Interfaces
{
    public partial interface ISimpleWebServerController
    {
        public Task HandleIncomingConnectionsAsync();
        public Task<bool> SetPageDataOnRequestAsync(string RequestedLandingSiteResource);

    }
}
