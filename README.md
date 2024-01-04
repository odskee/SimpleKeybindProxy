# SimpleKeybindProxy
A simple C# / .NET Console Keybind proxy

# Disclaimer
This started as a PoC project to teach some "cool" aspects about .NET to a young programmer - there are a number of optimisations and changes that can (and will) be made.  The project idea arrived from a desire to have a simple mechanism to remotely issue key presses to a Windows 10 PC that was: Free, easy to use, lightweight and didn't require registration or PPI.  Not finding much that was suitable, a goal was born.  Some code was taken from Benjamin N. Summerton / HttpsServer.cs.


# Description
A simple and very light weight Key Press Proxy server written in C# / .NET 8.  Starts a simple 'web server' accessible over local network and provides matching Landing Sites.  Landing Sites are web pages that show your desired input(s) / keybinds and are fully customisable.  For example, a Landing Site to represent a certain panel / area within your game can be created with appropriate button / switch graphics.  With Keybinds that are defined in a Keybind name to Key Press dictionary, an interaction on a Landing Site can request a keybind by name having the corresponding key press made on the computer running SinpleKeybindProxy.


# What Does it Do?
Allows you to create web pages that represent button/switch boxes that can then be accessed over the local network with a touchscreen device, allowing that device to input keypresses on the target computer.


# How Does it Work?
Running SimpleKeyBindProxy starts a simple web server - each time a request is made it is inspected for a Reserved Word (a command i.e. KeyBind_).  When a reserved word is not found, the requested Landing Site is shown based on the request URL.  When a command keyword is detected, that command is processed - for Keybinds, the name of the requested keybind is looked up in a dictionary for the desired key press(s).

Landing Sites are shown based on the request URl and must have a matching directory structure to the request.  Any request that contains a Reserved Word will work regardless of specified landing sites; if a command is given with no landing site in the URL, a generic error HTML page is shown.


# Setup
Once you have downloaded the release version:
> https://github.com/odskee/SimpleKeybindProxy/releases

And extract the zip file, you will have a SimpleKeyBindProxy folder - place this folder in a location you're happy with, this is where you will run it from.  By default, the SimpleKeyBindProxy folder contains both a "Binds" directory and a "Landing" directory - these are both required, although you can define an alternative location when starting the program.  There are two sample Landing Sites to give a very, very basic demonstration of their purpose.

You will also need to configure your binds.txt dictionary - by default this is in "SimpleKeybindProxy/Binds/binds.txt".  There are some pre-defined to act as a demonstration; they are not needed and can be removed / renamed.  The purpose of this file is to define a 'keybind name' and it's matching Key Press.  While the name can be anything you like, the associated Key Press must match exactly - a list of available key presses can be found in the "SimpleKeybindProxy/KeyPressNames.txt" file.  Note: Some experimentation may be required for symbols / certain keys due to language / regional differences.

To run the program, run SimpleKeybindProxy.exe AS AN ADMINISTRATOR - if you do not give the program admin permissions the server likely won't start.  By default, you can access your landing sites at "http://localhost:8001" (See Usage below on how to change this).

# Usage
## Run SimpleKeybindProxy.exe
Providing you are using the "Landing" and "Binds" folder contained within the same folder as SimpleKeybindProxy.exe, Right-Click and choose "Run as Administrator".  After a very short delay, the Keybind server is now running and listnening for requests.

## Run From Console
Running from the console allows you to change some settings.  To run from the command line, while holding Shift, right click in the SimpleKeybindProxy folder and choose 'open powershell window here'.  Alternatively, copy the folder location from the address bar and use 'cd' command in a powershell window (Start -> Search 'powershell' -> Right-Click and Run as Administrator):
```
cd "Paste_Here"
```

When running from the command line, you can provide the following arguments: 

```
.\SimpleKeybindProxy.exe -l - Define a custom Landing Site directory location.  Example: -l "C:\Folder1\LandingSites\"

.\SimpleKeybindProxy.exe -b - Define the directory that contains keybind dictionary.  Example: -b "C:\Folder1\Binds\"

.\SimpleKeybindProxy.exe -a - Define the IP address the server will listen for connections on.  Defaults to "*" or every address.  Example: -a 127.0.0.1

.\SimpleKeybindProxy.exe -p - Define the Port the server will listen for connections on.  Defaults to 8001.  Example: -p 1234
```

Once you have your binds defined and have created your landing site (or are using the samples), you can view them at _"http://localhost:8001/Directory_Structure_of_Landing_Site"_ (or the IP and Port you specified with -a / -p); for example, _http://localhost:8001/Landing1/_.

The URL request structure needs to match your Landing Site directory structure.  For example, if you add "SitesByBob/Panel_1/" into the Landing Site folder, you can view this landing site at http://localhost:8001/SitesByBob/Pane_1/.

When an interaction on a landing site requests a key press, the keybind name and Key Press will be displayed in the console output.  This can be tested using a "test" keybind name in the request - it does not need to be defined in a bind dictionary.  For Example, the following will mimic a Keybind request, visible in the consol output, and return the landing page for Landing1:
> http://localhost:8001/Landing1/KeyBind_test/

As shown above, you can make a manual key press by requesting the KeyBind URL directly.  To do this, navigate to http://server_address:server_port/KeyBind_name_of_keybind - you don't need to include a Landing page in your request but if you don't, the server will show you the default error HTML - your keybind has still been processed as shown in the console window.


# Setting Your Bind Dictionary
The bind dictionary is a simple txt file that specifies the name of a keybind, a comma (,) then the 'system' name for the key press - an example can be found at the bottom of this page.  A list of system names can also be found below.  You can combine as many modifiers (Shift, Control and Alt) as you like by using the plus (+) symbol but can only use one key at a time.  For example, LCONTROL+LMENU+LSHIFT+VK_T is fine but LCONTROL+VK_A+VK_T is not!

The names you list here will either be taken from a Landing page you're using or will be specified here for you to use in your own custom landing page.

# Making Custom Landing Sites
## Directory Structure
You can create as many landing sites as you want, providing you use an appropriate directory structure.  Create a new directory in the Landing Site location - this acts as your container.  You can either directly place resources such as HTML here or you can create further subdirectories.  For example, if you want to create multiple panels, you can create a structure that looks like "Landing\BobsPanels\Panel_1" and "Landing\BobsPanels\Panel_2" (These would be accessed by requesting /BobsPanels/Panel_1/ in the URL.

## Create the Landing Site
To create the landing site, add a new .html file - it **MUST** have the same name as the directory it resides in i.e. "Landing\BobsPanels\Panel_1\Panel_1.html".  If another name is used, the panel must be requested directly in the URL i.e. "/BobsPanels/Panel_1/index.html".

### Including Resources
Currently, the following resource types are supported:
* HTML
* CSS
* PNG
* SVG
* JPEG/JPG
* GIF

When you want to include a resource such as CSS stylesheet or images, you must use relative linking.  For example, if your CSS file is in the same directory as your html file, you would use href=".\style.css".

## Making Keybind Requests
You can issue a keybind request by making a POST request that terminates with "KeyBind_<name_of_Keybind>".  How you do this is up to you - one technique using JS is shown in the provided "Landing1" sample; by using the OnClick event and some JS, a POST request for a certain keybind name is sent when clicking on defined elements.

The name of the keybind is chosen by you and can be anything you want - each keybind name you use will need to be added to the binds.txt dictionary with a corresponding key press; it's suggested to name each keybind you create something intuitive and descriptive i.e. "LandingGearUp"

### JS Sample
The following shows a very simple example of how to use / make a keybind request:

JS / HTML:
```
<!DOCTYPE html>
<html>
    <head>
        <title>Simple Keybind Proxy</title>
        <link rel="stylesheet" href=".\style.css" type="text/css">
    </head>
    <body>
        <div OnCLick="IssueBind('MyCustomKeybind')">Press this Button</div>
        <script>
            function IssueBind(bindName) {
                var form = document.createElement('form');
                form.setAttribute('method', 'post');
                form.setAttribute('enctype', 'application/x-www-form-urlencoded');
        		    form.setAttribute('action', 'KeyBind_' + bindName);
                form.style.display = 'hidden';
                document.body.appendChild(form)
                form.submit();
            }
        </script>
        <header>
            <h1>Sample Simple CSS layout for buttons</h1>
        </header>
        <main>
            <div style="LeverStyle" onclick="IssueBind('LandingGear_Up')">Raise Gear</div>
        </main>
    </body>
</html>
```


## Special Notes
Currently, any favicon.ico requests are ignored by the server and will not be shown even if provided.

#

# Sample binds.txt
```
MyCustomName1,LCTRL+F
AnotherKeybind,LALT_F
GearDown,G
GearUp,LSHIFT+G
Test1,LCONTROL+VK_A
```

# Key Press Names
The following is a list of accepted KeyPress names - these are the values you add to the binds.txt file against a certain keybind name.  These are taken from InputSimulator which is used to simulate the key press ([http://inputsimulator.codeplex.com/](https://www.nuget.org/packages/InputSimulator/1.0.4)).
```
        
        Left mouse button
        LBUTTON

        
        Right mouse button
        RBUTTON

        
        Control-break processing
        CANCEL

        
        Middle mouse button (three-button mouse) - NOT contiguous with LBUTTON and RBUTTON
        MBUTTON

                
        BACKSPACE key
        BACK

        
        TAB key
        TAB

        
        CLEAR key
        CLEAR

        
        ENTER key
        RETURN 

        
        SHIFT key
        SHIFT

        
        CTRL key
        CONTROL

        
        ALT key
        MENU

        
        PAUSE key
        PAUSE

        
        CAPS LOCK key
        CAPITAL

        
        ESC key
        ESCAPE

        
        SPACEBAR
        SPACE

        
        PAGE UP key
        PRIOR

        
        PAGE DOWN key
        NEXT

        
        END key
        END

        
        HOME key
        HOME

        
        LEFT ARROW key
        LEFT

        
        UP ARROW key
        UP

        
        RIGHT ARROW key
        RIGHT

        
        DOWN ARROW key
        DOWN

        
        SELECT key
        SELECT

        
        PRINT key
        PRINT

        
        EXECUTE key
        EXECUTE

        
        PRINT SCREEN key
        SNAPSHOT

        
        INS key
        INSERT

        
        DEL key
        DELETE

        
        HELP key
        HELP

        
        A-Z, 0-9 (Don't Enter Braces) i.e. VK_A for A key.
        VK_<Key>

        
        Left Windows key (Microsoft Natural keyboard)
        LWIN

        
        Right Windows key (Natural keyboard)
        RWIN

        
        Computer Sleep key
        SLEEP

        
        Numeric keypad 0 key
        NUMPAD0

        
        Numeric keypad 1 key
        NUMPAD1

        
        Numeric keypad 2 key
        NUMPAD2

        
        Numeric keypad 3 key
        NUMPAD3

        
        Numeric keypad 4 key
        NUMPAD4

        
        Numeric keypad 5 key
        NUMPAD5

        
        Numeric keypad 6 key
        NUMPAD6

        
        Numeric keypad 7 key
        NUMPAD7

        
        Numeric keypad 8 key
        NUMPAD8

        
        Numeric keypad 9 key
        NUMPAD9

        
        Multiply key
        MULTIPLY

        
        Add key
        ADD

        
        Separator key
        SEPARATOR

        
        Subtract key
        SUBTRACT

        
        Decimal key
        DECIMAL

        
        Divide key
        DIVIDE


        F-Keys I.e. F1 for F1 key  Don't type braces.
        <F> + <1-24>

        
        NUM LOCK key
        NUMLOCK

        
        SCROLL LOCK key
        SCROLL

        
        Left SHIFT key
        LSHIFT

        
        Right SHIFT key
        RSHIFT = 0xA1,

        
        Left CONTROL key
        LCONTROL

        
        Right CONTROL key
        RCONTROL

        
        Left ALT key
        LMENU

        
        Right ALT key
        RMENU

        
        Windows 2000/XP: Browser Back key
        BROWSER_BACK

        
        Windows 2000/XP: Browser Forward key
        BROWSER_FORWARD

        
        Windows 2000/XP: Browser Refresh key
        BROWSER_REFRESH

        
        Windows 2000/XP: Browser Stop key
        BROWSER_STOP

        
        Windows 2000/XP: Browser Search key
        BROWSER_SEARCH

        
        Windows 2000/XP: Browser Favorites key
        BROWSER_FAVORITES

        
        Windows 2000/XP: Browser Start and Home key
        BROWSER_HOME

        
        Windows 2000/XP: Volume Mute key
        VOLUME_MUTE

        
        Windows 2000/XP: Volume Down key
        VOLUME_DOWN

        
        Windows 2000/XP: Volume Up key
        VOLUME_UP

        
        Windows 2000/XP: Next Track key
        MEDIA_NEXT_TRACK

        
        Windows 2000/XP: Previous Track key
        MEDIA_PREV_TRACK

        
        Windows 2000/XP: Stop Media key
        MEDIA_STOP

        
        Windows 2000/XP: Play/Pause Media key
        MEDIA_PLAY_PAUSE

        
        Windows 2000/XP: Start Mail key
        LAUNCH_MAIL

        
        Windows 2000/XP: Select Media key
        LAUNCH_MEDIA_SELECT

        
        Windows 2000/XP: Start Application 1 key
        LAUNCH_APP1

        
        Windows 2000/XP: Start Application 2 key
        LAUNCH_APP2

        
        Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the ';:' key 
        OEM_1

        
        Windows 2000/XP: For any country/region, the '+' key
        OEM_PLUS

        
        Windows 2000/XP: For any country/region, the ',' key
        OEM_COMMA

        
        Windows 2000/XP: For any country/region, the '-' key
        OEM_MINUS

        
        Windows 2000/XP: For any country/region, the '.' key
        OEM_PERIOD

        
        Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the '/?' key 
        OEM_2

        
        Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the '`~' key 
        OEM_3

        
        Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the '[{' key
        OEM_4

        
        Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the '\|' key
        OEM_5

        
        Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the ']}' key
        OEM_6

        
        Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the 'single-quote/double-quote' key
        OEM_7

        
        Used for miscellaneous characters; it can vary by keyboard.
        OEM_8

       
        Windows 2000/XP: Either the angle bracket key or the backslash key on the RT 102-key keyboard
        OEM_102

        
        Play key
        PLAY

        
        Zoom key
        ZOOM

        
        Clear key
        OEM_CLEAR
```
