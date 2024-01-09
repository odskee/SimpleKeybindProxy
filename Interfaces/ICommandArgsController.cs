using SimpleKeybindProxy.Models;

namespace SimpleKeybindProxy.Interfaces
{
    public interface ICommandArgsController
    {
        public Task SetProgramOptionsAsync(ProgramOptions programOptions, string environmentName = "");
        public Task<int> ProcessArgumentsAsync(string[] CommandArguments);
        public Task<bool> BuildRootCommandAsync();
    }
}
