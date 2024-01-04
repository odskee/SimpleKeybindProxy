// Author: Tom Bedford
// License: Free to use

// Simple Web Server credit to:
//// Filename:  HttpServer.cs        
//// Author:    Benjamin N. Summerton <define-private-public>        
//// License:   Unlicense (http://unlicense.org/)


// A simple keybind proxy - starts a basic HTTP server and listens for requests.  Landing sites (including CSS and images) can be created and will be returned for
// matching URL requests.  Server looks for "command POST requests" that contain a Reserved Word (see Docs for further info) i.e. "KeyBind_" and will then attempt
// to match the requested keybind name to a known keybind that is then issued (to whatever application has focus at the time).


using Controllers.SimpleWebService;
using SimpleKeybindProxy.Controllers;
using System.CommandLine;
using System.Net;

namespace HttpListenerExample
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            string LandingDir = "";
            string BindLocation = "";
            string Ip = "";
            string Port = "";
            string ServerAddess = "";
            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            //static string TerminalURL(string caption, string url) => $"\u001B]8;;{url}\a{caption}\u001B]8;;\a";

            // Build Arguments
            var LandSiteOption = new Option<string?>(
                name: "-l",
                description: "The directory where your Landing sites are.  If not supplied, assumed to be the programs working directory");

            var BindSiteOption = new Option<string?>(
                name: "-b",
                description: "The directory where name -> keybind pairs can be found.  All .txt files are loaded in this folder");

            var IPOption = new Option<string?>(
                name: "-a",
                description: "The IP Address the server will accept connections on.  Accept connections in all using * (default)");

            var PortOption = new Option<string?>(
                name: "-p",
                description: "The port the server will accept connections on.  Defaults to 8001");

            var LandSiteCommand = new RootCommand("Simple Key Bind Proxy");

            var readCommand = new Command("read", "Read and display the file.")
            {
                LandSiteOption,
                BindSiteOption,
                IPOption,
                PortOption
            };
            LandSiteCommand.AddCommand(readCommand);

            readCommand.SetHandler(async (landingDir, bindDir, ip, port) =>
            {
                if (string.IsNullOrEmpty(landingDir))
                {
                    if (environmentName.Equals("Development"))
                    {
                        LandingDir = "..\\..\\..\\Landing";
                    }
                    else
                    {
                        LandingDir = ".\\Landing";
                    }
                }
                else
                {
                    LandingDir = landingDir;
                }

                if (string.IsNullOrEmpty(bindDir))
                {
                    if (environmentName.Equals("Development"))
                    {
                        BindLocation = "..\\..\\..\\Binds\\";
                    }
                    else
                    {
                        BindLocation = ".\\Binds\\";
                    }
                }
                else
                {
                    BindLocation = bindDir;
                }

                if (string.IsNullOrEmpty(ip))
                {
                    Ip = "*";
                }
                else
                {
                    Ip = ip;
                }

                if (string.IsNullOrEmpty(port))
                {
                    Port = "8001";
                }
                else
                {
                    Port = port;
                }
            },
            LandSiteOption, BindSiteOption, IPOption, PortOption);
            var t = readCommand.InvokeAsync(args).Result;

            // Set a properly built server address string
            ServerAddess = $"http://{Ip}:{Port}/";

            // Initiate Keybind Controller
            KeyBindController bindController = new KeyBindController(BindLocation);
            if (!await bindController.LoadKeyBindLibraryAsync())
            {
                // If we can't load any Keybinds then there is currently no other function the program serves - terminate.
                return;
            }

            // Start Web Server
            HttpServer httpServer = new HttpServer(bindController, LandingDir);
            httpServer.Listener.Prefixes.Add(ServerAddess);
            httpServer.Listener.Start();

            if (!await httpServer.RegisterLandingSitesAsync())
            {
                return;
            }

            // Console Output - show all IP's this can be accessed on:
            if (!ServerAddess.Contains("*"))
            {
                Console.WriteLine("SimpleKeybindProxy has successfully started and can be accessed at {0}", ServerAddess);
            }
            else
            {
                IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress[] addr = ipEntry.AddressList;

                if (addr.Any())
                {
                    Console.WriteLine("SimpleKeybindProxy has successfully started and can be accessed at the following addresses:");
                    Console.WriteLine($"http://localhost:{Port}/");
                    Console.WriteLine($"http://127.0.0.1:{Port}/");
                    foreach (IPAddress addrAddr in addr.Where(a => a.ToString().Contains(":") == false))
                    {
                        Console.WriteLine($"http://{addrAddr}:{Port}/");
                    }
                }
            }
            Console.WriteLine($"");
            Console.WriteLine($"");


            // Console Output - Show list of available landing sites:
            List<string> LandingSites = httpServer.GetListOfLandingSites();
            if (LandingSites.Count > 0)
            {
                Console.WriteLine("The following Landing Sites were detected:");
                foreach (string site in LandingSites)
                {
                    Console.WriteLine($"> {ServerAddess.Replace("*", "localhost")}{site.Split("\\").Last()}/");
                }
            }

            Console.WriteLine($"---------------------------------------");


            // Handle requests
            Task listenTask = httpServer.HandleIncomingConnectionsAsync();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            httpServer.Listener.Close();
        }
    }
}