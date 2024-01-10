using Microsoft.Extensions.Logging;
using SimpleKeybindProxy.Controllers;
using SimpleKeybindProxy.Interfaces;
using SimpleKeybindProxy.Models;
using System.Net;
using System.Text;

namespace Controllers.SimpleWebService
{
    public partial class HttpServer
    {
        public partial Task HandleIncomingConnectionsAsync();
        public partial Task<bool> SetPageDataOnRequestAsync(string RequestedLandingSiteResource);
        public partial Task<bool> RegisterLandingSitesAsync();
        public partial List<string> GetListOfLandingSites();

    }

    public partial class HttpServer : ISimpleWebServerController
    {
        public HttpListener Listener { get; set; }
        public string PageData;
        public string LandingSiteLocation { get; set; }
        public ICollection<string> LandSiteLocations { get; set; }
        public KeyBindController BindController { get; set; }

        private string LastPageDisplay { get; set; }
        private byte[] data { get; set; }

        public ProgramOptions Options { get; set; }

        private readonly ILogger logger;


        // Constructor
        public HttpServer(ILogger<HttpServer> _logger, IKeyBindController keybindController)
        {
            Listener = new HttpListener();
            //BindController = bindController;
            LandSiteLocations = new HashSet<string>();
            //LandingSiteLocation = landingSiteLocation;

            logger = _logger;
        }


        public void SetBindController(KeyBindController keyBindController) { BindController = keyBindController; }
        public void SetProgramOptions(ProgramOptions options) { Options = options; LandingSiteLocation = options.LandingDir; }



        // Returns a list of Landing Sites
        public partial List<string> GetListOfLandingSites()
        {
            return LandSiteLocations.ToList();
        }


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
                    if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT").Equals("Development"))
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
                        PageData = await File.ReadAllTextAsync(rsource);
                        logger.LogDebug("File found and contents read: {0}", rsource);
                    }
                    else
                    {
                        PageData = "<HTML><HEAD><meta charset=\"utf-8\"><title>SimpleKeybindProxy</title></head><body><h1>Program Error</h1></body></html>";
                        logger.LogDebug("No resource data could be loaded");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error loading resource, See log for further info");
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
                Console.WriteLine("General Error, See log for further info");
                logger.LogCritical(1, ex, "General error finding resource");
                return false;
            }
        }



        // Handles incoming connections from the web server and responds accordingly.
        // Requests to certain endpoints will result in calls to KeyBindController.
        public partial async Task HandleIncomingConnectionsAsync()
        {
            try
            {
                bool runServer = true;
                bool PageDataSet = false;

                while (runServer)
                {
                    // Will wait here until we hear from a connection
                    HttpListenerContext ctx = await Listener.GetContextAsync();

                    // Peel out the requests and response objects
                    HttpListenerRequest req = ctx.Request;
                    HttpListenerResponse resp = ctx.Response;

                    // Bypass / refuse any favicon requests
                    if (req.Url.AbsolutePath.Contains("favicon"))
                    {
                        resp.StatusCode = 404;
                        resp.Close();
                    }
                    else
                    {

                        string CommandToExecute = "";
                        string CommandData = "";
                        resp.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                        resp.StatusCode = 200;

                        // Determine Request Landing Site / Resource
                        if (req.Url.Query.Length > 2)
                        {
                            // We have some query attached to the request
                            string queryTidy = req.Url.Query;
                            if (req.Url.Query.StartsWith("?"))
                            {
                                queryTidy = req.Url.Query.Replace("?", "");
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

                            if (!string.IsNullOrEmpty(CommandToExecute) && CommandToExecute.StartsWith("KeyBind"))
                            {
                                // Is a command to execute
                                PageData = LastPageDisplay;
                                resp.ContentType = "text/html";

                                if (!string.IsNullOrEmpty(CommandData))
                                {
                                    KeyBindController.KeypressType keypressType = KeyBindController.KeypressType.KeyPress;

                                    if (CommandToExecute.ToLower().Equals("keybind_press"))
                                    {
                                        keypressType = KeyBindController.KeypressType.KeyPress;
                                    }
                                    else if (CommandToExecute.ToLower().Equals("keybind_hold"))
                                    {
                                        keypressType = KeyBindController.KeypressType.KeyHold;
                                    }
                                    else if (CommandToExecute.ToLower().Equals("keybind_release"))
                                    {
                                        keypressType = KeyBindController.KeypressType.KeyRelease;
                                    }

                                    if (!await BindController.ProcessKeyBindRequestAsync(CommandData, keypressType))
                                    {
                                        // Problem occured during the keybind issue request
                                        Console.WriteLine("A problem occured trying to issue your requested keybind: {0}", CommandData);
                                    }
                                }

                                // Attempt to resolve a blank landing page for better feel
                                if (string.IsNullOrEmpty(PageData) && string.IsNullOrEmpty(LastPageDisplay))
                                {
                                    // No Page data, if we have a URL request with a matching landing page, can generate this...
                                    await SetPageDataOnRequestAsync(req.Url.AbsolutePath);
                                }
                            }
                        }



                        // Issue Console Output
                        if (Options.VerbosityLevel > 1)
                        {
                            Console.WriteLine("Resource Requested: {0} by {1}", req.Url.AbsolutePath, req.RemoteEndPoint);
                        }
                        logger.LogInformation("Resource Requested: {0} by {1}", req.Url.AbsolutePath, req.RemoteEndPoint);

                        // is a resource
                        await SetPageDataOnRequestAsync(req.Url.AbsolutePath);

                        // Figure out content type to set
                        if (req.Url.AbsolutePath.Contains(".css"))
                        {
                            resp.ContentType = "text/css";
                        }
                        else if (req.Url.AbsolutePath.Contains(".png")
                            || req.Url.AbsolutePath.Contains(".js")
                            || req.Url.AbsolutePath.Contains(".gif")
                            || req.Url.AbsolutePath.Contains(".jpeg")
                            || req.Url.AbsolutePath.Contains(".jpg")
                            || req.Url.AbsolutePath.Contains(".svg"))
                        {
                            string fileLocation = LandingSiteLocation + req.Url.AbsolutePath.Replace("/", "\\");
                            if (File.Exists(fileLocation))
                            {
                                data = await File.ReadAllBytesAsync(fileLocation);
                                PageDataSet = true;

                                switch (req.Url.AbsolutePath.Split(".").Last())
                                {
                                    case "png":
                                    resp.ContentType = "image/png";
                                    break;

                                    case "js":
                                    resp.ContentType = "text/javascript";
                                    break;

                                    case "gif":
                                    resp.ContentType = "image/gif";
                                    break;

                                    case "jpeg":
                                    resp.ContentType = "image/jpeg ";
                                    break;

                                    case "jpg":
                                    resp.ContentType = "image/jpeg ";
                                    break;

                                    case "svg":
                                    resp.ContentType = "image/svg+xml";
                                    break;

                                    default:
                                    resp.ContentType = "text/html";
                                    break;
                                }
                            }
                        }
                        else
                        {
                            resp.ContentType = "text/html";
                        }



                        if (!PageDataSet)
                        {

                            if (PageData != null)
                            {
                                data = Encoding.UTF8.GetBytes(PageData);
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(LastPageDisplay))
                                {
                                    data = Encoding.UTF8.GetBytes("ERROR");
                                }
                                else
                                {
                                    data = Encoding.UTF8.GetBytes(LastPageDisplay);
                                }
                            }
                        }
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;
                        resp.KeepAlive = true;

                        // Write out to the response stream (asynchronously), then close it
                        try
                        {
                            await resp.OutputStream.WriteAsync(data, 0, data.Length);
                        }
                        catch (HttpListenerException ex)
                        {
                            Console.WriteLine("General Error, see Log for more information");
                            logger.LogCritical(1, ex, "General Error");
                            resp.StatusCode = 500;
                        }
                        PageDataSet = false;

                        resp.Close();
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("General Error, see log for more information");
                logger.LogCritical(1, ex, "Error Serving Request");
                throw;
            }
        }
    }
}