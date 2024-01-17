using System.Net;
using System.Net.WebSockets;

namespace SimpleKeybindProxy.Models
{
    public class ConnectedWebSocket
    {
        public string Id { get; set; }
        public string Address { get; set; }
        public WebSocketContext SocketContext { get; set; }
        public IPEndPoint EndPoint { get; set; }
    }
}
