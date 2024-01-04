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
                if (Directory.Exists(BindLibraryLocation))
                {
                    IEnumerable<string> bindsToLoad = Directory.EnumerateFiles(BindLibraryLocation);
                    foreach (string toLoad in bindsToLoad.Where(a => a.Contains(".txt")))
                    {
                        string[] _bindlibrary = await File.ReadAllLinesAsync(toLoad);
                        foreach (string bind in _bindlibrary)
                        {
                            string[] paring = bind.Split(",");
                            BindLibrary.Add(new KeyValuePair<string, string>(paring[0].Trim(), paring[1].Trim()));
                        }
                    }
                }
                else
                {
                    Console.WriteLine("The provided Binds directory {} could not be loaded", BindLibraryLocation);
                    return false;
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine("The provided Bind directory {0} could not be found", BindLibraryLocation);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("Insufficient persmissions to access Bind directory {0}", BindLibraryLocation);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem accessing your Bind Library: {0}{1}", Environment.NewLine, ex.Message);
                return false;
            }

            Console.WriteLine("Bind Libarary loaded successfully.  {0} total binds found.", BindLibrary.Count);
            return true;
        }

        // Processing and issues a keypress based on the bind name / key mapping
        public partial async Task<bool> ProcessKeyBindRequestAsync(string bindRequest)
        {
            if (BindLibrary.Count == 0)
            {
                if (!await LoadKeyBindLibraryAsync())
                {
                    return false;
                }
            }

            string rBind = bindRequest.Split("/").First(a => a.Contains("KeyBind_")).Replace("KeyBind_", "");
            KeyValuePair<string, string>? bindMatch = BindLibrary.FirstOrDefault(a => a.Key.Equals(rBind));
            if (bindMatch.Value.Key == null || bindMatch.Value.Value == null)
            {
                Console.WriteLine("Unknown Bind: {0}", rBind);
                return false;
            }

            string KeyPressName = "";
            KeyPressName = bindMatch.Value.Value;
            Console.WriteLine("KeyBind {0} Initiated: {1}", bindMatch.Value.Key, KeyPressName);

            // Prep inputSimulator
            InputSimulator inputSimulator = new InputSimulator();
            List<VirtualKeyCode> buttonHeld = new List<VirtualKeyCode>();
            VirtualKeyCode? buttonPress = null;
            VirtualKeyCode[] ListOfKeyPresses = Enum.GetValues<VirtualKeyCode>();

            string[] ModifierKeys = { "LSHIFT", "RSHIFT", "LCONTROL", "RCONTROL", "LMENU", "RMENU" };

            // Check for keybind test
            if (!string.IsNullOrEmpty(KeyPressName) && KeyPressName.ToLower().Equals("test"))
            {
                return true;
            }

            // Is a modifier key needed..
            if (KeyPressName.Length > 1 && KeyPressName.Contains("+"))
            {
                string[] keyPressList = KeyPressName.Split("+");
                foreach (string key in keyPressList)
                {
                    if (ModifierKeys.Any(a => a.Equals(key)))
                    {
                        // Key is either "LSHIFT", "RSHIFT", "LCONTROL", "RCONTROL", "LMENU", "RMENU"
                        buttonHeld.Add(ListOfKeyPresses.FirstOrDefault(a => a.ToString().Equals(key)));
                    }
                    else
                    {
                        buttonPress = ListOfKeyPresses.FirstOrDefault(a => a.ToString().Equals(key));
                    }
                }
            }
            else
            {
                buttonPress = ListOfKeyPresses.FirstOrDefault(a => a.ToString().Equals(KeyPressName));
            }

            if (buttonHeld.Count > 0)
            {
                foreach (VirtualKeyCode key in buttonHeld)
                {
                    inputSimulator.Keyboard.KeyDown(key);
                }
            }
            if (buttonPress.HasValue)
            {
                inputSimulator.Keyboard.KeyPress(buttonPress.Value);
            }

            Console.WriteLine(rBind);

            return true;
        }

    }
}
