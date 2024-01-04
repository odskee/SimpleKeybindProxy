using SimpleKeybindProxy.Interfaces;
using WindowsInput;
using WindowsInput.Native;


namespace SimpleKeybindProxy.Controllers
{


    public partial class KeyBindController : IKeyBindController
    {
        public partial Task<bool> LoadKeyBindLibraryAsync();
        public partial Task<bool> ProcessKeyBindRequestAsync(string bindRequest);

    }

    public partial class KeyBindController : IKeyBindController
    {
        public ICollection<KeyValuePair<string, string>> BindLibrary { get; set; }
        public string BindLibraryLocation { get; set; }


        public KeyBindController(string BindDirectory)
        {
            BindLibrary = new List<KeyValuePair<string, string>>();
            BindLibraryLocation = BindDirectory;
        }



        // Loads the KeyBind File and creates a KeyValue Pair collection
        public partial async Task<bool> LoadKeyBindLibraryAsync()
        {
            // Load Keybind File
            try
            {
                if (File.Exists(BindLibraryLocation))
                {

                    string[] _bindlibrary = await File.ReadAllLinesAsync(BindLibraryLocation);
                    foreach (string bind in _bindlibrary)
                    {
                        string[] paring = bind.Split(",");
                        BindLibrary.Add(new KeyValuePair<string, string>(paring[0].Trim(), paring[1].Trim()));
                    }
                }
                else
                {
                    Console.WriteLine("There was a problem accessing your Bind Library.  Make sure the directory exists and is accessible.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem accessing your Bind Library: {0}{1}", Environment.NewLine, ex.Message);
                return false;
            }

            return true;
        }

        // Processing and issues a keypress based on the bind name / key mapping
        public partial async Task<bool> ProcessKeyBindRequestAsync(string bindRequest)
        {
            string rBind = bindRequest.Split("/").First(a => a.Contains("KeyBind_")).Replace("KeyBind_", "");
            KeyValuePair<string, string>? bindMatch = BindLibrary.FirstOrDefault(a => a.Key.Equals(rBind));
            if (bindMatch.Value.Key == null || bindMatch.Value.Value == null)
            {
                Console.WriteLine("Unknown Bind: {0}", rBind);
                return false;
            }

            string KeyPressName = "";
            KeyPressName = bindMatch.Value.Value;
            Console.WriteLine("KeyBind Initiated: {0}", KeyPressName);
            InputSimulator inputSimulator = new InputSimulator();
            VirtualKeyCode? buttonHeld = null;

            // Check for keybind test
            if (!string.IsNullOrEmpty(KeyPressName) && KeyPressName.ToLower().Equals("test"))
            {
                return true;
            }

            // Is a modifier key needed..
            if (KeyPressName.Length > 1 && KeyPressName.Contains("+"))
            {
                if (KeyPressName.Contains("LSHIFT"))
                {
                    buttonHeld = VirtualKeyCode.LSHIFT;
                }
                else if (KeyPressName.Contains("RSHIFT"))
                {
                    buttonHeld = VirtualKeyCode.RSHIFT;
                }
                else if (KeyPressName.Contains("LCONTROL"))
                {
                    buttonHeld = VirtualKeyCode.LCONTROL;
                }
                else if (KeyPressName.Contains("RCONTROL"))
                {
                    buttonHeld = VirtualKeyCode.RCONTROL;
                }
                else if (KeyPressName.Contains("LALT"))
                {
                    buttonHeld = VirtualKeyCode.MENU;
                }
                else if (KeyPressName.Contains("RALT"))
                {
                    buttonHeld = VirtualKeyCode.RMENU;
                }
                inputSimulator.Keyboard.KeyDown(buttonHeld.Value);
                KeyPressName = KeyPressName.Split("+").Last();
            }

            VirtualKeyCode[] ListOfKeyPresses = Enum.GetValues<VirtualKeyCode>();
            VirtualKeyCode? KeyToPress = ListOfKeyPresses.FirstOrDefault(a => a.ToString().Equals(KeyPressName));
            if (KeyToPress != null)
            {
                inputSimulator.Keyboard.KeyPress(KeyToPress.Value);
            }

            if (buttonHeld != null)
            {
                inputSimulator.Keyboard.KeyUp(buttonHeld.Value);
                buttonHeld = null;
            }

            Console.WriteLine(rBind);

            return true;
        }

    }
}
