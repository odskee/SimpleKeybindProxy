using SimpleKeybindProxy.Models;
using SimpleKeybindProxy.Models.SocketResponse;
using SimpleKeybindProxy.Models.SocketResponse.Commands;
using System.Net;

namespace SimpleKeybindProxy.Interfaces
{
    public partial interface ISimpleWebServerController
    {
        public Task<bool> RegisterLandingSitesAsync();
        public Task StartContext();
        public Task HandleIncomingConnectionsAsync(HttpListenerContext CTX);
        public Task<ServerCommandResponse> ProcessCommandRequestAsync(object commandRequest);
        public Task<ServerCommandResponse> CheckForAdditionalCommands(ServerCommandResponse serverResponse, object commandRequest);
        public Task<bool> SendDataOverSocketAsync(string ToSend, string Address);
        public Task<bool> SendDataOverSocketAsync(string ToSend, ConnectedWebSocket ConnectedSocket);
        public Task<int> CloseWebSocketConnectionAsync(string Id = "", string Address = "", string Reason = "");
        public Task<bool> SetPageDataOnRequestAsync(string RequestedLandingSiteResource);
        public List<ConnectedWebSocket>? GetConnectedWebSockets(string Address = "", string Id = "", string RegisteredName = "");
        public Task RegisterNewSocketConnectionAsync(ConnectedWebSocket NewSocket);
        public bool CanMakeNewSocketConnection();
        public List<string> GetListOfLandingSites();



        public Task<KeybindResponse> ProcessKeybindCommandAsync(object Request);
        public Task<RegisterWebSocketNameResponse> ProcessRegisterWebSocketCommandAsync(object Request);
        public Task<SocketSendResponse> ProcessSendToSocketCommandAsync(object Request);
    }
}
