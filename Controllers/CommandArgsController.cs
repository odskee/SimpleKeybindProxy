using SimpleKeybindProxy.Interfaces;
using SimpleKeybindProxy.Models;
using System.CommandLine;

namespace SimpleKeybindProxy.Controllers
{
    public partial class CommandArgsController : ICommandArgsController
    {
        public ProgramOptions ProgramOptions { get; set; }
        public Command ReadCommand { get; set; }
        public string EnvironmentName { get; set; }


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
            EnvironmentName = environmentName;
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

                    if (string.IsNullOrEmpty(ip))
                    {
                        ProgramOptions.Ip = "*";
                    }
                    else
                    {
                        ProgramOptions.Ip = ip;
                    }

                    if (string.IsNullOrEmpty(port))
                    {
                        ProgramOptions.Port = "8001";
                    }
                    else
                    {
                        ProgramOptions.Port = port;
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
            string holdLoc = "";
            if (string.IsNullOrEmpty(landingSite))
            {
                if (EnvironmentName.Equals("Development"))
                {
                    //ProgramOptions.LandingDir = "..\\..\\..\\Landing";
                    holdLoc = "..\\..\\..\\Landing";
                }
                else
                {
                    holdLoc = ".\\Landing";
                    //ProgramOptions.LandingDir = ".\\Landing";
                }
            }
            else
            {
                //ProgramOptions.LandingDir = landingSite;
                holdLoc = landingSite;
            }

            try
            {
                if (Directory.Exists(holdLoc))
                {
                    ProgramOptions.LandingDir = holdLoc;
                }
                else
                {
                    Directory.CreateDirectory(holdLoc);
                    Console.WriteLine("Landing Site Directory Created");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Landing Directory could not be found / Accessed!");
                return;
            }
        }

        public async Task SetBindingSiteAsync(string BindSite)
        {
            string holdLoc = "";
            if (string.IsNullOrEmpty(BindSite))
            {
                if (EnvironmentName.Equals("Development"))
                {
                    //ProgramOptions.BindLocation = "..\\..\\..\\Binds\\";
                    holdLoc = "..\\..\\..\\Binds\\";
                }
                else
                {
                    //ProgramOptions.BindLocation = ".\\Binds\\";
                    holdLoc = ".\\Binds\\";
                }
            }
            else
            {
                //ProgramOptions.BindLocation = BindSite;
                holdLoc = BindSite;
            }

            try
            {
                if (Directory.Exists(holdLoc))
                {
                    ProgramOptions.BindLocation = holdLoc;
                }
                else
                {
                    Directory.CreateDirectory(holdLoc);
                    Console.WriteLine("Bind file Directory Created");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Bind Directory could not be found / Accessed!");
                return;
            }
        }

        public async Task SetVerbosityLevel(string verbLevel)
        {
            ProgramOptions.VerbosityLevel = 1;
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
            string holdLoc = "";
            if (string.IsNullOrEmpty(LogDirectory))
            {
                if (EnvironmentName.Equals("Development"))
                {
                    holdLoc = "..\\..\\..\\Logs\\";
                }
                else
                {
                    holdLoc = ".\\Logs\\";
                }
            }
            else
            {
                holdLoc = LogDirectory;
            }

            try
            {
                if (Directory.Exists(holdLoc))
                {
                    string logFile = holdLoc + "Log.txt";
                    if (File.Exists(logFile))
                    {
                        var creationDatetime = File.GetCreationTimeUtc(logFile);
                        var ExpiredDatetime = creationDatetime.AddDays(1);

                        if (ExpiredDatetime < DateTime.UtcNow)
                        {
                            // log file older than 24 hours, rename and create a new one
                            var CreationTime = File.GetCreationTimeUtc(logFile);
                            string oldLogFileName = holdLoc + $"Log_{CreationTime.ToString("ddMMyy_HHmm")}.txt";
                            File.Move(logFile, oldLogFileName);
                            //File.CreateText(logFile);
                        }

                    }

                    ProgramOptions.Logfile = holdLoc + "Log.txt";
                }
                else
                {
                    Directory.CreateDirectory(holdLoc);
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
}
