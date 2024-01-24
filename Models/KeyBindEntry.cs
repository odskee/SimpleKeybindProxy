using static SimpleKeybindProxy.Controllers.KeyBindController;

namespace SimpleKeybindProxy.Models
{
    public class KeyBindEntry
    {
        public string BindName { get; set; }
        public ICollection<string> Modifiers { get; set; } = new List<string>();
        public ICollection<string> Keypress { get; set; } = new List<string>();
        public KeypressType? PressType { get; set; }
    }
}
