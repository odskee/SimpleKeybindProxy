# Changes 23/01/2024
Substantial changes have been made to the way SKP operates. Current Roadmap:
* Keybind type file override - press type can be defined in the bind file (and supersedes the requested type) 
* Finish and Implement message chunks
* Add "Monitor Game" command(s)
* Elite Dangrous Controller - using journal and keypresses
* DCS Controller - using DCS-BIOS
* 'Keybind Processor' - GUI to set keypress for each declared bind name

<br />

# Simple Keybind Proxy
A simple and light weight Key Press & command Proxy server - allowing you to locally make keyboard (and soon direct-to-game) inputs from any remote device on your network that has a web browser.  It starts a leight-weight web server accessible over local network, providing requested landing Site(s).  A landing site is a website that represents your inputs - it can look however you like, with any style of buttons, switches and sliders etc (a virtual button box).  Paired with this is a keybind dictionary, which translates keybind names into the specific keyboard inputs for that action.  Multiple landing site and keybind dictionaries can be created and used all at the same time, allowing multiple virtualised button boxes to be created.  Landing sites may implement persistent connections and receive updates from game events and requested commands.

<br />

# Getting Started
# Setup
Downloaded the latest version:
> https://github.com/odskee/SimpleKeybindProxy/releases

Extract the zip file to somewhere suitable i.e. *C:\SimpleKeybindProxy\*.  This folder contains a "Binds" and "Landing" folders which are used to hold your keybind dictionaries and landing sites.

There are two sample Landing Sites to give a very, very basic demonstration within the Landing Folder.  A matching bind dictionary is included at Binds/Binds.txt.  The samples are sufficient to observe the program but you will likely want to change these yourself - instead of modifying the included samples, duplicate then rename one of the samples to avoid overwritting your changes on updates.

<br />

# Usage
## Configure SKP
The main folder containing the SimpleKeybindProxy.exe will also have an AppSettings.json file.  This file contains a number of settings that can change the way SKP operates - for the majority of people however the defaults will be fine.  If you would like to use a custom log, landing or bind site directory, you can specify those here.

<br />

## Landing Sites and Keybind dictionaries
The idea is when using or creating a landing site, a template keybind dictionary should be provided.  SKP will import all dictionaries found within the Binds directory, so these can be seperated into multiple files that correspond to a specific landing site.  Landing sites can be created by yourself or others, and be as simple or complex as you would like.

### Setting Your Binds
The bind dictionary is a simple txt file that specifies the name of a keybind, a comma (,) then the 'system' name for the key press - an example can be found in Apendix 2.

You can combine as many modifiers as you like by using the plus (+) symbol and can combine as many key presses with the hash (#) symbol.  For example: LMENU+LSHIFT+VK_A#VK_B which would: press and hold left alt and left shift, press and release 'A' then 'B' and finally release left shift and left alt - the last keypress after a + is not treated as modifier.

<br />

## Run SimpleKeybindProxy.exe
Right-Click SimpleKeybindProxy.exe and choose "Run as Administrator" - SKP should request this by default, running without admin rights will likely cause SKP to fail to start.  Once SKPT is running, it will begin listnening for requests.  The output at this time will show you both the network addresses it's accessible on and the landing site URL's for each landing site you have.  By default, you can access your landing sites at "http://localhost:8001/"; navigate to a landing site to begin interacting with SKP.

<br />

<br />

# Power Users
## Run From Console
Power / Advances users can run directly from the command line with additional arguments.  Make sure you run from a Powershell / Terminal window with admin rights.

The following arguments are available: 

```
-l - Define a custom Landing Site directory location.  Example: -l "C:\Folder1\LandingSites\"
-b - Define the directory that contains keybind dictionary.  Example: -b "C:\Folder1\Binds\"
-a - Define the IP address the server will listen for connections on.  Defaults to "*" or every address.  Example: -a 127.0.0.1
-p - Define the Port the server will listen for connections on.  Defaults to 8001.  Example: -p 1234
-v - Verbosity level - 1: Standard, 2: Noisy.  Defaults to 1.  Example: -v 2
-o - Log file directory.  Defaults to /Logs.  Example: -o "C:\Folder1\SKPLogs\"
--ignore - Ignore missing Landing site location(s).  I.e. run with externally hosted landing sites
--noissue - Don't actually send the requested keybind - use for testing.
```
<br />

## Interacting with Simple Keybind Proxy
Once SKP is running, you can view your landing sites at _"http://localhost:8001/Directory_Structure_of_Landing_Site"_ (or the IP and Port you specified with -a / -p); for example, _http://localhost:8001/Landing1/_.

The URL request structure needs to match your Landing Site directory structure.  For example, if you add "SitesByBob/Panel_1/" into the Landing Site folder, you can view this landing site at http://localhost:8001/SitesByBob/Pane_1/.  By default, requesting the URL with no landing site will display a default page with minimal content - this is currently being turned into a web management interface.

You can also issue commands into the running console window; currently the following commands are available:

`v <1 | 2>` - Change the verbosity level.  Default 1.  Example "v 2"`

`reload` - Reloads all keybind dictionaries.  Example "reload"

`showbinds` - Shows all bind names to keypress pairs in your dictionaries.  Example: "showbinds"

`socketsend <-a [address] | -i [Id] Text_To_Send>` - Sends data to the specified web socket.  Example: socketsend -a 127.0.0.1:1234 Text sent to landing site

`noissue <0 | 1>` - Don't issue the actual keypress / combination - useful for testing.  Default 0.  Example: noissue 1

*Verbosity level dictates how noisy the console output is and what gets stored in the log file.  Recomend only setting '2' when troubleshooting issues.*

<br />

<br />

# Making Custom Landing Sites
## Directory Structure
You can create as many landing sites as you want, providing you use an appropriate directory structure.  Create a new directory in the Landing Site location - this acts as your container.  You can either directly place resources such as HTML here or you can create further subdirectories.  For example, if you want to create multiple panels, you can create a structure that looks like "Landing\BobsPanels\Panel_1" and "Landing\BobsPanels\Panel_2" (These would be accessed by requesting /BobsPanels/Panel_1/ and /BobsPanels/Panel_2/ in the URL.

<br />

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

<br />

## Making keybind requests
You can either make a keybind request manually by 'building' the URL yourself, or by starting a web socket connection and submitting a valid json string.

### Using HTTP GET
This is the simplest method of making a request, but it does not provide any feedback on if the request was successful or not.  A request is made by including the required parameters per command requested in the URL - see the JSON descriptions under the Web Sockets section for a full list of parameters.  Unlike web sockets, you do not need to provide an Id as a parameter.

For example, The URL structure for a Keybind request command would look like http://server_address:server_port/?Command=Keybind&BindName=MyBindName - in this example, this will show an output in the SKP console confirming a button press request.

**Example**
Locate and open you binds.txt file (or create a new blank .txt file in the same folder).  Add the following entry *"ScreenSample, SCREENSHOT"* then save and close the file.  Start SKP and browse to the following address *"http://localhost:8001/?Command=Keybind&BindName=ScreenSample"*.  This will show both the request in the SKP console window while also pressing the print screen keyboard key - you can verify this by pasting into MSPaint or similar.  Furthermore, verify this behaviour by navigating to the URL from another device on your network, replacing localhost with the local IP of your computer.

<br />

### Using Web Sockets
_Test using any tool of choice (Weasel for Firefox for example)._

Using Web Sockets is a far better method as it allows request feedback and multi-landing site communication.  SKP works by sending and receiving JSON text over web socket.  Each request you make needs to be structured according to the specific command model. Each command will produce a response, containing the requested command and the outcome of the request (success or fail) - note that there is a `CommandSuccess` flag to indicate a successful receipt and translation of the command in addition to a `Success` flag indicating the result of the command request itself.  A full list of JSON descriptors are available in Apendix 3.

Important note: The max size of a message is 512; meaning any messages above this size will be split into multiple parts.

Once you initiate a WS connection, you will receive a `SocketConnectedResponse` object that contains an Id and 'hello' message.  
```
{
   "Id": String,
   "Message": String
}
```

Each further request you make will also generate a `ServerCommandResponse`; this holds a `CommandResponse` object which provided additional information about the specfic command:
```
{
  "id": String,
  "command": Object,
  "commandSuccess": Bool,
  "message": String,
  "commandResponse": Object
}
```
<br />

Each subsequent request you make must contain the ID you are provided.  To submit a command, you need to provide a josn string representing the desired Command object; this includes you command and any relevant command data.  The following commands are currentlya available:

**Command: Keybind**

Requests the keypresses of the matching provided Keybind Name.  BindName is the name of the keypress(s) as listed in one of the bind dictionaries.  PressType is not yet implemented.

Request:
```
{
   "Id": String,
   "Command": String,
   "BindName": String,
   "PressType": String
}	
```

Response:
```
{
  "keybindName": String,
  "keypressCombination": String[],
  "modifierCombination": String[],
  "success": Bool,
  "responseMessage": String
}
```

<br />

<br />

**Command: RegisterWebSocket**

Allows you to register a known name for you Landing site.  Registered connections / landing sites can communicate with each other.
```
{
   "Id": String,
   "Command": String,
   "RegisteredName": String
}	
```

Response:
```
{
  "oldName": String,
  "newName": String,
  "success": Bool,
  "message": String
}

```

<br />

<br />

**Command: SendToSocket**

Sends the provided data to the requested registered connection / landing site. 
```
{
   "Id": String,
   "Command": String,
   "DestinationName": String,
   "Message": Object
}	
```
Response:
```
{
  "toId": String,
  "destinationName": String,
  "success": Bool,
  "message": String
}


```

<br />

#### Example - make a keybind request using web sockets from start to finish
*Assumes a bind name of 'TestBind'*

Open a new web socket connection and store the recived Id property (**4a01ca13-2d1f-46e2-b6ff-9e5f79afa943**) within your response / 'hello message':
```
{
   "Id": "4a01ca13-2d1f-46e2-b6ff-9e5f79afa943",
   "Message": "Server Says Hello"
}	
```

<br />

Next, submit a keybind command request to the server:
```
{
   "Id": "4a01ca13-2d1f-46e2-b6ff-9e5f79afa943",
   "Command": "Keybind",
   "BindName": "TestBind",
   "PressType": String
}
```

<br />

Observer from your response whether your request was successful or not:
```
{
   "Id": "4a01ca13-2d1f-46e2-b6ff-9e5f79afa943",
   "Command": {
      "BindName": "TestBind",
      "PressType": null,
      "Id": "4a01ca13-2d1f-46e2-b6ff-9e5f79afa943",
      "RequesterName": "",
      "Command": "keybind"
   },
   "CommandSuccess": true,
   "Message": "Command was successfully executed and processed",
   "CommandResponse": {
      "KeybindName": "TestBind",
      "KeypressCombination": [
         "CAPITAL"
      ],
      "ModifierCombination": [],
      "Success": true,
      "ResponseMessage": null
   }
}	
```

<br />

<br />

#### Landing Site Communication
More complex landing sites may have a need to interact / synchronise state with each other.  This is done by first registering your landing site with a unique name - this is so other landing sites can address you (as Id is random per connection).  Once two or more landing sites have been registered with a name, the SendToSocket command can be used to provide text / Json to one of the other landing sites based on the name it was registered with.

For example, given the Id: "a06334d7-45d7-4d2b-bf30-4067d444599d", you would first register each landing site respectively:
```
{"Id": "a06334d7-45d7-4d2b-bf30-4067d444599d", "Command": "RegisterWebSocket", "RegisteredName": "LandingSite1"}
{"Id": "2efe669c-b66e-4f10-b1fb-f0fd7698c9e5", "Command": "RegisterWebSocket", "RegisteredName": "AnotherLandingSite"}
```
Once done, you can then communicate from 'LandingSite1' to 'AnotherLandingSite' with:
```
{"Id": "a06334d7-45d7-4d2b-bf30-4067d444599d","Command": "SendToSocket","DestinationName": "AnotherLandingSite","Message": {"Text" : "Hello World"}}
```

<br />

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
    </head>
    <body>
        <main>
            <div OnCLick="IssueBind('MyCustomKeybind')" style="PressButtonSmall">Press this Button</div>
        </main>
        <script type="text/javascript">
        	const socket = new WebSocket("ws://127.0.0.1:8001");
        	const sendMsg = {
        		Command: "Keybind_Press",
        		CommandData: ["FlapsInc"]
        	};
        
        	socket.addEventListener("message", (event) => {
        		console.log("Message from server ", event.data);
        	});
        	socket.addEventListener("error", (event) => {
        		console.log("WebSocket error: ", event);
        	});
        	socket.addEventListener("close", (event) => {
        		console.log("The connection has been closed successfully.");
        	})
        	function IssueBind(bindName) {
        		$.get("./", { Command: "KeyBind_Press", CommandData: bindName });
        	}
        
        	function TestSocket() {
        		console.log("Sending Data");
        		console.log(socket.readyState);
        
        		socket.send(JSON.stringify(sendMsg));
        	}
        </script>
    </body>
</html>
```

<br />

# Special Notes
## Favicon.ico Requests
Currently, any favicon.ico requests are ignored by the server and will not be shown even if provided.

## Security - Use at your own risk!
SKP started originally from a PoC idea and has grown to meet the requirements in the most simple way possible.  As such, it is intended to be operated in an environment that you control with landing sites that you trust / have made yourself.  DO NOT expose SKP to public networks / port forward for off-site access.  The author of SKP accepts zero responsibility for any harm, damage or 'bad times' that result in using SKP in any way.

## Max Web Socket Requests
By default, SKP has a limit of 15 active web socket connections.  This means a total of 15 landing sites with active socket connections can be use simultaneously, although there is no limit on the number of non-socket HTTP requests that can be made.  To change this value, locate the AppSettings.json file and set accordingly.

<br />
<br />


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

<br />

# Appendix 2 - Sample Keybind dictionary
```
FlapsInc,LCTRL+LMENU+LSHIFT+F
FlapsDec,LALT_F
GearDn,G
GearUp,LSHIFT+G
Test,LCONTROL+LMENU+VK_A
OpenMap,VK_M
```

<br />

# Appending 3 - Request / Response Objects
## CommandRequest
```
{
  "command": string,
  "commandData": string[]
}
```

## SocketConnectedResponse
```
{
  "id": string,
  "message": string
}
```

## ServerCommandResponse
```
{
  "command": CommandRequest,
  "commandSuccess": bool,
  "message": string,
  "bindCommandResponse": KeyBindResponse
}
```

## KeybindResponse
```
{
  "keybindName": string,
  "keypressCombination": string[],
  "modifierCombination": string[],
  "success": bool,
  "responseMessage": string
}
```
