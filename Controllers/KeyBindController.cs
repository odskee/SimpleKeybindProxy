using Microsoft.Extensions.Logging;
using SimpleKeybindProxy.Interfaces;
using SimpleKeybindProxy.Models;
using SimpleKeybindProxy.Models.SocketRequest.Commands;
using SimpleKeybindProxy.Models.SocketResponse.Commands;
using WindowsInput;
using WindowsInput.Native;


namespace SimpleKeybindProxy.Controllers
{


    public partial class KeyBindController : IKeyBindController
    {
        public partial Task<bool> LoadKeyBindLibraryAsync();
        public partial Task<KeybindResponse> ProcessKeyBindRequestAsync(KeybindRequest RequestedKeybindCommand);

    }

    public partial class KeyBindController : IKeyBindController
    {
        //public ICollection<KeyValuePair<string, string>> BindLibrary { get; set; }
        public ICollection<KeyBindEntry> KeybindLibrary { get; set; }

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
            KeybindLibrary = new List<KeyBindEntry>();
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
                    IEnumerable<string> bindsToLoad = Directory.EnumerateFiles(BindLibraryLocation, "", SearchOption.AllDirectories);
                    KeybindLibrary.Clear();
                    if (!bindsToLoad.Any())
                    {
                        OutputController.StandardOutput("A bind library was found but no binds have been loaded");
                    }
                    else
                    {
                        foreach (string toLoad in bindsToLoad.Where(a => a.Contains(".binds")))
                        {
                            string[] _bindlibrary = await File.ReadAllLinesAsync(toLoad);
                            foreach (string bind in _bindlibrary)
                            {
                                string bindNormalised = bind;
                                // there needs to be 4 sections to each bind, add missing end comma to complete missing segment
                                if (bind.Split(",").Count() == 3 && !bind.EndsWith(","))
                                {
                                    bindNormalised += ",";
                                }

                                string[] paring = bindNormalised.Split(",");

                                if (paring.Length == 4)
                                {
                                    KeyBindEntry newEntry = new KeyBindEntry()
                                    {
                                        BindName = paring[0],
                                        Modifiers = paring[1].Split("+"),
                                        Keypress = paring[2].Split("+"),
                                    };
                                    KeypressType ty;
                                    Enum.TryParse(paring[3], out ty);
                                    newEntry.PressType = ty;

                                    if (KeybindLibrary.Any(a => a.BindName.Equals(paring[0])))
                                    {
                                        // make sure we don't already have a bind with the same name
                                        logger.LogInformation("A duplicate keybind name was detected: {0}", paring[0].Trim());
                                        OutputController.StandardOutput("A duplicate keybind was detected: {0}", paring[0].Trim());
                                    }
                                    else
                                    {
                                        KeybindLibrary.Add(newEntry);
                                    }
                                }
                                else
                                {
                                    // This bind isn't defined properly, ignore it
                                    OutputController.StandardOutput("Undefined Bind Skipped: {0}", bind);
                                }

                                logger.LogDebug("Keybind Mapping Detected: {0} maps to {1}", paring[0].Trim(), paring[1].Trim());
                            }
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

            logger.LogInformation("Bind Libarary loaded successfully.  {0} total binds found.", KeybindLibrary.Count);
            OutputController.StandardOutput("Bind Libarary loaded successfully.  {0} total binds found.", KeybindLibrary.Count);
            return true;
        }


        // Processing and issues a keypress based on the bind name / key mapping
        public partial async Task<KeybindResponse> ProcessKeyBindRequestAsync(KeybindRequest RequestedKeybindCommand)
        {
            KeybindResponse keybindResponse = new KeybindResponse() { Success = false };

            if (RequestedKeybindCommand != null && !string.IsNullOrEmpty(RequestedKeybindCommand.Command) && !string.IsNullOrEmpty(RequestedKeybindCommand?.BindName))
            {

                string RequestedBindName = RequestedKeybindCommand?.BindName ?? string.Empty;
                KeypressType KeypressRequestType = KeypressType.KeyPress;       // Keypress is the default action
                KeypressType KeypressDeclaredType = KeypressType.KeyPress;       // Keypress is the default action


                // Use if / else as Enum.TryParse is caps sensitive
                if (!string.IsNullOrEmpty(RequestedKeybindCommand.PressType))
                {
                    if (RequestedKeybindCommand.PressType.ToLower().Equals(KeypressType.KeyHold.ToString().ToLower()))
                    {
                        KeypressRequestType = KeyBindController.KeypressType.KeyHold;
                    }
                    else if (RequestedKeybindCommand.PressType.ToLower().Equals(KeypressType.KeyRelease.ToString().ToLower()))
                    {
                        KeypressRequestType = KeyBindController.KeypressType.KeyRelease;
                    }
                }

                // Check we have a loaded bind library - required for further execution
                if (KeybindLibrary.Count == 0)
                {
                    if (!await LoadKeyBindLibraryAsync())
                    {
                        keybindResponse.Success = false;
                        keybindResponse.ResponseMessage = "No keybind dictionary found";
                    }
                }

                // Get the matching name to keypress pair from the keybind file
                KeyBindEntry? bindMatch = KeybindLibrary.FirstOrDefault(a => a.BindName.Equals(RequestedBindName));
                if (bindMatch == null)
                {
                    // The result we got was null, meaning no match was found
                    OutputController.DebugOutput("Unknown Bind: {0}", RequestedBindName);
                    logger.LogDebug("Unknown Bind Requested: {0}", RequestedBindName);
                    keybindResponse.Success = false;
                    keybindResponse.ResponseMessage = "Unknown Bind Name";
                }
                else
                {
                    // Log output
                    logger.LogInformation("KeyBind {0} Initiated", bindMatch.BindName);

                    // Check for keybind test
                    if (!string.IsNullOrEmpty(bindMatch.BindName) && bindMatch.BindName.ToLower().Equals("test"))
                    {
                        ExecuteKeybindTest(keybindResponse);
                    }

                    // Declared press types (those in the bind file) override the requested type
                    if (bindMatch.PressType != null && bindMatch.PressType.HasValue)
                    {
                        KeypressRequestType = bindMatch.PressType.Value;
                    }

                    // Generate list of keypresses
                    IEnumerable<VirtualKeyCode> ListOfKeyPresses = Enum.GetValues<VirtualKeyCode>();
                    IEnumerable<VirtualKeyCode> ModifierKeysList = new List<VirtualKeyCode>();
                    IEnumerable<VirtualKeyCode> KeyPressKeys = new List<VirtualKeyCode>();


                    ModifierKeysList.Concat(ListOfKeyPresses.Where(a => bindMatch.Modifiers.Contains(a.ToString())));
                    KeyPressKeys = KeyPressKeys.Concat(ListOfKeyPresses.Where(a => bindMatch.Keypress.Contains(a.ToString())));

                    // Process our request
                    bool KeybindPressResp = false;
                    KeybindPressResp = KeyBindRequest(KeypressRequestType, ModifierKeysList.ToList(), KeyPressKeys.ToList());

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

                        keybindResponse.KeybindName = bindMatch.BindName;
                    }


                    OutputController.StandardOutput("Keybind: {0}", RequestedBindName);
                }
            }
            return keybindResponse;
        }


        // Handles the actual keybind request using Input Simulator.  Checks to ensure noissue has not been enabled.
        private bool KeyBindRequest(KeypressType TypeOfKeypress, ICollection<VirtualKeyCode>? ModifierKeysList = null, ICollection<VirtualKeyCode>? KeyPressList = null)
        {
            if (ProgramOptions.ProgramOptions.PreventBindIssue == true)
            {
                return true;
            }

            InputSimulator inputSimulator = new InputSimulator();

            if (ModifierKeysList?.Count == 0 && KeyPressList?.Count == 0)
            {
                logger.LogWarning("Keybind requested but binds not provided");
                return false;
            }

            try
            {
                if (TypeOfKeypress == KeypressType.KeyPress)
                {
                    inputSimulator.Keyboard.ModifiedKeyStroke(ModifierKeysList, KeyPressList);
                }
                else
                {
                    // Keys need to be held down until told to release
                    if (ModifierKeysList?.Count > 0)
                        ActionInputList(ModifierKeysList, TypeOfKeypress, inputSimulator);

                    if (KeyPressList.Count > 0)
                        ActionInputList(KeyPressList, TypeOfKeypress, inputSimulator);
                }
                return true;

            }
            catch (Exception ex)
            {
                logger.LogCritical(1, ex, "Error while Switching keypress type");
                return false;
            }


        }

        // Cycles through the provided key press list and either holds or release based on TypeOfKeypress
        public void ActionInputList(ICollection<VirtualKeyCode> Keys, KeypressType TypeOfKeypress, InputSimulator inputSimulator)
        {
            foreach (VirtualKeyCode keyToPress in Keys)
            {
                if (!ProgramOptions.ProgramOptions.PreventBindIssue)
                {
                    if (TypeOfKeypress == KeypressType.KeyHold)
                    {
                        inputSimulator.Keyboard.KeyDown(keyToPress);
                    }
                    else if (TypeOfKeypress == KeypressType.KeyRelease)
                    {
                        inputSimulator.Keyboard.KeyDown(keyToPress);
                    }
                }
                logger.LogDebug("Keyboard input (KeyPress): {0}", keyToPress);
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
            return KeybindLibrary.Count;
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
