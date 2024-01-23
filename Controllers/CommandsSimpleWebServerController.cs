using SimpleKeybindProxy.Interfaces;
using SimpleKeybindProxy.Models.SocketRequest.Commands;
using SimpleKeybindProxy.Models.SocketResponse;
using SimpleKeybindProxy.Models.SocketResponse.Commands;
using System.Text.Json;

namespace SimpleKeybindProxy.Controllers
{
    public partial class SimpleWebServerController : ISimpleWebServerController
    {
        ///Command Specific
        public async Task<KeybindResponse> ProcessKeybindCommandAsync(object Request)
        {
            // Command success does not equal keybind success
            KeybindRequest commandRequest = (KeybindRequest)Request;
            ServerCommandResponse serverResponse = new ServerCommandResponse() { Command = commandRequest };
            serverResponse.CommandSuccess = true;
            KeybindResponse? kbResp = new KeybindResponse();

            if (!string.IsNullOrEmpty(commandRequest.BindName))
            {
                kbResp = await BindController.ProcessKeyBindRequestAsync(commandRequest);

                if (!kbResp.Success)
                {
                    // Problem occured during the keybind issue request
                    OutputController.StandardOutput("A problem occurred trying to issue your requested keybind: {0}", commandRequest.BindName);
                }
            }
            else
            {
                serverResponse.CommandSuccess = false;
                serverResponse.Message = "CommandData was not valid";
            }

            return kbResp;
        }



        // Sets a given name for a web socket connection
        public async Task<RegisterWebSocketNameResponse> ProcessRegisterWebSocketCommandAsync(object Request)
        {
            RegisterWebSocketRequest commandRequest = (RegisterWebSocketRequest)Request;

            RegisterWebSocketNameResponse resp = new RegisterWebSocketNameResponse();
            if (!string.IsNullOrEmpty(commandRequest.RegisteredName))
            {
                string oldSocketName = ConnectedWebSocketList.FirstOrDefault(a => a.Id.Equals(commandRequest.Id))?.RegisteredName ?? "";
                ConnectedWebSocketList.Where(a => a.Id == commandRequest.Id).ToList().ForEach(a => a.RegisteredName = commandRequest.RegisteredName);
                resp = new RegisterWebSocketNameResponse() { OldName = oldSocketName, NewName = commandRequest.RegisteredName, Success = true, Message = "Name Set" };
            }
            return resp;
        }



        // Sends data from one web socket to another based on the registered name
        public async Task<SocketSendResponse> ProcessSendToSocketCommandAsync(object Request)
        {
            SendToSocketRequest commandRequest = (SendToSocketRequest)Request;
            SocketSendResponse resp = new SocketSendResponse();

            if (!string.IsNullOrEmpty(commandRequest.DestinationName))
            {
                var matchingSocketList = GetConnectedWebSockets(RegisteredName: commandRequest.DestinationName);
                if (matchingSocketList != null && matchingSocketList.Count == 1)
                {
                    MessageOverSocketResponse messageToSend = new MessageOverSocketResponse() { Message = commandRequest.Message, MessageSenderId = commandRequest.Id, MessageSenderName = commandRequest.RequesterName };
                    string messageString = JsonSerializer.Serialize<MessageOverSocketResponse>(messageToSend);

                    await SendDataOverSocketAsync(messageString, Address: matchingSocketList[0].Address);
                    resp.ToId = matchingSocketList[0].Id;
                    resp.Success = true;
                    resp.Message = "Data sent successfully";
                    resp.DestinationName = commandRequest.DestinationName;
                }
                else
                {
                    resp.Success = false;
                    resp.Message = "Couldn't find a matching destination";
                }
            }

            return resp;
        }
    }
}
