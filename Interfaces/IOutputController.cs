namespace SimpleKeybindProxy.Interfaces
{
    public interface IOutputController
    {
        public bool RegisterStandardOutputProvider(Action<string> OutputProvider);
        public void StandardOutput(string Message);
        public void StandardOutput(string Message, params object?[] args);
        public void StandardOutput(Exception ex, string Message);
        public void DebugOutput(string Message);
        public void DebugOutput(string Message, params object?[] args);
        public void DebugOutput(Exception ex, string Message);

    }
}
