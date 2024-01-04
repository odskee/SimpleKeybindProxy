using SimpleKeybindProxy.Controllers;
using SimpleKeybindProxy.Interfaces;
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
        private static string[] ReservedWords = { "KeyBind" };
        public HttpListener Listener { get; set; }
        public string PageData;
        public string LandingSiteLocation { get; set; }
        public ICollection<string> LandSiteLocations { get; set; }
        public KeyBindController BindController { get; set; }

        private string LastPageDisplay { get; set; }
        private byte[] data { get; set; }



        // Constructor
        public HttpServer(KeyBindController bindController, string landingSiteLocation)
        {
            Listener = new HttpListener();
            BindController = bindController;
            LandSiteLocations = new HashSet<string>();
            LandingSiteLocation = landingSiteLocation;
        }



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
                    Console.WriteLine("Landing Directory is either not set or cannot be found");
                    return false;
                }

                IEnumerable<string> landingSites = Directory.EnumerateDirectories(LandingSiteLocation);

                foreach (string ls in landingSites)
                {
                    LandSiteLocations.Add(ls);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured building landing sites:");
                Console.WriteLine(ex.Message);
                return false;
            }


            return true;
        }



        // Sets the page data based on a requested URI.  Assumes the URI incoming is valid.
        public partial async Task<bool> SetPageDataOnRequestAsync(string RequestedLandingSiteResource)
        {
            try
            {
                if (RequestedLandingSiteResource.Contains("favicon.ico"))
                {
                    return false;
                }

                string[] requestBreakdown = RequestedLandingSiteResource.Split('/');
                requestBreakdown = requestBreakdown.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                string rsource = "";


                if (LandSiteLocations.Any(ls => ls.Contains(requestBreakdown[0])))
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
                        rsource = "..\\..\\HTML\\Landing.html";

                    }
                    else
                    {
                        rsource = ".\\HTML\\Landing.html";
                    }
                }

                if (!rsource.Split("/").Last().Contains("."))
                {
                    if (ReservedWords.Any(s => rsource.Split("/").Last().Contains(s)))
                    {
                        // Command issued, remove command and apply .html to landing
                        string[] rsourceSplit = rsource.Split("/");
                        IEnumerable<string> rsourceCompiled = rsourceSplit.Take(rsourceSplit.Count() - 1);
                        rsource = rsource.Replace(rsourceSplit.Last(), $"{rsourceCompiled.Last()}.html");
                    }
                    else
                    {
                        rsource = rsource + "/" + rsource.Split("/").Last() + ".html";
                    }
                }

                try
                {
                    if (File.Exists(rsource))
                    {
                        PageData = await File.ReadAllTextAsync(rsource);
                    }
                    else
                    {
                        PageData = await File.ReadAllTextAsync(LandingSiteLocation + "\\..\\HTML\\Landing.html");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error loading resource: {0}", ex.Message);
                    throw;
                }

                if (rsource.Contains(".html") && !rsource.Contains("Error.html"))
                {
                    LastPageDisplay = PageData;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("General Error: {0}", ex.Message);
                throw;
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

                    resp.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                    resp.StatusCode = 200;

                    // Get Data from request
                    Stream body = req.InputStream;
                    Encoding encoding = req.ContentEncoding;
                    StreamReader reader = new StreamReader(body, encoding);
                    string s = reader.ReadToEnd();

                    // Determine Request Landing Site / Resource
                    if (ReservedWords.Any(s => req.Url.AbsolutePath.Contains(s)))
                    {
                        // Is a command to execute
                        PageData = LastPageDisplay;
                        resp.ContentType = "text/html";

                        if (req.Url.AbsolutePath.Contains("/KeyBind_"))
                        {
                            await BindController.ProcessKeyBindRequestAsync(req.Url.AbsolutePath);
                        }

                        // Attempt to resolve a blank landing page for better feel
                        if (string.IsNullOrEmpty(PageData) && string.IsNullOrEmpty(PageData))
                        {
                            // No Page data, if we have a URL request with a matching landing page, can generate this...
                            await SetPageDataOnRequestAsync(req.Url.AbsolutePath);
                        }
                    }
                    else
                    {
                        // Issue Console Output
                        Console.WriteLine("Resource Requested: {0}", req.Url.AbsolutePath);

                        // is a resource
                        await SetPageDataOnRequestAsync(req.Url.AbsolutePath);

                        // Figure out content type to set
                        if (req.Url.AbsolutePath.Contains(".css"))
                        {
                            resp.ContentType = "text/css";
                        }
                        else if (req.Url.AbsolutePath.Contains(".png")
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

                    // Write out to the response stream (asynchronously), then close it
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    PageDataSet = false;

                    resp.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("General Error: {0}", ex.Message);
                throw;
            }
        }
    }
}