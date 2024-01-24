using Microsoft.Extensions.Logging;
using SimpleKeybindProxy.Controllers.Helpers;
using SimpleKeybindProxy.Interfaces;
using SimpleKeybindProxy.Models;
using SimpleKeybindProxy.Models.SocketResponse;
using System.Buffers;
using System.Collections.Specialized;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;

namespace SimpleKeybindProxy.Controllers
{
    public partial class SimpleWebServerController
    {
        public partial Task<bool> RegisterLandingSitesAsync();
        public partial Task StartContext();
        public partial Task HandleIncomingConnectionsAsync(HttpListenerContext CTX);
        public partial Task<ServerCommandResponse> ProcessCommandRequestAsync(object commandRequest);
        public partial Task<bool> SendDataOverSocketAsync(string ToSend, string Address);
        public partial Task<bool> SendDataOverSocketAsync(string ToSend, ConnectedWebSocket ConnectedSocket);
        public partial Task<int> CloseWebSocketConnectionAsync(string Id = "", string Address = "", string Reason = "");
        public partial Task<bool> SetPageDataOnRequestAsync(string RequestedLandingSiteResource);
        public partial List<ConnectedWebSocket>? GetConnectedWebSockets(string Address = "", string Id = "", string RegisteredName = "");
        public partial Task RegisterNewSocketConnectionAsync(ConnectedWebSocket NewSocket);
        public partial bool CanMakeNewSocketConnection();
        public partial List<string> GetListOfLandingSites();
    }

    public partial class SimpleWebServerController : ISimpleWebServerController
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
        private const int receiveChunkSize = 512;
        private const int sendChunkSize = 512;
        private int RequestCount = 0;



        private string LastPageDisplay { get; set; }
        private readonly ILogger logger;
        private readonly ProgramOptionsController ProgramOptions;


        // Constructor
        public SimpleWebServerController(ILogger<SimpleWebServerController> _logger, IKeyBindController keybindController, IProgramOptionsController _programOptionsController, IOutputController _outputController)
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
                    // Bypass / refuse any favicon requests because I'm lazy and don't want to work out landing sites from browser request URLs.
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
                    OutputController.StandardOutput("Upgrading to web socket");
                    try
                    {
                        if (!CanMakeNewSocketConnection())
                        {
                            OutputController.StandardOutput("Max number of socket connections reached");
                            logger.LogWarning("Max number of web socket connections reached");
                            return false;
                        }

                        webSocketContext = await CTX.AcceptWebSocketAsync(subProtocol: null, receiveChunkSize, TimeSpan.FromSeconds(15));
                        webSocket = webSocketContext.WebSocket;


                        // Connected - add to socket list
                        Interlocked.Increment(ref RequestCount);
                        connectedWebSocket = new ConnectedWebSocket() { SocketContext = webSocketContext, Id = Guid.NewGuid().ToString(), Address = CTX.Request.RemoteEndPoint.ToString(), EndPoint = CTX.Request.RemoteEndPoint };
                        await RegisterNewSocketConnectionAsync(connectedWebSocket);
                        OutputController.StandardOutput("Web Socket Connection(s): {0}", RequestCount);


                        // Say Hello
                        SocketConnectedResponse socketConnectedResponse = new SocketConnectedResponse() { Id = connectedWebSocket.Id, Message = "Server Says Hello" };
                        await SendDataOverSocketAsync(JsonSerializer.Serialize<SocketConnectedResponse>(socketConnectedResponse), ConnectedSocket: connectedWebSocket);

                    }
                    catch (Exception ex)
                    {
                        OutputController.StandardOutput(ex, ex.Message);
                        logger.LogCritical(1, ex, "");
                    }

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
                                ConnectedWebSocketList.Remove(connectedWebSocket);
                            }
                            else
                            {
                                // Start conversation.
                                var dr = Encoding.Default.GetString(arrayBuffer);
                                string dataReceived = dr.Replace("\0", string.Empty);
                                OutputController.StandardOutput("Received Data: " + dataReceived);
                                object? reqData = new object();

                                if (!string.IsNullOrEmpty(dataReceived))
                                {
                                    // Need to empty receive buffer to hold next possible request
                                    receiveBuffer = new byte[receiveChunkSize];

                                    var t = JsonSerializer.Deserialize<object>(dataReceived);
                                    reqData = t.FromJson();

                                    // Provided ID and requester match
                                    if (reqData != null)
                                    {
                                        string requesterID = reqData?.GetType().GetProperty("Id")?.GetValue(reqData)?.ToString() ?? "";
                                        if ((!string.IsNullOrEmpty(requesterID) && requesterID.Equals(connectedWebSocket.Id)) || (ProgramOptions.ProgramOptions.AllowDeveloperId && requesterID.Equals("developer")))
                                        {
                                            // Provided ID and requester match
                                            /// If a registered name exists, set / overwrite with correct value
                                            if (!requesterID.Equals("developer"))
                                            {
                                                reqData?.GetType().GetProperty("RequesterName").SetValue(reqData, ConnectedWebSocketList.First(a => a.Id == requesterID).RegisteredName ?? "");
                                            }

                                            ServerCommandResponse commandResponse = await ProcessCommandRequestAsync(reqData);
                                            string toJson = "";
                                            if (commandResponse.Command != null)
                                            {
                                                toJson = JsonSerializer.Serialize<ServerCommandResponse>(commandResponse);
                                            }
                                            else
                                            {
                                                commandResponse.Id = connectedWebSocket.Id;
                                                commandResponse.Command = reqData;
                                                commandResponse.CommandSuccess = false;
                                                commandResponse.CommandResponse = null;
                                                commandResponse.Message = "Unknown command / Syntax error";
                                                toJson = JsonSerializer.Serialize<ServerCommandResponse>(commandResponse);
                                            }


                                            try
                                            {
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
                                        else
                                        {
                                            ServerCommandResponse serverCommandResponse = new ServerCommandResponse() { Id = connectedWebSocket.Id, Command = reqData, CommandSuccess = false, Message = "Provided ID does not match the request origin" };
                                            string toJson = JsonSerializer.Serialize<ServerCommandResponse>(serverCommandResponse);
                                            if (!string.IsNullOrEmpty(toJson))
                                            {
                                                await SendDataOverSocketAsync(toJson, ConnectedSocket: connectedWebSocket);
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
        public partial async Task<bool> SendDataOverSocketAsync(string ToSend, string Address = "")
        {

            ConnectedWebSocket? ConnectedSocket = ConnectedWebSocketList.FirstOrDefault(a => a.Address.Equals(Address));
            if (ConnectedSocket == null)
            {
                return false;
            }

            return await SendDataOverSocketAsync(ToSend, ConnectedSocket);
        }

        public partial async Task<bool> SendDataOverSocketAsync(string ToSend, ConnectedWebSocket ConnectedSocket)
        {
            if (ConnectedSocket != null && ConnectedSocket.SocketContext.WebSocket != null && ToSend != null)
            {
                WebSocket webSocket = ConnectedSocket.SocketContext.WebSocket;

                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    byte[] sendBuffer = new byte[sendChunkSize];
                    ICollection<SocketResponseChunk> responseChunks = new List<SocketResponseChunk>();
                    int IdStart = 1;

                    int TextToSendLength = ToSend.Length;
                    int TotalChunks = (TextToSendLength / sendChunkSize) + ((TextToSendLength % sendChunkSize) - ((TextToSendLength % sendChunkSize) - 1));
                    int StartPosition = 0;
                    int CharCount = sendChunkSize;

                    for (int i = 1; i <= TotalChunks; i++)
                    {
                        if (sendChunkSize * i > TextToSendLength)
                        {
                            CharCount = TextToSendLength - (sendChunkSize * (i - 1));
                        }

                        responseChunks.Add(new SocketResponseChunk() { ChunkValue = ToSend.Substring(StartPosition, CharCount), Id = IdStart, TotalChunks = TotalChunks });
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
        public partial async Task<int> CloseWebSocketConnectionAsync(string Id = "", string Address = "", string Reason = "")
        {
            IEnumerable<ConnectedWebSocket> CloseSocketList = ConnectedWebSocketList.Where(a => a.Id.Equals(Address) || Address.Equals("*"));
            int closedConnectionCount = 0;
            foreach (ConnectedWebSocket connectedSocket in CloseSocketList)
            {
                if (connectedSocket.SocketContext.WebSocket != null && connectedSocket.SocketContext.WebSocket.State == WebSocketState.Open)
                {
                    await connectedSocket.SocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, Reason, CancellationToken.None);

                    // Remove socket from connected socket list
                    ConnectedWebSocketList.Remove(connectedSocket);
                    closedConnectionCount++;
                }

            }
            return closedConnectionCount;
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
        public partial async Task<ServerCommandResponse> ProcessCommandRequestAsync(object commandRequest)
        {
            ServerCommandResponse serverResponse = new ServerCommandResponse() { Command = commandRequest, Id = commandRequest.GetType().GetProperty("Id").GetValue(commandRequest)?.ToString() ?? "" };

            string CommandName = commandRequest.GetType().GetProperty("Command").GetValue(commandRequest).ToString();
            if (commandRequest != null && !string.IsNullOrEmpty(CommandName))
            {
                MethodInfo? dMeth = this.GetType().GetMethods().FirstOrDefault(a => a.Name.ToLower().Equals($"process{CommandName.ToLower()}commandasync"));

                if (dMeth != null)
                {
                    //serverResponse.CommandResponse = dMeth.Invoke(this, new object[] { commandRequest });
                    try
                    {
                        if (this.GetType().GetMethods().Any(a => a.Name.ToLower().Equals($"process{CommandName.ToLower()}commandasync")))
                        {
                            Task? t = (Task)GetType().GetMethods().First(a => a.Name.ToLower().Equals($"process{CommandName.ToLower()}commandasync")).Invoke(this, new object[] { commandRequest });
                            if (t != null)
                            {
                                serverResponse.CommandSuccess = true;
                                serverResponse.CommandResponse = t.GetType().GetProperty("Result").GetValue(t);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        serverResponse.CommandSuccess = false;
                        return serverResponse;
                    }
                }

                if (serverResponse.CommandResponse != null)
                {
                    var successProp = serverResponse.CommandResponse.GetType().GetProperty("Success").GetValue(serverResponse.CommandResponse);
                    if ((bool)successProp == true)
                    {
                        serverResponse.Message = "Command was successfully executed and processed";
                    }
                    else
                    {
                        serverResponse.Message = "Command was successfully executed but not processed";
                    }
                }
                else
                {
                    serverResponse.Message = "Unknown command was requested";
                }
            }

            return serverResponse;
        }


        // Handles unknown command types
        public virtual async Task<ServerCommandResponse> CheckForAdditionalCommands(ServerCommandResponse serverResponse, object commandRequest)
        {
            serverResponse.CommandSuccess = false;
            serverResponse.Message = "Unknown Command Request";
            return serverResponse;
        }




        // Inspects and processes possible GET query arguments and builds a Command Reqest from them
        public async Task<bool> ProcessRequestQuery(HttpListenerRequest HttpRequest)
        {
            if (HttpRequest?.Url != null)
            {
                // Determine Request Landing Site / Resource
                if (HttpRequest.Url.Query.Length > 2)
                {
                    // We have some query attached to the request
                    string CommandToExecute = "";
                    string QueryCommandData = "";
                    string queryTidy = HttpRequest.Url.Query;
                    if (HttpRequest.Url.Query.StartsWith("?"))
                    {
                        queryTidy = HttpRequest.Url.Query.Replace("?", "");
                    }
                    var parsedQuery = HttpUtility.ParseQueryString(queryTidy);
                    CommandToExecute = parsedQuery.GetValues("Command").FirstOrDefault() ?? "";
                    Type? checkModelsList = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(t => t.Namespace.Contains("SocketRequest.Commands") && t.Name.ToLower().Equals($"{CommandToExecute.ToLower()}request"));

                    if (checkModelsList != null)
                    {
                        var nInstance = Activator.CreateInstance(checkModelsList);
                        nInstance = ConvertFromCommandData(parsedQuery, nInstance);
                        _ = await ProcessCommandRequestAsync(nInstance);
                    }
                }
            }
            return true;
        }





        // Returns all matching web sockets
        public partial List<ConnectedWebSocket> GetConnectedWebSockets(string Address = "", string Id = "", string RegisteredName = "")
        {
            List<ConnectedWebSocket> connectedWebSockets = new List<ConnectedWebSocket>();

            if (ConnectedWebSocketList.Count > 0)
            {
                if (string.IsNullOrEmpty(Address) && string.IsNullOrEmpty(Id) && string.IsNullOrEmpty(RegisteredName))
                {
                    return ConnectedWebSocketList.ToList();
                }
                else
                {
                    connectedWebSockets.AddRange(ConnectedWebSocketList.Where(a => a.Address.StartsWith(Address) || a.Id.Equals(Id) || a.RegisteredName.Equals(RegisteredName)));
                }
            }

            return connectedWebSockets;
        }



        // Registers a new Web Socket connection
        public partial async Task RegisterNewSocketConnectionAsync(ConnectedWebSocket NewSocket)
        {
            if (CanMakeNewSocketConnection())
            {
                ConnectedWebSocketList.Add(NewSocket);
            }
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


        public object ConvertFromCommandData(NameValueCollection CommandData, object Target)
        {
            foreach (PropertyInfo propertyInfo in Target.GetType().GetProperties())
            {
                foreach (var key in CommandData.AllKeys)
                {
                    if (key.Equals(propertyInfo.Name))
                    {
                        propertyInfo.SetValue(Target, CommandData[key]);
                    }
                }
            }
            return Target;
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