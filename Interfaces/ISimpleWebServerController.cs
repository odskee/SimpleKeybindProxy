using SimpleKeybindProxy.Models;
using SimpleKeybindProxy.Models.SocketRequest;
using System.Net;

namespace SimpleKeybindProxy.Interfaces
{
    public partial interface ISimpleWebServerController
    {
        public Task<bool> RegisterLandingSitesAsync();
        public Task StartContext();
        public Task HandleIncomingConnectionsAsync(HttpListenerContext CTX);
        public Task<ServerCommandResponse> ProcessCommandRequestAsync(CommandRequest commandRequest);
        public Task<bool> SendDataOverSocketAsync(string TextToSend, string Address = "", ConnectedWebSocket? ConnectedSocket = null);
        public Task<bool> CloseWebSocketConnectionAsync(string Id = "", string Address = "", string Reason = "");
        public Task<bool> SetPageDataOnRequestAsync(string RequestedLandingSiteResource);
        public ConnectedWebSocket? GetConnectedWebSocket(string Address = "", string Id = "");
        public Task RegisterNewSocketConnectionAsync(ConnectedWebSocket NewSocket);
        public bool CanMakeNewSocketConnection();
        public List<string> GetListOfLandingSites();
    }
}
