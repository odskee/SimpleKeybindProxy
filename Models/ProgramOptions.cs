namespace SimpleKeybindProxy.Models
{
    public partial class ProgramOptions
    {
        public string LandingDir { get; set; }
        public string BindLocation { get; set; }
        public string Ip { get; set; }
        public string Port { get; set; }
        public string ServerAddess { get; set; }
        public int VerbosityLevel { get; set; }
        public string Logfile { get; set; }
        public bool IgnoreMissingLanding { get; set; }
        public bool PreventBindIssue { get; set; }


    }
}
