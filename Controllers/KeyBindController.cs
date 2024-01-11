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
        public partial Task<bool> ProcessKeyBindRequestAsync(string RequestedBindName, KeypressType KeypressRequestType);

    }

    public partial class KeyBindController : IKeyBindController
    {
        public ICollection<KeyValuePair<string, string>> BindLibrary { get; set; }
        public string BindLibraryLocation { get; set; }
        public ProgramOptions Options { get; set; }
        public enum KeypressType
        {
            KeyPress,
            KeyHold,
            KeyRelease
        }



        private readonly ILogger logger;


        public KeyBindController(ILogger<HttpServer> _logger)
        {
            BindLibrary = new List<KeyValuePair<string, string>>();
            logger = _logger;
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
                    BindLibrary.Clear();
                    foreach (string toLoad in bindsToLoad.Where(a => a.Contains(".txt")))
                    {
                        string[] _bindlibrary = await File.ReadAllLinesAsync(toLoad);
                        foreach (string bind in _bindlibrary)
                        {
                            string[] paring = bind.Split(",");

                            if (BindLibrary.Any(a => a.Key.Equals(paring[0].Trim())))
                            {
                                // make sure we don't already have a bind with the same name
                                logger.LogInformation("A duplicate keybind name was detected: {0}", paring[0].Trim());
                                Console.WriteLine("A duplicate keybind was detected: {0}", paring[0].Trim());
                            }
                            else
                            {
                                BindLibrary.Add(new KeyValuePair<string, string>(paring[0].Trim(), paring[1].Trim()));
                            }

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
        public partial async Task<bool> ProcessKeyBindRequestAsync(string RequestedBindName, KeypressType KeypressRequestType)
        {
            if (BindLibrary.Count == 0)
            {
                if (!await LoadKeyBindLibraryAsync())
                {
                    return false;
                }
            }

            // Get the matching name to keypress pair from the keybind file
            KeyValuePair<string, string>? bindMatch = BindLibrary.FirstOrDefault(a => a.Key.Equals(RequestedBindName));
            if (bindMatch.Value.Key == null || bindMatch.Value.Value == null)
            {
                // The result we got was null, meaning no match was found
                if (Options.VerbosityLevel > 1)
                {
                    Console.WriteLine("Unknown Bind: {0}", RequestedBindName);
                }
                logger.LogDebug("Unknown Bind Requested: {0}", RequestedBindName);
                return false;
            }
            string KeyPressName = bindMatch.Value.Key;
            string KeyPressValue = bindMatch.Value.Value;


            // 
            logger.LogInformation("KeyBind {0} Initiated: {1}", KeyPressName, KeyPressValue);
            if (Options.VerbosityLevel > 1)
            {
                Console.WriteLine("KeyBind {0} Initiated: {1}", KeyPressName, KeyPressValue);
            }


            // Generate list of keypresses
            VirtualKeyCode[] ListOfKeyPresses = Enum.GetValues<VirtualKeyCode>();

            // Check for keybind test
            if (!string.IsNullOrEmpty(KeyPressName) && KeyPressName.ToLower().Equals("test"))
            {
                Console.WriteLine("Test Keybind Pressed");
                logger.LogInformation("Test Keybind Pressed");
                return true;
            }


            // Figure out what type of keypress we need to do
            string[] keyPressList = KeyPressValue.Split("+");
            if (keyPressList.Length == 1)
            {
                // if there is only one key in the list, we can check now and request the keypress
                KeyBindRequest(KeypressRequestType, ListOfKeyPresses.FirstOrDefault(a => a.ToString().Equals(keyPressList[0])));
            }
            else
            {
                ICollection<VirtualKeyCode> ModifierKeysList = new List<VirtualKeyCode>();
                ICollection<VirtualKeyCode> KeyPressKeys = new List<VirtualKeyCode>();
                foreach (string key in keyPressList)
                {
                    if (!ListOfKeyPresses.Any(a => a.ToString().Equals(key)))
                    {
                        // There are no instances of this key in the available to press, can't continue
                        logger.LogError("Keypress is not valid: {0}", key);
                        return false;
                    }

                    if (!keyPressList.Last().ToString().Equals(key))
                    {
                        // This is a modifier key(s), anything between + are modifiers, last entry will either be single key or keys separated by #
                        ModifierKeysList.Add(ListOfKeyPresses.FirstOrDefault(a => a.ToString().Equals(key)));
                    }
                    else
                    {
                        // Last keypress(s) in the list, any further ones seperated by #
                        if (key.Contains("#"))
                        {
                            string[] listOfNonModifierKeys = key.Split("#");
                            foreach (string nonModifierKey in listOfNonModifierKeys)
                            {
                                KeyPressKeys.Add(ListOfKeyPresses.FirstOrDefault(a => a.ToString().Equals(nonModifierKey)));
                            }
                        }
                        else
                        {
                            KeyPressKeys.Add(ListOfKeyPresses.FirstOrDefault(a => a.ToString().Equals(key)));

                        }
                    }
                }
                KeyBindRequest(KeypressRequestType, null, ModifierKeysList, KeyPressKeys);
            }


            Console.WriteLine("Keybind: {0}", RequestedBindName);
            return true;
        }


        // Handles the actual keybind request using Input Simulator.  Checks to ensure noissue has not been enabled.
        private bool KeyBindRequest(KeypressType TypeOfKeypress, VirtualKeyCode? SingleKeyToPress = null, ICollection<VirtualKeyCode>? ModifierKeysList = null, ICollection<VirtualKeyCode>? KeyPressList = null)
        {
            InputSimulator inputSimulator = new InputSimulator();

            if (!SingleKeyToPress.HasValue && ModifierKeysList?.Count == 0 && KeyPressList?.Count == 0)
            {
                logger.LogWarning("Keybind requested but binds not provided");
                return false;
            }

            try
            {
                if (SingleKeyToPress.HasValue)
                {
                    // If we have a single key press request, detect type and issue request
                    switch (TypeOfKeypress)
                    {
                        case KeypressType.KeyPress:
                        if (!Options.PreventBindIssue)
                        {
                            inputSimulator.Keyboard.KeyPress(SingleKeyToPress.Value);
                        }
                        logger.LogDebug("Keyboard input (KeyPress): {0}", SingleKeyToPress.Value);
                        break;

                        case KeypressType.KeyHold:
                        if (!Options.PreventBindIssue)
                        {
                            inputSimulator.Keyboard.KeyDown(SingleKeyToPress.Value);
                        }
                        logger.LogDebug("Keyboard input (KeyDown): {0}", SingleKeyToPress.Value);
                        break;

                        case KeypressType.KeyRelease:
                        if (!Options.PreventBindIssue)
                        {
                            inputSimulator.Keyboard.KeyUp(SingleKeyToPress.Value);
                        }
                        logger.LogDebug("Keyboard input (KeyUp): {0}", SingleKeyToPress.Value);
                        break;
                    }
                }


                if (ModifierKeysList?.Count > 0 || KeyPressList?.Count > 0)
                {
                    // If there are a list of either modifier or normal keypresses, detect the type and issue them.
                    switch (TypeOfKeypress)
                    {
                        case KeypressType.KeyPress:
                        if (!Options.PreventBindIssue)
                        {
                            inputSimulator.Keyboard.ModifiedKeyStroke(ModifierKeysList, KeyPressList);
                        }
                        logger.LogDebug("Keyboard input (Modified): {0}+{1}", ModifierKeysList, KeyPressList);
                        break;

                        case KeypressType.KeyHold:
                        foreach (VirtualKeyCode keyToPress in ModifierKeysList)
                        {
                            if (!Options.PreventBindIssue)
                            {
                                inputSimulator.Keyboard.KeyDown(keyToPress);
                            }
                            logger.LogDebug("Keyboard input (KeyPress): {0}", keyToPress);
                        }
                        foreach (VirtualKeyCode keyToPress in KeyPressList)
                        {
                            if (!Options.PreventBindIssue)
                            {
                                inputSimulator.Keyboard.KeyDown(keyToPress);
                            }
                            logger.LogDebug("Keyboard input (KeyPress): {0}", keyToPress);
                        }
                        break;

                        case KeypressType.KeyRelease:
                        foreach (VirtualKeyCode keyToPress in ModifierKeysList)
                        {
                            if (!Options.PreventBindIssue)
                            {
                                inputSimulator.Keyboard.KeyUp(keyToPress);
                            }
                            logger.LogDebug("Keyboard input (KeyUp): {0}", keyToPress);

                        }
                        foreach (VirtualKeyCode keyToPress in KeyPressList)
                        {
                            if (!Options.PreventBindIssue)
                            {
                                inputSimulator.Keyboard.KeyUp(keyToPress);
                            }
                            logger.LogDebug("Keyboard input (KeyUp): {0}", keyToPress);
                        }
                        break;
                    }
                }

                return true;

            }
            catch (Exception ex)
            {
                logger.LogCritical(1, ex, "Error while Switching keypress type");
                return false;
            }


        }


        // Sets the program options property
        public void SetProgramOptions(ProgramOptions options)
        {
            Options = options;
        }


        // Sets the bind library locaion
        public void SetBindLibraryLocation(string bindLibraryLocation)
        {
            BindLibraryLocation = bindLibraryLocation;
        }


        // Returns the count of the current BindLibrary list
        public int GetBindLibraryCount()
        {
            return BindLibrary.Count;
        }

    }
}
