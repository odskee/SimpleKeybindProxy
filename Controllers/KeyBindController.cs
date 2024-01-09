using Controllers.SimpleWebService;
using Microsoft.Extensions.Logging;
using SimpleKeybindProxy.Interfaces;
using SimpleKeybindProxy.Models;
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
        public ProgramOptions Options { get; set; }

        private readonly ILogger logger;


        public KeyBindController(ILogger<HttpServer> _logger)
        {
            BindLibrary = new List<KeyValuePair<string, string>>();
            logger = _logger;
        }


        public void SetProgramOptions(ProgramOptions options)
        {
            Options = options;
        }

        public void SetBindLibraryLocation(string bindLibraryLocation)
        {
            BindLibraryLocation = bindLibraryLocation;
        }

        public int GetBindLibraryCount()
        {
            return BindLibrary.Count;
        }

        // Loads the KeyBind File and creates a KeyValue Pair collection
        public partial async Task<bool> LoadKeyBindLibraryAsync()
        {
            // Load Keybind File
            try
            {
                if (Directory.Exists(BindLibraryLocation))
                {
                    logger.LogDebug("Keybind directory found: {0}", BindLibraryLocation);
                    IEnumerable<string> bindsToLoad = Directory.EnumerateFiles(BindLibraryLocation);
                    foreach (string toLoad in bindsToLoad.Where(a => a.Contains(".txt")))
                    {
                        string[] _bindlibrary = await File.ReadAllLinesAsync(toLoad);
                        foreach (string bind in _bindlibrary)
                        {
                            string[] paring = bind.Split(",");
                            BindLibrary.Add(new KeyValuePair<string, string>(paring[0].Trim(), paring[1].Trim()));

                            logger.LogDebug("Keybind Mapping Detected: {0} maps to {1}", paring[0].Trim(), paring[1].Trim());
                        }
                    }
                }
                else
                {
                    logger.LogError($"The provided Binds directory {BindLibraryLocation} could not be loaded");
                    return false;
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                logger.LogCritical(1, ex, "The provided Bind directory {0} could not be found");
                logger.LogDebug("The provided Bind directory could not be found: {0}", ex.Message);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogCritical(1, ex, "Insufficient permissions to access Bind directory:");
                logger.LogDebug("Insufficient permissions to access Bind directory: {0}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogCritical(1, ex, "There was a problem accessing your Bind Library");
                return false;
            }

            logger.LogInformation("Bind Libarary loaded successfully.  {0} total binds found.", BindLibrary.Count);
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
                if (Options.VerbosityLevel > 1)
                {
                    Console.WriteLine("Unknown Bind: {0}", rBind);
                }
                logger.LogDebug("Unknown Bind Requested: {0}", rBind);

                return false;
            }

            string KeyPressName = "";
            KeyPressName = bindMatch.Value.Value;
            logger.LogInformation("KeyBind {0} Initiated: {1}", bindMatch.Value.Key, KeyPressName);
            if (Options.VerbosityLevel > 1)
            {
                Console.WriteLine("KeyBind {0} Initiated: {1}", bindMatch.Value.Key, KeyPressName);
            }


            // Prep inputSimulator
            InputSimulator inputSimulator = new InputSimulator();
            List<VirtualKeyCode> buttonHeld = new List<VirtualKeyCode>();
            VirtualKeyCode? buttonPress = null;
            VirtualKeyCode[] ListOfKeyPresses = Enum.GetValues<VirtualKeyCode>();

            string[] ModifierKeys = { "LSHIFT", "RSHIFT", "LCONTROL", "RCONTROL", "LMENU", "RMENU" };

            // Check for keybind test
            if (!string.IsNullOrEmpty(KeyPressName) && KeyPressName.ToLower().Equals("test"))
            {
                Console.WriteLine("Test Keybind Pressed");
                logger.LogInformation("Test Keybind Pressed");
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


            try
            {
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
                if (buttonHeld.Count > 0)
                {
                    foreach (VirtualKeyCode key in buttonHeld)
                    {
                        inputSimulator.Keyboard.KeyUp(key);
                    }
                }

                Console.WriteLine("Keybind: {0}", rBind);

            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
                return false;
            }

            return true;
        }

    }
}
