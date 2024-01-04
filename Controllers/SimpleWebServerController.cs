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


        // Constructor
        public HttpServer(KeyBindController bindController, string landingSiteLocation)
        {
            Listener = new HttpListener();
            BindController = bindController;
            LandSiteLocations = new HashSet<string>();
            LandingSiteLocation = landingSiteLocation;
        }



        // Sets the page data based on a requested URI.  Assumes the URI incoming is valid.
        public partial async Task<bool> SetPageDataOnRequestAsync(string RequestedLandingSiteResource)
        {
            if (RequestedLandingSiteResource.Contains("favicon.ico"))
            {
                return false;
            }


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
                rsource = LandingSiteLocation + "\\Landing.html";
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
                }
                else
                {
                    PageData = await File.ReadAllTextAsync(LandingSiteLocation + "\\Landing.html");
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



        // Handles incoming connections from the web server and responds accordingly.
        // Requests to certain endpoints will result in calls to KeyBindController.
        public partial async Task HandleIncomingConnectionsAsync()
        {
            bool runServer = true;

            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await Listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                resp.AddHeader("Content-Type", "application/x-www-form-urlencoded");

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
                    else if (req.Url.AbsolutePath.Contains(".png"))
                    {

                    }
                    else
                    {
                        resp.ContentType = "text/html";
                    }
                }

                byte[] data;
                if (PageData != null)
                {
                    data = Encoding.UTF8.GetBytes(PageData);
                }
                else
                {
                    data = Encoding.UTF8.GetBytes("ERROR");
                }
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length);

                resp.Close();
            }
        }
    }
}