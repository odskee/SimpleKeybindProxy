using SimpleKeybindProxy.Models;

namespace SimpleKeybindProxy.Interfaces
{
    public partial interface IProgramOptionsController
    {
        public void SetProgramOptions(ProgramOptions programOptions);
        public Task<bool> LoadProgramOptionsAsync(string FromFile = "");
    }
}
