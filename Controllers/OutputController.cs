using Microsoft.Extensions.Logging;
using SimpleKeybindProxy.Interfaces;

namespace SimpleKeybindProxy.Controllers
{
    public partial class OutputController : IOutputController
    {
        public ICollection<string> StandardOutputLog { get; set; }



        private List<Action<string>> StandardActionList { get; set; }
        private readonly ProgramOptionsController ProgOptions;
        private readonly ILogger logger;


        public OutputController(ILogger<OutputController> _logger, IProgramOptionsController _programOptionsController)
        {
            logger = _logger;
            ProgOptions = (ProgramOptionsController)_programOptionsController;

            StandardOutputLog = new List<string>();
            StandardActionList = new List<Action<string>>();

        }


        public bool RegisterStandardOutputProvider(Action<string> OutputProvider)
        {
            StandardActionList.Add(OutputProvider);
            return true;
        }


        // Standard Output
        public void StandardOutput(string Message)
        {
            foreach (Action<string> OutputAction in StandardActionList)
            {
                StandardOutputLog.Add(Message);
                OutputAction.Invoke(Message);
            }
        }
        public void StandardOutput(string Message, params object?[] args)
        {
            Message = string.Format(Message, args);
            StandardOutput(Message);

        }
        public void StandardOutput(Exception ex, string Message)
        {
            string builtMessage = Message + ":  <---- TRACE ---->  " + ex.ToString();
            StandardOutput(builtMessage);

        }




        // Debug Output
        public void DebugOutput(string Message)
        {
            if (ProgOptions.ProgramOptions.VerbosityLevel > 1)
            {
                foreach (Action<string> OutputAction in StandardActionList)
                {
                    StandardOutputLog.Add(Message);
                    OutputAction.Invoke(Message);
                    logger.LogDebug(Message);
                }
            }
        }
        public void DebugOutput(string Message, params object?[] args)
        {
            Message = string.Format(Message, args);
            DebugOutput(Message);

        }
        public void DebugOutput(Exception ex, string Message)
        {
            string builtMessage = Message + ":  <---- TRACE ---->  " + ex.ToString();
            DebugOutput(builtMessage);

        }
    }
}
