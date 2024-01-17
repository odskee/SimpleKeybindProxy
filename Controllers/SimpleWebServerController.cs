using Microsoft.Extensions.Logging;
using SimpleKeybindProxy.Controllers;
using SimpleKeybindProxy.Interfaces;
using SimpleKeybindProxy.Models;
using SimpleKeybindProxy.Models.SocketRequest;
using SimpleKeybindProxy.Models.SocketResponse;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Controllers.SimpleWebService
{
    public partial class HttpServer
    {
        public partial Task<bool> RegisterLandingSitesAsync();
        public partial Task StartContext();
        public partial Task HandleIncomingConnectionsAsync(HttpListenerContext CTX);
        public partial Task<ServerCommandResponse> ProcessCommandRequestAsync(CommandRequest commandRequest);
        public partial Task<bool> SendDataOverSocketAsync(string TextToSend, string Address = "", ConnectedWebSocket? ConnectedSocket = null);
        public partial Task<bool> CloseWebSocketConnectionAsync(string Id = "", string Address = "", string Reason = "");
        public partial Task<bool> SetPageDataOnRequestAsync(string RequestedLandingSiteResource);
        public partial ConnectedWebSocket? GetConnectedWebSocket(string Address = "", string Id = "");
        public partial Task RegisterNewSocketConnectionAsync(ConnectedWebSocket NewSocket);
        public partial bool CanMakeNewSocketConnection();
        public partial List<string> GetListOfLandingSites();
    }

    public partial class HttpServer : ISimpleWebServerController
    {
        public HttpListener Listener { get; set; }
        public string PageData;
        public string LandingSiteLocation { get; set; }
        public ICollection<string> LandSiteLocations { get; set; }
        public IKeyBindController BindController { get; set; }
        public IOutputController OutputController { get; set; }

        public bool ProcessWebSocketConnectionEnabled { get; set; }
        public bool HandleIncomingConnectionsEnabled { get; set; }


        // HTTP server
        private byte[] DataEncoded { get; set; }
        private bool PageDataSet = false;

        //// Web Socket
        private ICollection<ConnectedWebSocket> ConnectedWebSocketList { get; set; }
        private const int receiveChunkSize = 256;
        private const int sendChunkSize = 256;
        private int RequestCount = 0;



        private string LastPageDisplay { get; set; }
        private readonly ILogger logger;
        private readonly ProgramOptionsController ProgramOptions;


        // Constructor
        public HttpServer(ILogger<HttpServer> _logger, IKeyBindController keybindController, IProgramOptionsController _programOptionsController, IOutputController _outputController)
        {
            Listener = new HttpListener();
            BindController = keybindController;
            LandSiteLocations = new HashSet<string>();
            ProgramOptions = (ProgramOptionsController)_programOptionsController;
            logger = _logger;
            OutputController = _outputController;

            // Enable continuous listening
            ProcessWebSocketConnectionEnabled = true;
            HandleIncomingConnectionsEnabled = true;
            ConnectedWebSocketList = new List<ConnectedWebSocket>();
        }




        // Configuration
        // -------------------

        // Enumerates and registers the Landing Sites
        public partial async Task<bool> RegisterLandingSitesAsync()
        {
            try
            {
                if (!Directory.Exists(LandingSiteLocation))
                {
                    logger.LogError("Landing Directory is either not set or cannot be found");
                    return false;
                }

                IEnumerable<string> landingSites = Directory.EnumerateDirectories(LandingSiteLocation);

                foreach (string ls in landingSites)
                {
                    LandSiteLocations.Add(ls);
                    logger.LogDebug("Landing Directory Added: {0}", ls);

                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(1, ex, "Failed to load / access Landing Directory");
                return false;
            }

            logger.LogInformation("All Landing Sites Registered");

            return true;
        }

        // Sets the Options property
        public void SetProgramOptions(ProgramOptions options) { ProgramOptions.ProgramOptions = options; LandingSiteLocation = options.LandingDir; }





        // Handle Connections
        // -------------------


        // Listen for connections
        public partial async Task StartContext()
        {
            try
            {
                while (HandleIncomingConnectionsEnabled)
                {
                    var ctx = await Listener.GetContextAsync();
                    Task worker = Task.Run(() => HandleIncomingConnectionsAsync(ctx));
                }
            }
            catch (Exception ex)
            {
                OutputController.StandardOutput(ex, "Couldn't start Context!");
                logger.LogCritical(1, ex, "");
            }
        }



        // Handles incoming connections from the web server and responds accordingly.
        // Requests to certain endpoints will result in calls to KeyBindController.
        public partial async Task HandleIncomingConnectionsAsync(HttpListenerContext CTX)
        {
            HttpListenerRequest? HttpRequest;
            HttpListenerResponse? HttpResponse;

            try
            {
                // Peel out the requests and response objects
                HttpRequest = CTX.Request;
                HttpResponse = CTX.Response;

                if (HttpRequest != null)
                {
                    // Bypass / refuse any favicon requests
                    if (HttpRequest.Url.AbsolutePath.Contains("favicon"))
                    {
                        HttpResponse.StatusCode = 404;
                        HttpResponse.Close();
                    }

                    else if (HttpRequest.IsWebSocketRequest)
                    {
                        await ProcessWebSocketConnectionAsync(CTX);
                    }

                    // Request if for a specific resource (non-generic route)
                    else if (!HttpRequest.Url.AbsolutePath.EndsWith("/") && !HttpRequest.Url.AbsolutePath.Contains("."))
                    {
                        // If requesting a landing site without an ending '/', relative URL linking in the html file doesn't work
                        string newURI = HttpRequest.Url + "/";
                        HttpResponse.Redirect(HttpRequest.Url + "/");
                        HttpResponse.Close();
                    }

                    // standard route request (no resource directly requested), identify and server correct landing site
                    else
                    {
                        HttpResponse.StatusCode = 200;
                        HttpResponse.AddHeader("Content-Type", "application/x-www-form-urlencoded");

                        // Detect and process Request query
                        if (HttpRequest.Url.Query.Length > 2)
                        {
                            await ProcessRequestQuery(HttpRequest);

                        }


                        // Issue Console / log Output
                        OutputController.DebugOutput("Resource Requested: {0} by {1}", HttpRequest.Url.AbsolutePath, HttpRequest.RemoteEndPoint);
                        logger.LogInformation("Resource Requested: {0} by {1}", HttpRequest.Url.AbsolutePath, HttpRequest.RemoteEndPoint);


                        // Set page data to respond with based on request
                        if (!await SetPageDataOnRequestAsync(HttpRequest.Url.AbsolutePath))
                        {
                            PageDataSet = false;
                        }


                        // Figure out content type to set based on request
                        if (!SetContentTypeFromRequest(HttpRequest.Url.AbsolutePath, HttpResponse))
                        {
                            HttpResponse.ContentType = "text/html";
                        }

                        if (!PageDataSet)
                        {
                            if (!string.IsNullOrEmpty(PageData))
                            {
                                DataEncoded = Encoding.UTF8.GetBytes(PageData);
                            }
                            else if (!string.IsNullOrEmpty(LastPageDisplay))
                            {
                                DataEncoded = Encoding.UTF8.GetBytes(LastPageDisplay);
                            }
                            else
                            {
                                DataEncoded = Encoding.UTF8.GetBytes("ERROR");
                            }
                        }

                        // Set headers and Write out to the response stream (asynchronously), then close it
                        try
                        {
                            if (!HttpRequest.IsWebSocketRequest)
                            {
                                HttpResponse.ContentEncoding = Encoding.UTF8;
                                HttpResponse.ContentLength64 = DataEncoded.LongLength;
                                HttpResponse.KeepAlive = true;
                                await HttpResponse.OutputStream.WriteAsync(DataEncoded, 0, DataEncoded.Length);
                            }
                        }
                        catch (HttpListenerException ex)
                        {
                            OutputController.StandardOutput("General Error, see Log for more information");
                            logger.LogCritical(1, ex, "General Error");
                            //HttpResponse.StatusCode = 500;
                        }
                        PageDataSet = false;

                        HttpResponse.Close();
                    }
                }
            }

            catch (Exception ex)
            {
                OutputController.StandardOutput("General Error, see log for more information");
                logger.LogCritical(1, ex, "Error Serving Request");
                CTX.Response.Close();
            }
        }



        // Processes an incoming web socket request / connection
        public async Task<bool> ProcessWebSocketConnectionAsync(HttpListenerContext CTX)
        {
            // If the request is a web socket protocol
            HttpListenerRequest? HttpRequest = CTX.Request;
            HttpListenerResponse? HttpResponse = CTX.Response;

            WebSocketContext webSocketContext;
            WebSocket? webSocket = null;
            ConnectedWebSocket connectedWebSocket = null;

            if (CTX != null)
            {
                if (HttpRequest != null && HttpRequest.IsWebSocketRequest)
                {
                    try
                    {
                        if (HttpRequest.HttpMethod == null)
                        {
                            OutputController.StandardOutput("HttpMethod Null!");
                        }
                        if (!CanMakeNewSocketConnection())
                        {
                            OutputController.StandardOutput("Max number of socket connections reached");
                            logger.LogWarning("Max number of web socket connections reached");
                            return false;
                        }

                        webSocketContext = await CTX.AcceptWebSocketAsync(subProtocol: null, receiveChunkSize, TimeSpan.FromSeconds(15));
                        webSocket = webSocketContext.WebSocket;

                        // add to socket list
                        string socketId = Guid.NewGuid().ToString();
                        connectedWebSocket = new ConnectedWebSocket() { SocketContext = webSocketContext, Id = socketId, Address = CTX.Request.RemoteEndPoint.ToString(), EndPoint = CTX.Request.RemoteEndPoint };
                        await RegisterNewSocketConnectionAsync(connectedWebSocket);


                        // Say Hello
                        SocketConnectedResponse socketConnectedResponse = new SocketConnectedResponse() { Id = socketId, Message = "Server Says Hello" };
                        await SendDataOverSocketAsync(JsonSerializer.Serialize<SocketConnectedResponse>(socketConnectedResponse), ConnectedSocket: connectedWebSocket);

                    }
                    catch (Exception ex)
                    {
                        OutputController.StandardOutput(ex, ex.Message);
                        logger.LogCritical(1, ex, "");
                    }

                    OutputController.StandardOutput("Upgrading to web socket");

                    // Connected
                    Interlocked.Increment(ref RequestCount);
                    OutputController.StandardOutput("Web Socket Connection(s): {0}", RequestCount);



                    try
                    {

                        CancellationTokenSource receiveCancelToken = new CancellationTokenSource();
                        byte[] receiveBuffer = new byte[receiveChunkSize];

                        while (webSocket.State == WebSocketState.Open)
                        {
                            // Receive the next set of data.
                            ArraySegment<byte> arrayBuffer = new ArraySegment<byte>(receiveBuffer);
                            WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(arrayBuffer, receiveCancelToken.Token);

                            // If the connection has been closed.
                            if (receiveResult.MessageType == WebSocketMessageType.Close)
                            {
                                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                                HttpResponse.Close();
                                //break;
                            }
                            else
                            {
                                // Start conversation.
                                var dr = System.Text.Encoding.Default.GetString(arrayBuffer);
                                string dataReceived = dr.Replace("\0", string.Empty);
                                OutputController.StandardOutput("Received Data: " + dataReceived);
                                CommandRequest? reqData = new CommandRequest();

                                if (!string.IsNullOrEmpty(dataReceived))
                                {
                                    if (dataReceived.Contains("SocketTest"))
                                    {
                                        await SendDataOverSocketAsync("Socket Test Successful", ConnectedSocket: connectedWebSocket);
                                    }
                                    else
                                    {
                                        reqData = JsonSerializer.Deserialize<CommandRequest>(dataReceived);
                                        if (reqData != null)
                                        {
                                            ServerCommandResponse commandResponse = await ProcessCommandRequestAsync(reqData);

                                            if (commandResponse.Command != null)
                                            {
                                                try
                                                {
                                                    string toJson = JsonSerializer.Serialize<ServerCommandResponse>(commandResponse);
                                                    if (!string.IsNullOrEmpty(toJson))
                                                    {
                                                        await SendDataOverSocketAsync(toJson, ConnectedSocket: connectedWebSocket);
                                                    }

                                                }
                                                catch (Exception ex)
                                                {
                                                    OutputController.StandardOutput("Something went wrong responding through web socket");
                                                    logger.LogCritical(1, ex, "Error in ProcessWebSocketConnectionAsync");
                                                    return false;
                                                }

                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        OutputController.StandardOutput(ex, ex.Message);
                        logger.LogCritical(1, ex, "Error accepting websocket");
                    }
                }
            }

            return true;
        }



        // Sends the specified JSON over an active web socket connection
        public partial async Task<bool> SendDataOverSocketAsync(string TextToSend, string Address = "", ConnectedWebSocket? ConnectedSocket = null)
        {
            if (ConnectedSocket == null)
            {
                if (string.IsNullOrEmpty(Address))
                    return false;

                ConnectedSocket = ConnectedWebSocketList.First(a => a.Address.Equals(Address));
            }
            if (ConnectedSocket != null && ConnectedSocket.SocketContext.WebSocket != null)
            {
                WebSocket webSocket = ConnectedSocket.SocketContext.WebSocket;

                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    byte[] sendBuffer = new byte[sendChunkSize];
                    ICollection<SocketResponseChunk> responseChunks = new List<SocketResponseChunk>();
                    int IdStart = 1;

                    int TextToSendLength = TextToSend.Length;



                    int TotalChunks = (TextToSend.Length / sendChunkSize) + ((TextToSend.Length % sendChunkSize) - ((TextToSend.Length % sendChunkSize) - 1));
                    int StartPosition = 0;
                    int CharCount = sendChunkSize;

                    for (int i = 1; i <= TotalChunks; i++)
                    {
                        TextToSendLength = TextToSend.Length;
                        if (sendChunkSize * i > TextToSendLength)
                        {
                            CharCount = TextToSendLength - (sendChunkSize * (i - 1));
                        }

                        responseChunks.Add(new SocketResponseChunk() { ChunkValue = TextToSend.Substring(StartPosition, CharCount), Id = IdStart, TotalChunks = TotalChunks });
                        StartPosition += sendChunkSize;

                    }

                    foreach (SocketResponseChunk chunk in responseChunks)
                    {

                        sendBuffer = Encoding.UTF8.GetBytes(chunk.ChunkValue);
                        try
                        {

                            if (chunk.Id == chunk.TotalChunks)
                            {
                                await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                            else
                            {
                                await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogCritical(1, ex, "Problem sending socket data");
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }



        // Closes and active web socket connection
        public partial async Task<bool> CloseWebSocketConnectionAsync(string Id = "", string Address = "", string Reason = "")
        {
            ConnectedWebSocket? connectedSocket = ConnectedWebSocketList.First(a => a.Id.Equals(Id) || a.Address.Equals(Address));
            if (connectedSocket != null)
            {
                if (connectedSocket.SocketContext.WebSocket != null)
                {
                    await connectedSocket.SocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, Reason, CancellationToken.None);
                    //webSocket.Dispose();
                }
                return true;
            }
            return false;
        }




        // Sets the page data based on a requested URI.  Assumes the URI incoming is valid.
        public partial async Task<bool> SetPageDataOnRequestAsync(string RequestedLandingSiteResource)
        {
            try
            {
                string[] requestBreakdown = RequestedLandingSiteResource.Split('/');
                requestBreakdown = requestBreakdown.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                string rsource = "";


                if (requestBreakdown.Length > 0 && LandSiteLocations.Any(ls => ls.Contains(requestBreakdown[0])))
                {
                    string pTerminal = "/";
                    foreach (string rq in requestBreakdown.Where(a => LandSiteLocations.Any(b => b.Split("\\").Last().Equals(a)) == false))
                    {
                        pTerminal += rq + "/";
                    }
                    rsource = LandSiteLocations.FirstOrDefault(a => a.Contains(requestBreakdown[0])).Replace("\\", "/") + pTerminal;
                    rsource = rsource.Remove(rsource.Length - 1, 1);
                }

                if (String.IsNullOrEmpty(rsource))
                {
                    // No matching pages could be found, show generic page
                    if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT").Equals("debug"))
                    {
                        rsource = "..\\..\\..\\HTML\\Landing.html";
                    }
                    else
                    {
                        rsource = ".\\HTML\\Landing.html";
                    }
                }

                if (!rsource.Split("/").Last().Contains("."))
                {
                    rsource = rsource + "/" + rsource.Split("/").Last() + ".html";
                }

                try
                {
                    if (File.Exists(rsource))
                    {
                        // Do we need to ReadBytes or ReadText?
                        DataEncoded = await File.ReadAllBytesAsync(rsource);
                        PageDataSet = true;
                        logger.LogDebug("File found and contents read: {0}", rsource);
                    }
                    else
                    {
                        PageData = "<HTML><HEAD><meta charset=\"utf-8\"><title>SimpleKeybindProxy</title></head><body><h1>Program Error</h1></body></html>";
                        DataEncoded = Encoding.UTF8.GetBytes(PageData);
                        logger.LogDebug("No resource data could be loaded");
                        return false;
                    }

                }
                catch (Exception ex)
                {
                    OutputController.StandardOutput("Error loading resource, See log for further info");
                    logger.LogCritical(1, ex, "Error loading resource");
                    return false;
                }

                if (rsource.Contains(".html") && !rsource.Contains("Error.html"))
                {
                    LastPageDisplay = PageData;
                }

                return true;
            }
            catch (Exception ex)
            {
                OutputController.StandardOutput("General Error, See log for further info");
                logger.LogCritical(1, ex, "General error finding resource");
                return false;
            }
        }



        // Processes a Command Request
        public partial async Task<ServerCommandResponse> ProcessCommandRequestAsync(CommandRequest commandRequest)
        {
            ServerCommandResponse serverResponse = new ServerCommandResponse() { Command = commandRequest };
            if (commandRequest != null && !string.IsNullOrEmpty(commandRequest.Command))
            {
                if (commandRequest.Command.ToLower().Contains("keybind"))
                {
                    serverResponse.CommandSuccess = true;
                    if (!string.IsNullOrEmpty(commandRequest?.CommandData?.FirstOrDefault()))
                    {
                        KeybindResponse? kbResp = new KeybindResponse();
                        kbResp = await BindController.ProcessKeyBindRequestAsync(commandRequest);

                        if (!kbResp.Success)
                        {
                            // Problem occured during the keybind issue request
                            OutputController.StandardOutput("A problem occurred trying to issue your requested keybind: {0}", commandRequest?.CommandData?.FirstOrDefault());

                        }
                        else
                        {
                            // Process received response
                            serverResponse.Command = commandRequest; serverResponse.BindCommandResponse = kbResp;
                            serverResponse.CommandSuccess = true;
                            serverResponse.Message = "Command was successfully processed";
                            return serverResponse;
                        }
                    }
                    else
                    {
                        serverResponse.CommandSuccess = false;
                        serverResponse.Message = "CommandData was not valid";
                    }
                }
                else
                {
                    serverResponse.CommandSuccess = false;
                    serverResponse.Message = "Unknown Command Request";
                }
            }

            return serverResponse;
        }



        // Inspects and processes possible query arguments
        private async Task<bool> ProcessRequestQuery(HttpListenerRequest HttpRequest)
        {
            if (HttpRequest?.Url != null)
            {
                // Determine Request Landing Site / Resource
                if (HttpRequest.Url.Query.Length > 2)
                {
                    // We have some query attached to the request
                    string CommandToExecute = "";
                    string CommandData = "";
                    string queryTidy = HttpRequest.Url.Query;
                    if (HttpRequest.Url.Query.StartsWith("?"))
                    {
                        queryTidy = HttpRequest.Url.Query.Replace("?", "");
                    }

                    string[] queryBreakdown = queryTidy.Split("&");

                    // Check for a command attribute
                    if (queryBreakdown.Any(a => a.Split("=").Any(a => a.ToLower().Equals("command"))))
                    {
                        // Command found, extract command and data
                        CommandToExecute = queryBreakdown.FirstOrDefault(a => a.Split("=").Any(a => a.ToLower().Equals("command"))).Split("=")[1];
                        if (queryBreakdown.Any(a => a.Split("=").Any(a => a.ToLower().Equals("commanddata"))))
                        {
                            CommandData = queryBreakdown.FirstOrDefault(a => a.Split("=").Any(a => a.ToLower().Equals("commanddata"))).Split("=")[1];
                        }
                    }

                    CommandRequest builtCommand = new CommandRequest() { Command = CommandToExecute, CommandData = new List<string>() { CommandData } };


                    _ = await ProcessCommandRequestAsync(builtCommand);


                }
            }

            return true;
        }



        // Returns a matching ConnectedWebSocket object
        public partial ConnectedWebSocket? GetConnectedWebSocket(string Address = "", string Id = "")
        {
            ConnectedWebSocket? foundSocket = null;

            if (ConnectedWebSocketList.Count > 0)
            {
                if (string.IsNullOrEmpty(Address) && string.IsNullOrEmpty(Id))
                {
                    foundSocket = ConnectedWebSocketList.Last();
                }
                else
                {
                    foundSocket = ConnectedWebSocketList.LastOrDefault(a => a.Address.StartsWith(Address) || a.Id.Equals(Id));
                }
            }

            return foundSocket;
        }



        // Registers a new Web Socket connection
        public partial async Task RegisterNewSocketConnectionAsync(ConnectedWebSocket NewSocket)
        {
            if (CanMakeNewSocketConnection())
            {
                ConnectedWebSocketList.Add(NewSocket);
            }

            //TODO: Callback action
        }



        // Determines if any further connections can be made
        public partial bool CanMakeNewSocketConnection()
        {
            if (ProgramOptions.ProgramOptions.MaxSocketConnections != 0 && ConnectedWebSocketList.Count >= ProgramOptions.ProgramOptions.MaxSocketConnections)
            {
                return false;
            }

            return true;
        }







        // Helpers
        // -------------------


        // Returns a list of Landing Sites
        public partial List<string> GetListOfLandingSites()
        {
            return LandSiteLocations.ToList();
        }



        // Sets the HTTP response content type based on the request made
        private bool SetContentTypeFromRequest(string Request, HttpListenerResponse HttpResponse)
        {
            if (Request.Contains(".css"))
            {
                HttpResponse.ContentType = "text/css";
                return true;
            }
            else if (Request.Contains("."))
            {
                switch (Request.Split(".").Last())
                {
                    case "png":
                    HttpResponse.ContentType = "image/png";
                    return true;

                    case "js":
                    HttpResponse.ContentType = "text/javascript";
                    return true;

                    case "gif":
                    HttpResponse.ContentType = "image/gif";
                    return true;

                    case "jpeg":
                    HttpResponse.ContentType = "image/jpeg ";
                    return true;

                    case "jpg":
                    HttpResponse.ContentType = "image/jpeg ";
                    return true;

                    case "svg":
                    HttpResponse.ContentType = "image/svg+xml";
                    return true;

                    default:
                    HttpResponse.ContentType = "text/html";
                    return true;
                }
            }

            return false;
        }


    }
}