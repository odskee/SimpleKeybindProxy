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


            // Build Arguments
            var LandSiteOption = new Option<string?>(
                name: "-l",
                description: "The directory where your Landing sites are.  If not supplied, assumed to be the programs working directory");

            var BindSiteOption = new Option<string?>(
                name: "-b",
                description: "The directory / file where name -> keybind pairs can be found.  If no filename is supplied, binds.txt is used.  If no directory is supplied, the programs working directory is used");

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
                        BindLocation = "..\\..\\..\\Binds\\binds.txt";
                    }
                    else
                    {
                        BindLocation = ".\\Binds\\binds.txt";
                    }
                }
                else
                {
                    string bindCorrected = bindDir.Replace("/", "\\");
                    string[] fullOrSingle = bindDir.Split("\\");
                    if (fullOrSingle.Count() > 1 && fullOrSingle.Last().Contains(".txt"))
                    {
                        // Path and name
                        BindLocation = bindDir;
                    }
                    else if (fullOrSingle.Count() == 1)
                    {
                        // Just filename
                        if (environmentName.Equals("Development"))
                        {
                            BindLocation = $"..\\..\\..\\Binds\\{bindDir}";
                        }
                        else
                        {
                            BindLocation = $".\\Binds\\{bindDir}";
                        }
                    }
                    else
                    {
                        // Just Path
                        BindLocation = bindDir + "/binds.txt";
                    }
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
            Console.WriteLine("Listening for connections on {0}", ServerAddess);

            // Handle requests
            Task listenTask = httpServer.HandleIncomingConnectionsAsync();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            httpServer.Listener.Close();
        }
    }
}