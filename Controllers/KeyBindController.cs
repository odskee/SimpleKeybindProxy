using Microsoft.Extensions.Logging;
using SimpleKeybindProxy.Interfaces;
using SimpleKeybindProxy.Models.SocketRequest;
using SimpleKeybindProxy.Models.SocketResponse;
using WindowsInput;
using WindowsInput.Native;


namespace SimpleKeybindProxy.Controllers
{


    public partial class KeyBindController : IKeyBindController
    {
        public partial Task<bool> LoadKeyBindLibraryAsync();
        public partial Task<KeybindResponse> ProcessKeyBindRequestAsync(CommandRequest RequestedKeybindCommand);

    }

    public partial class KeyBindController : IKeyBindController
    {
        public ICollection<KeyValuePair<string, string>> BindLibrary { get; set; }
        public string BindLibraryLocation { get; set; }
        public enum KeypressType
        {
            KeyPress,
            KeyHold,
            KeyRelease
        }
        public IOutputController OutputController { get; set; }



        private readonly ILogger logger;
        private readonly ProgramOptionsController ProgramOptions;

        public KeyBindController(ILogger<KeyBindController> _logger, IProgramOptionsController _programOptionsController, IOutputController _outputController)
        {
            BindLibrary = new List<KeyValuePair<string, string>>();
            ProgramOptions = (ProgramOptionsController)_programOptionsController;
            logger = _logger;
            OutputController = _outputController;

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
                                OutputController.StandardOutput("A duplicate keybind was detected: {0}", paring[0].Trim());

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
            OutputController.StandardOutput("Bind Libarary loaded successfully.  {0} total binds found.", BindLibrary.Count);
            return true;
        }


        // Processing and issues a keypress based on the bind name / key mapping
        public partial async Task<KeybindResponse> ProcessKeyBindRequestAsync(CommandRequest RequestedKeybindCommand)
        {
            KeybindResponse keybindResponse = new KeybindResponse() { Success = false };

            if (RequestedKeybindCommand != null && !string.IsNullOrEmpty(RequestedKeybindCommand.Command) && RequestedKeybindCommand.CommandData.Any())
            {
                string RequestedBindName = RequestedKeybindCommand?.CommandData?.FirstOrDefault() ?? "";
                KeyBindController.KeypressType KeypressRequestType;

                // if there is no keybind type provided, we assume normal keypress event and check the binds file to confirm
                KeypressRequestType = KeyBindController.KeypressType.KeyPress;

                if (RequestedKeybindCommand.Command.ToLower().Equals("keybind_hold"))
                {
                    KeypressRequestType = KeyBindController.KeypressType.KeyHold;
                }
                else if (RequestedKeybindCommand.Command.ToLower().Equals("keybind_release"))
                {
                    KeypressRequestType = KeyBindController.KeypressType.KeyRelease;
                }


                if (BindLibrary.Count == 0)
                {
                    if (!await LoadKeyBindLibraryAsync())
                    {
                        keybindResponse.Success = false;
                        keybindResponse.ResponseMessage = "No keybind dictionary found";
                    }
                }

                // Get the matching name to keypress pair from the keybind file
                KeyValuePair<string, string>? bindMatch = BindLibrary.FirstOrDefault(a => a.Key.Equals(RequestedBindName));
                if (bindMatch.Value.Key == null || bindMatch.Value.Value == null)
                {
                    // The result we got was null, meaning no match was found
                    OutputController.DebugOutput("Unknown Bind: {0}", RequestedBindName);
                    logger.LogDebug("Unknown Bind Requested: {0}", RequestedBindName);
                    keybindResponse.Success = false;
                    keybindResponse.ResponseMessage = "Unknown Bind Name";
                }
                else
                {
                    // Good result matching request from dictionary
                    string KeyPressName = bindMatch.Value.Key;
                    string KeyPressValue = bindMatch.Value.Value;


                    // Log output
                    logger.LogInformation("KeyBind {0} Initiated: {1}", KeyPressName, KeyPressValue);

                    // Check for keybind test
                    if (!string.IsNullOrEmpty(KeyPressName) && KeyPressName.ToLower().Equals("test"))
                    {
                        ExecuteKeybindTest(keybindResponse);
                    }


                    // Generate list of keypresses
                    VirtualKeyCode[] ListOfKeyPresses = Enum.GetValues<VirtualKeyCode>();

                    // Figure out what type of keypress we need to do
                    string[] keyPressList = KeyPressValue.Split("+");
                    ICollection<VirtualKeyCode> ModifierKeysList = new List<VirtualKeyCode>();
                    ICollection<VirtualKeyCode> KeyPressKeys = new List<VirtualKeyCode>();
                    foreach (string key in keyPressList)
                    {
                        if (!ListOfKeyPresses.Any(a => a.ToString().Equals(key)))
                        {
                            // There are no instances of this key in the available to press, can't continue
                            logger.LogError("Keypress is not valid: {0}", key);
                            keybindResponse.Success = false;
                            keybindResponse.ResponseMessage = "Unknown Keypress Name";
                            return keybindResponse;
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

                    bool KeybindPressResp = false;
                    if (KeyPressValue.Split("+").Length == 1)
                    {
                        // only one keypress to issue
                        KeybindPressResp = KeyBindRequest(KeypressRequestType, ListOfKeyPresses.FirstOrDefault(a => a.ToString().Equals(keyPressList[0])));
                    }
                    else
                    {
                        KeybindPressResp = KeyBindRequest(KeypressRequestType, null, ModifierKeysList, KeyPressKeys);
                    }

                    if (KeybindPressResp)
                    {
                        keybindResponse.Success = true;

                        keybindResponse.ModifierCombination = new List<string>();
                        foreach (var mc in ModifierKeysList)
                        {
                            keybindResponse.ModifierCombination.Add(mc.ToString());
                        }

                        keybindResponse.KeypressCombination = new List<string>();
                        foreach (var mc in KeyPressKeys)
                        {
                            keybindResponse.KeypressCombination.Add(mc.ToString());
                        }

                        keybindResponse.KeybindName = KeyPressName;
                    }


                    OutputController.StandardOutput("Keybind: {0}", RequestedBindName);
                }
            }
            return keybindResponse;
        }


        // Handles the actual keybind request using Input Simulator.  Checks to ensure noissue has not been enabled.
        private bool KeyBindRequest(KeypressType TypeOfKeypress, VirtualKeyCode? SingleKeyToPress = null, ICollection<VirtualKeyCode>? ModifierKeysList = null, ICollection<VirtualKeyCode>? KeyPressList = null)
        {
            if (ProgramOptions.ProgramOptions.PreventBindIssue == true)
            {
                return true;
            }

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
                        if (!ProgramOptions.ProgramOptions.PreventBindIssue)
                        {
                            inputSimulator.Keyboard.KeyPress(SingleKeyToPress.Value);
                        }
                        logger.LogDebug("Keyboard input (KeyPress): {0}", SingleKeyToPress.Value);
                        break;

                        case KeypressType.KeyHold:
                        if (!ProgramOptions.ProgramOptions.PreventBindIssue)
                        {
                            inputSimulator.Keyboard.KeyDown(SingleKeyToPress.Value);
                        }
                        logger.LogDebug("Keyboard input (KeyDown): {0}", SingleKeyToPress.Value);
                        break;

                        case KeypressType.KeyRelease:
                        if (!ProgramOptions.ProgramOptions.PreventBindIssue)
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
                        if (!ProgramOptions.ProgramOptions.PreventBindIssue)
                        {
                            inputSimulator.Keyboard.ModifiedKeyStroke(ModifierKeysList, KeyPressList);
                        }
                        logger.LogDebug("Keyboard input (Modified): {0}+{1}", ModifierKeysList, KeyPressList);
                        break;

                        case KeypressType.KeyHold:
                        foreach (VirtualKeyCode keyToPress in ModifierKeysList)
                        {
                            if (!ProgramOptions.ProgramOptions.PreventBindIssue)
                            {
                                inputSimulator.Keyboard.KeyDown(keyToPress);
                            }
                            logger.LogDebug("Keyboard input (KeyPress): {0}", keyToPress);
                        }
                        foreach (VirtualKeyCode keyToPress in KeyPressList)
                        {
                            if (!ProgramOptions.ProgramOptions.PreventBindIssue)
                            {
                                inputSimulator.Keyboard.KeyDown(keyToPress);
                            }
                            logger.LogDebug("Keyboard input (KeyPress): {0}", keyToPress);
                        }
                        break;

                        case KeypressType.KeyRelease:
                        foreach (VirtualKeyCode keyToPress in ModifierKeysList)
                        {
                            if (!ProgramOptions.ProgramOptions.PreventBindIssue)
                            {
                                inputSimulator.Keyboard.KeyUp(keyToPress);
                            }
                            logger.LogDebug("Keyboard input (KeyUp): {0}", keyToPress);

                        }
                        foreach (VirtualKeyCode keyToPress in KeyPressList)
                        {
                            if (!ProgramOptions.ProgramOptions.PreventBindIssue)
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
        public void SetProgramOptions()
        {
            BindLibraryLocation = ProgramOptions.ProgramOptions.BindLocation;
        }


        // Sets the bind library location
        public void SetBindLibraryLocation(string bindLibraryLocation)
        {
            BindLibraryLocation = bindLibraryLocation;
        }


        // Returns the count of the current BindLibrary list
        public int GetBindLibraryCount()
        {
            return BindLibrary.Count;
        }


        // Handles a specific test scenario
        private KeybindResponse ExecuteKeybindTest(KeybindResponse KeybindResponse)
        {
            OutputController.StandardOutput("Test Keybind Pressed");
            logger.LogInformation("Test Keybind Pressed");
            KeybindResponse.Success = true;
            KeybindResponse.ResponseMessage = "The test was successful";
            return KeybindResponse;
        }


    }
}
