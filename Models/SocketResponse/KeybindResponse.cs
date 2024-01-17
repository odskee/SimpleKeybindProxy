namespace SimpleKeybindProxy.Models.SocketResponse
{
    public partial class KeybindResponse
    {
        public string KeybindName { get; set; }
        public List<string> KeypressCombination { get; set; }
        public List<string> ModifierCombination { get; set; }
        public bool Success { get; set; }
        public string ResponseMessage { get; set; }
    }
}
