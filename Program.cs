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
            // Command Line Args
            ProgramOptions options = new ProgramOptions();
            CommandArgsController argsController = new CommandArgsController();
            var envName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "";
            await argsController.SetProgramOptionsAsync(options, envName);
            await argsController.BuildRootCommandAsync();
            await argsController.ProcessArgumentsAsync(args);

            // Logging
            if (options.VerbosityLevel > 1)
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File(options.Logfile, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .MinimumLevel.Verbose()
                    .CreateLogger();
            }
            else
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(options.Logfile, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();
            }

            //Dependancy Injection
            var services = new ServiceCollection()
                .AddTransient<ISimpleWebServerController, HttpServer>()
                .AddTransient<IKeyBindController, KeyBindController>()
                .AddLogging(configure => configure.AddSerilog());
            IServiceProvider serviceProvider = services.BuildServiceProvider();


            //Get Logger
            var logger = serviceProvider.GetService<ILogger<Program>>();

            // Set a properly built server address string
            options.ServerAddess = $"http://{options.Ip}:{options.Port}/";

            // Initiate Keybind Controller
            KeyBindController bindController = (KeyBindController)serviceProvider.GetService(typeof(IKeyBindController));
            bindController.SetBindLibraryLocation(options.BindLocation);
            bindController.SetProgramOptions(options);
            if (!await bindController.LoadKeyBindLibraryAsync())
            {
                // If we can't load any Keybinds then there is currently no other function the program serves - terminate.
                Console.WriteLine($"Could not load any Keybind dictionaries at {options.BindLocation}");
                return;
            }
            else
            {
                Console.WriteLine("Bind Libarary loaded successfully.  {0} total binds found.", bindController.GetBindLibraryCount());
            }

            // Configure Web Server
            HttpServer httpServer = (HttpServer)serviceProvider.GetService(typeof(ISimpleWebServerController));
            httpServer.SetBindController(bindController);
            httpServer.SetProgramOptions(options);
            httpServer.Listener.Prefixes.Add(options.ServerAddess);
            httpServer.Listener.Start();

            if (!await httpServer.RegisterLandingSitesAsync())
            {
                Console.WriteLine($"Could not find any Landing sites to display at {options.BindLocation}");

                if (options.IgnoreMissingLanding)
                {
                    logger.LogDebug($"Ignoring missing Landing Site(s)");
                }
                else
                {
                    return;
                }
            }

            // Console Output - show all IP's this can be accessed on:
            if (!options.ServerAddess.Contains("*"))
            {
                logger.LogDebug($"SimpleKeybindProxy is attempting to start at http://{options.Ip}:{options.Port}/");
                Console.WriteLine($"SimpleKeybindProxy is attempting to start at http://{options.Ip}:{options.Port}/");
            }
            else
            {
                IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress[] addr = ipEntry.AddressList;

                if (addr.Any())
                {
                    Console.WriteLine("SimpleKeybindProxy can be accessed at the following addresses:");
                    Console.WriteLine($"http://localhost:{options.Port}/");
                    Console.WriteLine($"http://127.0.0.1:{options.Port}/");
                    foreach (IPAddress add in addr)
                    {
                        if (add.ToString().Contains(":") == false)
                        {
                            Console.WriteLine($"http://{add}:{options.Port}/");
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
                    Console.WriteLine($"> {options.ServerAddess.Replace("*", "localhost")}{site.Split("\\").Last()}/");
                }
            }

            Console.WriteLine($"");
            Console.WriteLine($"---------------------------------------");
            Console.WriteLine($"Awaiting Requests");
            Console.WriteLine($"---------------------------------------");
            logger.LogInformation($"SimpleKeybindProxy is starting at http://{options.Ip}:{options.Port}/");



            // Start Request Handler
            Task listenTask = httpServer.HandleIncomingConnectionsAsync();
            listenTask.GetAwaiter().GetResult();


            // Close the listener
            httpServer.Listener.Close();
        }

    }
}