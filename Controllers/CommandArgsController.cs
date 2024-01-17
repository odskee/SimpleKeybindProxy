using SimpleKeybindProxy.Interfaces;
using SimpleKeybindProxy.Models;
using System.CommandLine;

namespace SimpleKeybindProxy.Controllers
{
    public partial class CommandArgsController : ICommandArgsController
    {
        public ProgramOptions ProgramOptions { get; set; }
        public Command ReadCommand { get; set; }


        public CommandArgsController()
        {
            ProgramOptions = new ProgramOptions();
        }


        public async Task SetProgramOptionsAsync(ProgramOptions programOptions, string environmentName = "")
        {
            if (programOptions != null)
            {
                ProgramOptions = programOptions;
            }
            ProgramOptions.EnvironmentName = environmentName;
        }



        public async Task<int> ProcessArgumentsAsync(string[] CommandArguments)
        {
            // Check for no-option params
            ProgramOptions.IgnoreMissingLanding = false;
            if (CommandArguments.Any(a => a.Equals("--ignore")))
            {
                ProgramOptions.IgnoreMissingLanding = true;
            }
            if (CommandArguments.Any(a => a.Equals("--noissue")))
            {
                ProgramOptions.PreventBindIssue = true;
            }


            return ReadCommand.InvokeAsync(CommandArguments).Result;
        }


        public async Task<bool> BuildRootCommandAsync()
        {
            try
            {
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

                var VerbosityLevelOption = new Option<string?>(
                    name: "-v",
                    description: "Verbosity level - 1: Standard, 2: Noisy.  Defaults to 1");

                var LogDirOption = new Option<string?>(
                    name: "-o",
                    description: "Log file directory.  Defaults to /Logs");

                var IgnoreLandingOption = new Option<string?>(
                    name: "--ignore",
                    description: "Ignore missing Landing site location(s).  I.e. run with externally hosted landing sites");

                var NoKeybindIssueOption = new Option<string?>(
                    name: "--noissue",
                    description: "Don't actually send the requested keybind - use for testing.");


                // Build Root Command
                var LandSiteCommand = new RootCommand("Simple Key Bind Proxy");

                // Set options
                ReadCommand = new Command("read", "Read and display the file."){
                    LandSiteOption,
                    BindSiteOption,
                    IPOption,
                    PortOption,
                    VerbosityLevelOption,
                    LogDirOption,
                    IgnoreLandingOption,
                    NoKeybindIssueOption
                };
                LandSiteCommand.AddCommand(ReadCommand);

                // Build Handlers
                ReadCommand.SetHandler(async (landingDir, bindDir, ip, port, verbLevel, logDir, ignoreLanding, noIssue) =>
                {
                    await SetLandingSiteAsync(landingDir);
                    await SetBindingSiteAsync(bindDir);

                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (ValidateIPv4(ip))
                        {
                            ProgramOptions.Ip = ip;
                        }
                        else
                        {
                            Console.WriteLine("-a {0} is not a valid address", ip);
                        }
                    }


                    if (!string.IsNullOrEmpty(port))
                    {
                        if (Int32.TryParse(port, out _) && Int32.Parse(port) >= 0 && Int32.Parse(port) <= 65535)
                        {
                            ProgramOptions.Port = port;
                        }
                        else
                        {
                            Console.WriteLine("-p {0} is not a valid port", port);
                        }
                    }
                    else
                    {
                    }

                    await SetVerbosityLevel(verbLevel);
                    await SetLogFile(logDir);


                },
                LandSiteOption, BindSiteOption, IPOption, PortOption, VerbosityLevelOption, LogDirOption, IgnoreLandingOption, NoKeybindIssueOption);



                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected Error: {0}", ex.Message);
                return false;
            }

        }


        public async Task SetLandingSiteAsync(string landingSite)
        {
            if (!string.IsNullOrEmpty(landingSite))
            {
                try
                {
                    if (Directory.Exists(landingSite))
                    {
                        ProgramOptions.LandingDir = landingSite;
                    }
                    else
                    {
                        Directory.CreateDirectory(landingSite);
                        Console.WriteLine("Landing Site Directory Created");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Landing Directory could not be found / Accessed!");
                    return;
                }
            }
        }

        public async Task SetBindingSiteAsync(string BindSite)
        {
            if (!string.IsNullOrEmpty(BindSite))
            {
                try
                {
                    if (Directory.Exists(BindSite))
                    {
                        ProgramOptions.BindLocation = BindSite;
                    }
                    else
                    {
                        Directory.CreateDirectory(BindSite);
                        Console.WriteLine("Bind file Directory Created");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Bind Directory could not be found / Accessed!");
                    return;
                }
            }

        }

        public async Task SetVerbosityLevel(string verbLevel)
        {
            if (!string.IsNullOrEmpty(verbLevel))
            {
                try
                {
                    ProgramOptions.VerbosityLevel = Int32.Parse(verbLevel);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unknown Verbosity Level");
                    return;
                }
            }
        }


        public async Task SetLogFile(string LogDirectory)
        {
            if (!string.IsNullOrEmpty(LogDirectory))
            {
                try
                {
                    if (Directory.Exists(LogDirectory))
                    {
                        string logFile = LogDirectory + "Log.txt";
                        ProgramOptions.Logfile = LogDirectory + "Log.txt";
                    }
                    else
                    {
                        Directory.CreateDirectory(LogDirectory);
                        Console.WriteLine("Log file Directory Created");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Log file Directory could not be found / Accessed!");
                    return;
                }
            }
        }



        public bool ValidateIPv4(string ipString)
        {
            if (String.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] ipSplit = ipString.Split('.');
            if (ipSplit.Length != 4)
            {
                return false;
            }

            byte toParse;
            return ipSplit.All(r => byte.TryParse(r, out toParse));
        }
    }
}
