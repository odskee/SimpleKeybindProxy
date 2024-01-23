// Author: Tom Bedford
// License: Free to use


// A simple keybind proxy - starts a basic HTTP server and listens for requests.  Landing sites (including CSS and images) can be created and will be returned for
// matching URL requests.  Server looks for "command POST requests" that contain a Reserved Word (see Docs for further info) i.e. "KeyBind_" and will then attempt
// to match the requested keybind name to a known keybind that is then issued (to whatever application has focus at the time).


using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SimpleKeybindProxy.Controllers;
using SimpleKeybindProxy.Interfaces;
using SimpleKeybindProxy.Models;
using System.Net;

namespace HttpListenerExample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            //Dependancy Injection
            var services = new ServiceCollection()
                .AddSingleton<IProgramOptionsController, ProgramOptionsController>()
                .AddSingleton<ISimpleWebServerController, SimpleWebServerController>()
                .AddSingleton<IKeyBindController, KeyBindController>()
                .AddSingleton<IOutputController, OutputController>()
                .AddLogging(configure => configure.AddSerilog());
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            ProgramOptionsController OptionsController = (ProgramOptionsController)serviceProvider.GetService(typeof(IProgramOptionsController));
            OptionsController.ProgramOptions = new ProgramOptions() { EnvironmentName = "debug" };
            await OptionsController.LoadProgramOptionsAsync();


            // Command Line Args
            CommandArgsController argsController = new CommandArgsController();
            var envName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "";
            await argsController.SetProgramOptionsAsync(OptionsController.ProgramOptions, envName);
            await argsController.BuildRootCommandAsync();
            await argsController.ProcessArgumentsAsync(args);

            //TODO: Implement global socket monitor

            // Logging
            if (OptionsController.ProgramOptions.VerbosityLevel > 1)
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File(OptionsController.ProgramOptions.Logfile, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .MinimumLevel.Verbose()
                    .CreateLogger();
            }
            else
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(OptionsController.ProgramOptions.Logfile, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();
            }

            //Get Logger
            var logger = serviceProvider.GetService<ILogger<Program>>();


            // Configure output for Console
            OutputController outputController = (OutputController)serviceProvider.GetService(typeof(IOutputController));
            Action<string> consoleOutput = delegate (string msg)
            {
                Console.WriteLine(msg);
            };
            Action<string> logOutput = delegate (string msg)
            {
                logger.LogInformation(msg);
            };

            outputController.RegisterStandardOutputProvider(consoleOutput);
            outputController.RegisterStandardOutputProvider(logOutput);


            // Initiate Keybind Controller
            KeyBindController bindController = (KeyBindController)serviceProvider.GetService(typeof(IKeyBindController));
            bindController.SetProgramOptions();
            if (!await bindController.LoadKeyBindLibraryAsync())
            {
                // If we can't load any Keybinds then there is currently no other function the program serves - terminate.
                Console.WriteLine($"Could not load any Keybind dictionaries at {OptionsController.ProgramOptions.BindLocation}");
                return;
            }


            // Configure Web Server
            SimpleWebServerController httpServer = (SimpleWebServerController)serviceProvider.GetService(typeof(ISimpleWebServerController));
            httpServer.SetProgramOptions(OptionsController.ProgramOptions);
            httpServer.BindController = bindController;
            httpServer.Listener.Prefixes.Add(OptionsController.ServerAddress);
            try
            {
                httpServer.Listener.Start();

            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine("One or more the provided addresses could be bound too, server cannot start.  Try specifying a manual address with -a");
                logger.LogCritical(1, ex, "Error binding to address");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine("A general failure occured trying to bind to one or more addresses");
                logger.LogCritical(1, ex, "Error binding to address");
                return;
            }

            // Register landing sites
            if (!await httpServer.RegisterLandingSitesAsync())
            {
                Console.WriteLine($"Could not find any Landing sites to display at {OptionsController.ProgramOptions.BindLocation}");
                if (OptionsController.ProgramOptions.IgnoreMissingLanding)
                {
                    logger.LogDebug($"Ignoring missing Landing Site(s)");
                }
                else
                {
                    return;
                }
            }

            // Console Output - show all IP's this can be accessed on:
            if (!OptionsController.ServerAddress.Contains("*"))
            {
                logger.LogDebug($"SimpleKeybindProxy is attempting to start at http://{OptionsController.ProgramOptions.Ip}:{OptionsController.ProgramOptions.Port}/");
                Console.WriteLine($"SimpleKeybindProxy is attempting to start at http://{OptionsController.ProgramOptions.Ip}:{OptionsController.ProgramOptions.Port}/");
            }
            else
            {
                IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress[] addr = ipEntry.AddressList;

                if (addr.Any())
                {
                    Console.WriteLine("SimpleKeybindProxy can be accessed at the following addresses:");
                    Console.WriteLine($"http://localhost:{OptionsController.ProgramOptions.Port}/");
                    Console.WriteLine($"http://127.0.0.1:{OptionsController.ProgramOptions.Port}/");
                    foreach (IPAddress add in addr)
                    {
                        if (add.ToString().Contains(":") == false)
                        {
                            Console.WriteLine($"http://{add}:{OptionsController.ProgramOptions.Port}/");
                        }
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
                    Console.WriteLine($"> {OptionsController.ServerAddress.Replace("*", "localhost")}{site.Split("\\").Last()}/");
                }
            }

            Console.WriteLine($"");
            Console.WriteLine($"---------------------------------------");
            Console.WriteLine($"Awaiting Requests");
            Console.WriteLine($"---------------------------------------");
            logger.LogInformation($"SimpleKeybindProxy is starting at http://{OptionsController.ProgramOptions.Ip}:{OptionsController.ProgramOptions.Port}/");



            // Start server and handle continuous running
            bool Running = true;

            // Start Web Server
            await Task.Factory.StartNew(httpServer.StartContext);

            // Start Socket Monitor after short delay
            await Task.Delay(500);
            //await Task.Factory.StartNew(httpServer.ProcessWebSocketConnectionAsync);

            while (Running)
            {

                Console.Write("> ");
                string input = Console.ReadLine();
                await ProcessRunningArgs(input.Split(" "), bindController, httpServer, OptionsController);
                if (!string.IsNullOrEmpty(input) && (input.ToLower().Equals("exit") || input.ToLower().Equals("close") || input.ToLower().Equals("quit") || input.ToLower().Equals("shutdown")))
                {
                    Running = false;
                }
            }

            // Close the listener
            await httpServer.CloseWebSocketConnectionAsync(Address: "*");
            httpServer.Listener.Close();
        }


        static async Task ProcessRunningArgs(string[] args, KeyBindController bindController, SimpleWebServerController httpServer, ProgramOptionsController OptionsController)
        {
            if (args.Length == 0)
            {
                return;
            }

            switch (args.First())
            {
                case "v":
                try
                {
                    if (!string.IsNullOrEmpty(args[1]))
                    {
                        int Verblevel = Int32.Parse(args[1]);
                        OptionsController.ProgramOptions.VerbosityLevel = Verblevel;
                        Console.WriteLine("Verbosity Level Changed to {0}", Verblevel);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unrecognised Command");
                }
                break;


                case "reload":
                if (await bindController.LoadKeyBindLibraryAsync())
                {
                    Console.WriteLine("Keybind dictionaries have been reloaded: {0} Entries", bindController.GetBindLibraryCount());
                }
                break;

                case "showbinds":
                foreach (var bind in bindController.BindLibrary)
                {
                    Console.WriteLine("{0} -> {1}", bind.Key, bind.Value);
                }
                Console.WriteLine("");
                break;

                case "socketsend":
                if (!string.IsNullOrEmpty(args[1]))
                {
                    string textToSend = "";
                    ConnectedWebSocket? connectedWebSocket = null;
                    if (args[1].StartsWith("-a"))
                    {
                        if (!string.IsNullOrEmpty(args[2]))
                        {
                            connectedWebSocket = httpServer.GetConnectedWebSockets(Address: args[2]).First();
                        }
                        else
                        {
                            Console.WriteLine("Syntax error");
                        }

                        textToSend = string.Join(" ", args.TakeLast(args.Length - 3));
                    }
                    else if (args[1].StartsWith("-i"))
                    {
                        if (!string.IsNullOrEmpty(args[2]))
                        {
                            connectedWebSocket = httpServer.GetConnectedWebSockets(Id: args[2]).First();
                        }
                        else
                        {
                            Console.WriteLine("Syntax error");
                        }

                        textToSend = string.Join(" ", args.TakeLast(args.Length - 3));
                    }
                    else if (args[1].StartsWith("-n"))
                    {
                        if (!string.IsNullOrEmpty(args[2]))
                        {
                            connectedWebSocket = httpServer.GetConnectedWebSockets(RegisteredName: args[2]).First();
                        }
                        else
                        {
                            Console.WriteLine("Syntax error");
                        }

                        textToSend = string.Join(" ", args.TakeLast(args.Length - 3));
                    }
                    else
                    {
                        textToSend = string.Join(" ", args.TakeLast(args.Length - 2));
                    }


                    if (await httpServer.SendDataOverSocketAsync(textToSend, ConnectedSocket: connectedWebSocket))
                    {
                        Console.WriteLine("Text sent over websocket");
                    }
                    else
                    {
                        Console.WriteLine("Websocket not active / could not sent data");
                    }
                }
                else
                {
                    Console.WriteLine("Syntax error");
                }
                Console.WriteLine("");
                break;


                case "noissue":
                if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
                {
                    if (args[1].Equals("1") || args[1].ToLower().Equals("y"))
                    {
                        OptionsController.ProgramOptions.PreventBindIssue = true;
                        Console.WriteLine("Keypresses will no longer be issued");
                    }
                    else if (args[1].Equals("0") || args[1].ToLower().Equals("n"))
                    {
                        OptionsController.ProgramOptions.PreventBindIssue = false;
                        Console.WriteLine("Keypresses will be issued");
                    }
                    else
                    {
                        Console.WriteLine("Unknown Command");
                    }
                }
                Console.WriteLine("");
                break;

                case "showsockets":
                List<ConnectedWebSocket> socketList = httpServer.GetConnectedWebSockets();

                if (socketList?.Count > 0)
                {
                    Console.WriteLine("");
                    Console.WriteLine("Total Open Sockets: {0}", socketList.Count);


                    foreach (ConnectedWebSocket socket in socketList)
                    {
                        Console.WriteLine("");

                        Console.WriteLine("-----------------------------------");
                        Console.WriteLine("Socket: {0}", socket.Address);
                        Console.WriteLine("-----------------------------------");
                        Console.WriteLine("Id: {0}", socket.Id);
                        Console.WriteLine("Registered Name: {0}", socket.RegisteredName);
                        Console.WriteLine("");
                    }
                }
                else
                {
                    Console.WriteLine("There are no open web sockets");
                }

                Console.WriteLine("");
                break;


                default:
                break;
            }
        }
    }
}