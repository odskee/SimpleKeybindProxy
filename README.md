# SimpleKeybindProxy
A simple C# / .NET Console Keybind proxy

# Changes 11/01/2024
Substantial changes have been made to the way SKP operates.

#

# Description / What does it do?
A simple and very light weight Key Press Proxy server - allowing you to run keyboard inputs from any remote device on your network that has a web browser.  It starts a leight-weight web server accessible over local network, providing the requested landing Site(s).  A landing site is a website that represents your inputs - it can look however you like, with any style of buttons, switches and sliders etc.  Paired with this is a keybind dictionary, which translates keybind names into the specific keyboard inputs for that action.  Multiple landing site and keybind dictionaries can be created and used all at the same time, allowing multiple virtualised button boxes to be created.


# How Does it Work?
Running SimpleKeyBindProxy.exe starts a local web server with a specified landing site directory as it's base.  When a request is received, the URL is converted in a corresponding directory structure i.e. *localhost:8001/Landing1/Panel_1/* would translate to *.\Landing\Landing1\Panel_1*.  SSK now uses the GET method to detect and process keybind requests, with multiple request types now available.  If the URL contains the parameters "Command" and "CommandData", the values of these are inspected and processed.  The "Command" parameter dictates what action is being requested, while the "CommandData" parameter supplies any data required to complete the requested action; for keybinds, the command would specify the type of keypress while commandData would specify the name of the keybind requested.

Keybind command requests will specify a name of the keybind they are requesting, not the key combination itself - allowing landing sites to be kept generic.  A seperate keybind dictionary is used, which is a comma seperated list of a Keybind name to a matching keyboard input / input combination, SKP matches the keybind name i.e. "ButtonCombination_1" to the actual keyboard input that's needed i.e. "LSHIFT+VK_A" (a list of the keyboard input names is provided in Apendix 1).

## Landing Sites and Keybind dictionaries
The idea is when creating a landing site, a template keybind dictionary should be provided.  SKP will import all dictionaries found within the Binds directory, so these can be seperated into multiple files that correspond to a specific landing site.  Landing sites can be created by yourself or others, and be as simple or complex as you would like. 


# Setup
Downloaded the latest version:
> https://github.com/odskee/SimpleKeybindProxy/releases

Extract the zip file to somewhere suitable i.e. *C:\SimpleKeybindProxy\*.  This folder contains a "Binds" and "Landing" which are used to hold your keybind dictionaries and landing sites.

There are two sample Landing Sites to give a very, very basic demonstration within the Landing Folder.  A matching bind dictionary is included at Binds/Binds.txt.  The samples are sufficient to observe the program but you will likely want to change these yourself - instead of modifying the included samples, duplicate then rename one of the samples to avoid overwritting your changes on updates.

To run the program, run SimpleKeybindProxy.exe AS AN ADMINISTRATOR - the server likely will not start without admin rights.  The console output will show you both what network addresses it can be accessed on and the landing site URL's it has detected.  By default, you can access your landing sites at "http://localhost:8001/" (See Usage below on how to change this).


# Usage
## Starting SKP
### Run SimpleKeybindProxy.exe
Providing you are using the default "Landing" and "Binds" folders, Right-Click SimpleKeybindProxy.exe and choose "Run as Administrator".  SKP will then start and once running, begin listnening for requests.  The output at this time will show you both the network addresses it's accessible on and the landing site URL's for each landing site you have.

### Run From Console
Power / Advances users can run directly from the command line with additional arguments.  Note, some (and soon all) of these can be configured within the running console.  Make sure you run from a Powershell / Terminal window with admin rights.

The following arguments are available: 

```
-l - Define a custom Landing Site directory location.  Example: -l "C:\Folder1\LandingSites\"
-b - Define the directory that contains keybind dictionary.  Example: -b "C:\Folder1\Binds\"
-a - Define the IP address the server will listen for connections on.  Defaults to "*" or every address.  Example: -a 127.0.0.1
-p - Define the Port the server will listen for connections on.  Defaults to 8001.  Example: -p 1234
-v - Verbosity level - 1: Standard, 2: Noisy.  Defaults to 1.  Example: -v 2
-o - Log file directory.  Defaults to /Logs.  Example: -o "C:\Folder1\SKPLogs\"
--ignore - Ignore missing Landing site location(s).  I.e. run with externally hosted landing sites
--noissue Don't actually send the requested keybind - use for testing.
```

## Interacting with Simple Keybind Proxy
Once SKP is running, you can view your landing sites at _"http://localhost:8001/Directory_Structure_of_Landing_Site"_ (or the IP and Port you specified with -a / -p); for example, _http://localhost:8001/Landing1/_.

The URL request structure needs to match your Landing Site directory structure.  For example, if you add "SitesByBob/Panel_1/" into the Landing Site folder, you can view this landing site at http://localhost:8001/SitesByBob/Pane_1/.  By default, requesting the URL with no landing site will display a default page with minimal content - this is currently being turned into a web management interface.

You can also issue commands into the running console window; currently the following commands are available:
```
v <number> - Change the verbosity level.  Example "v 2"
reload - Reloads all keybind dictionaries.  Example "reload"
showbinds - Shows all bind names to keypress pairs in your dictionaries.  Example: "showbinds"
```

Verbosity level sets how noisy the output is - by default this is 1.  With this value, you will see the keybind name and matching keypress shown in the console window, along with the source of the request.  With Verbosity level 2, all requests are shown in addition to a trace of the keybind execution.


## Setting Your Binds
The bind dictionary is a simple txt file that specifies the name of a keybind, a comma (,) then the 'system' name for the key press - an example can be found in Apendix 2.

You can combine as many modifiers as you like by using the plus (+) symbol and can combine as many key presses with the hash (#) symbol.  For example: LMENU+LSHIFT+VK_A#VK_B which would: press and hold left alt and left shift, press and release 'A' then 'B' and finally release left shift and left alt - the last keypress after a + is not treated as modifier.



## Making keybind requests
You can either make a keybind request manually by 'building' the URL yourself, use one of the Landing site examples or create your own.  See the section below on creating your own landing site.

A request is made by including two parameters in the URL; Command and CommandData i.e. */?Command=KeyPress&CommandData=MyKeybindName*.  You can request one of three keypress types as the Command:
+ KeyPress - this is a keydown and keyup event i.e. a KeyPress for the CTRL button would press and then release control.  For key conbinations, the modifiers are pressed and held, while the keys are pressed and released.
+ KeyDown - Request press only; the requested button / combination will be pressed and not released.  For combinations, this occurs in the order they are specified.
+ KeyUp - Request release only; the requested button / combination will be released, not pressed.  For combinations, this occurs in the order they are specified.

The CommandData parameter should comtain the name of a matching keybind in one of the keybind dictionaries.

As shown above, you can make a manual key press by requesting the KeyBind URL directly.  To do this, navigate to http://server_address:server_port/?Command=KeyPress&CommandData=test - in this example, this will show an output in the SKP console showing a button press was requested, you can run this from a network device to verify SKP is working properly.

### Manual Example
Locate and open you binds.txt file (or create a new blank .txt file in the same folder).  Add the following entry *"ScreenSample, SCREENSHOT"* then save and close the file.  Start SKP and browse to the following address *"http://localhost:8001/?Command=KeyPress&CommandData=ScreenSample"*.  This will show both the request in the SKP console window while also pressing the print screen keyboard key - you can verify this by pasting into MSPaint or similar.  Furthermore, verify this behaviour by navigating to the URL from another device on your network, replacing localhost with the local IP of your computer.


# 

# Making Custom Landing Sites
## Directory Structure
You can create as many landing sites as you want, providing you use an appropriate directory structure.  Create a new directory in the Landing Site location - this acts as your container.  You can either directly place resources such as HTML here or you can create further subdirectories.  For example, if you want to create multiple panels, you can create a structure that looks like "Landing\BobsPanels\Panel_1" and "Landing\BobsPanels\Panel_2" (These would be accessed by requesting /BobsPanels/Panel_1/ and /BobsPanels/Panel_2/ in the URL.

## Create the Landing Site
To create the landing site, add a new .html file - ideally, it should have the same name as the directory it resides in i.e. "Landing\BobsPanels\Panel_1\Panel_1.html".  If another name is used i.e. index.html, the html page must be requested directly in the URL i.e. "/BobsPanels/Panel_1/index.html".

### Including Resources
Currently, the following resource types are supported:
* HTML
* JS
* CSS
* PNG
* SVG
* JPEG/JPG
* GIF

When you want to include a resource such as CSS stylesheet or images, you must use relative linking.  For example, if your CSS file is in the same directory as your html file, you would use href=".\style.css".  This also applies to any form actions - unless you require any advanced behaviour, you should use ./ as your action value (Alternatively, you can specify a different landing site if you want a different site to be shown upon requesting a keypress).  

## Making Keybind Requests
You can issue a keybind request by making a GET request that contains the parameters "Command" and CommandData".  How you do this is up to you - one technique using JS is shown in the provided "Landing1" sample; by using the OnClick event and some JQuery, a GET request for a certain keybind name is sent when clicking on <div> elements.

### Naming your Keybinds
The name of the keybind is chosen by you and can be anything you want - each one you use needs to be added to a dictionary for you / the user the match a keyboard input to.  When naming your binds, it is advisable to be as unique as possible while making them intuative.  For example, to create an input the issues a request to raise landing gear in MS Flight Sim, choosing a name formatted like "msfs_myPlane1_gearUp" minimises the chance of conflicting with the same or similar event in another game or application.

It is also suggested to provide a template keybind dictionary that lists all keybind names you've used in your landing site - this makes it much easier to both keep binds seperate and tie them to actual keyboard inputs.


### JS Sample
The following shows a very simple example of how to use / make a keybind request:

JS / HTML:
```
<!DOCTYPE html>
<html>
    <head>
    	<title>Simple Keybind Proxy</title>
    	<link rel="stylesheet" href=".\style.css" type="text/css">
    	<script src="https://ajax.googleapis.com/ajax/libs/jquery/3.7.1/jquery.min.js"></script>
    	<script>
    		function IssueBind(bindName) {
    			$.get("./", { Command: "KeyBind_Press", CommandData: bindName });
    		}
    	</script>
    </head>
    <body>
        <main>
            <div OnCLick="IssueBind('MyCustomKeybind')" style="PressButtonSmall">Press this Button</div>
        </main>
    </body>
</html>
```


## Special Notes
Currently, any favicon.ico requests are ignored by the server and will not be shown even if provided.

#
#


# Appendix 1 - Key Press Names
The following is a list of accepted KeyPress names - these are the values you add to the binds.txt file against a certain keybind name.  These are taken from InputSimulator which is used to simulate the key press ([http://inputsimulator.codeplex.com/](https://www.nuget.org/packages/InputSimulator/1.0.4)).
```
+ Left mouse button
LBUTTON
       
+ Right mouse button
RBUTTON

        
+ Control-break processing
        CANCEL
        
+ Middle mouse button (three-button mouse) - NOT contiguous with LBUTTON and RBUTTON
        MBUTTON
                
+ BACKSPACE key
        BACK
        
+ TAB key
        TAB
        
+ CLEAR key
        CLEAR

+ ENTER key
        RETURN 

+ SHIFT key
        SHIFT

+ CTRL key
        CONTROL

+ ALT key
        MENU

+ PAUSE key
        PAUSE

+ CAPS LOCK key
        CAPITAL

+ ESC key
        ESCAPE

+ SPACEBAR
        SPACE

+ PAGE UP key
        PRIOR

+ PAGE DOWN key
        NEXT

+ END key
        END

+ HOME key
        HOME

+ LEFT ARROW key
        LEFT

+ UP ARROW key
        UP

+ RIGHT ARROW key
        RIGHT

+ DOWN ARROW key
        DOWN

+ SELECT key
        SELECT

+ PRINT key
        PRINT

+ EXECUTE key
        EXECUTE

+ PRINT SCREEN key
        SNAPSHOT

+ INS key
        INSERT

+ DEL key
        DELETE

+ HELP key
        HELP

+ A-Z, 0-9 (Don't Enter Braces) i.e. VK_A for A key.
        VK_<Key>

+ Left Windows key (Microsoft Natural keyboard)
        LWIN

+ Right Windows key (Natural keyboard)
        RWIN

+ Computer Sleep key
        SLEEP

+ Numeric keypad 0-9 key
NUMPAD<0-1>
i.e NUMPAD4
    
+ Multiply key
        MULTIPLY

+ Add key
        ADD

+ Separator key
        SEPARATOR

+ Subtract key
        SUBTRACT

+ Decimal key
        DECIMAL

+ Divide key
        DIVIDE

+ F-Keys I.e. F1 for F1 key  Don't type braces.
        <F> + <1-24>

+ NUM LOCK key
        NUMLOCK

+ SCROLL LOCK key
        SCROLL

+ Left SHIFT key
        LSHIFT
      
+ Right SHIFT key
        RSHIFT = 0xA1,

+ Left CONTROL key
        LCONTROL

+ Right CONTROL key
        RCONTROL

+ Left ALT key
        LMENU

+ Right ALT key
        RMENU

+ Windows 2000/XP: Browser Back key
        BROWSER_BACK

+ Windows 2000/XP: Browser Forward key
        BROWSER_FORWARD

+ Windows 2000/XP: Browser Refresh key
        BROWSER_REFRESH

+ Windows 2000/XP: Browser Stop key
        BROWSER_STOP

+ Windows 2000/XP: Browser Search key
        BROWSER_SEARCH

+ Windows 2000/XP: Browser Favorites key
        BROWSER_FAVORITES

+ Windows 2000/XP: Browser Start and Home key
        BROWSER_HOME

+ Windows 2000/XP: Volume Mute key
        VOLUME_MUTE

+ Windows 2000/XP: Volume Down key
        VOLUME_DOWN

+ Windows 2000/XP: Volume Up key
        VOLUME_UP

+ Windows 2000/XP: Next Track key
        MEDIA_NEXT_TRACK

+ Windows 2000/XP: Previous Track key
        MEDIA_PREV_TRACK

+ Windows 2000/XP: Stop Media key
        MEDIA_STOP

+ Windows 2000/XP: Play/Pause Media key
        MEDIA_PLAY_PAUSE

+ Windows 2000/XP: Start Mail key
        LAUNCH_MAIL

+ Windows 2000/XP: Select Media key
        LAUNCH_MEDIA_SELECT

+ Windows 2000/XP: Start Application 1 key
        LAUNCH_APP1

+ Windows 2000/XP: Start Application 2 key
        LAUNCH_APP2

+ Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the ';:' key 
        OEM_1

+ Windows 2000/XP: For any country/region, the '+' key
        OEM_PLUS

+ Windows 2000/XP: For any country/region, the ',' key
        OEM_COMMA

+ Windows 2000/XP: For any country/region, the '-' key
        OEM_MINUS

+ Windows 2000/XP: For any country/region, the '.' key
        OEM_PERIOD

+ Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the '/?' key 
        OEM_2

+ Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the '`~' key 
        OEM_3

+ Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the '[{' key
        OEM_4

+ Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the '\|' key
        OEM_5

+ Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the ']}' key
        OEM_6

+ Used for miscellaneous characters; it can vary by keyboard. Windows 2000/XP: For the US standard keyboard, the 'single-quote/double-quote' key
        OEM_7

+ Used for miscellaneous characters; it can vary by keyboard.
        OEM_8

+ Windows 2000/XP: Either the angle bracket key or the backslash key on the RT 102-key keyboard
        OEM_102

+ Play key
        PLAY

+ Zoom key
        ZOOM

+ Clear key
        OEM_CLEAR
```

# Appendix 2 - Sample Keybind dictionary
```
FlapsInc,LCTRL+LMENU+LSHIFT+F
FlapsDec,LALT_F
GearDn,G
GearUp,LSHIFT+G
Test,LCONTROL+LMENU+VK_A
OpenMap,VK_M
```
