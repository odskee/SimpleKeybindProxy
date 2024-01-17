using SimpleKeybindProxy.Interfaces;
using SimpleKeybindProxy.Models;
using System.Text.Json;

namespace SimpleKeybindProxy.Controllers
{

    public partial class ProgramOptionsController : IProgramOptionsController
    {
        public partial void SetProgramOptions(ProgramOptions programOptions);
        public partial Task<bool> LoadProgramOptionsAsync(string FromFile = "");
    }


    public partial class ProgramOptionsController : IProgramOptionsController
    {
        public ProgramOptions ProgramOptions { get; set; }
        public string ServerAddress { get; set; }
        public bool ServerListen { get; set; }


        public ProgramOptionsController()
        {
            ProgramOptions = new ProgramOptions();
        }

        public partial void SetProgramOptions(ProgramOptions programOptions)
        {
            ProgramOptions = programOptions;
        }

        public partial async Task<bool> LoadProgramOptionsAsync(string FromFile = "")
        {
            if (string.IsNullOrEmpty(FromFile))
            {
                if (ProgramOptions != null && ProgramOptions.EnvironmentName.Equals("debug"))
                {
                    FromFile = "..\\..\\..\\.\\AppSettings.json";

                }
                else
                {
                    FromFile = ".\\AppSettings.json";
                }

                if (File.Exists(FromFile))
                {
                    try
                    {
                        using (FileStream fs = File.OpenRead(FromFile))
                        {
                            if (fs.CanRead)
                            {
                                ProgramOptions proOp = await JsonSerializer.DeserializeAsync<ProgramOptions>(fs);
                                ProgramOptions = proOp;

                                ServerAddress = $"http://{ProgramOptions.Ip}:{ProgramOptions.Port}/";

                                // Append correct log file name to log file location
                                string logFileName = ProgramOptions.Logfile + "/log.txt";

                                if (File.Exists(logFileName))
                                {
                                    // If it's older than 1 day, make it an old log and create a new one
                                    DateTime creationDateTime = File.GetCreationTime(ProgramOptions.Logfile + "/" + logFileName);
                                    DateTime expiredDateTime = creationDateTime.AddDays(1);

                                    if (expiredDateTime >= DateTime.Now)
                                    {
                                        // Expired
                                        if (File.Exists(ProgramOptions.Logfile + $"/log{DateTime.Now.ToString("dd_MM_yy")}.txt"))
                                        {
                                            File.Move(ProgramOptions.Logfile + $"/log{DateTime.Now.ToString("dd_MM_yy")}.txt", ProgramOptions.Logfile + $"/log{DateTime.Now.ToString("dd_MM_yy")}.txt.bac");
                                        }
                                        File.Move(logFileName, ProgramOptions.Logfile + $"/log{DateTime.Now.ToString("dd_MM_yy")}.txt");
                                    }
                                }
                                ProgramOptions.Logfile = logFileName;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return false;
                        //throw;
                    }
                }

            }
            return true;
        }

    }
}
