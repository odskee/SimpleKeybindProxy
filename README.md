# SimpleKeybindProxy
A simple C# / .NET Console Keybind proxy

# Disclaimer
This started as a PoC project to teach some "cool" aspects about .NET to a young programmer - there are a number of optimisations and changes that can (and will) be made.  The project idea arrived from a desire to have a simple mechanism to remotely issue key presses to a Windows 10 PC that was: Free, easy to use, lightweight, didn't require registration or PPI.  Not finding much that was suitable, a goal was born.  Some code was taken from Benjamin N. Summerton / HttpsServer.cs.


# Description
A simple and very light weight Key Press Proxy server written in C# / .NET 8.  Starts a simple 'web server' accessible over local network and provides matching Landing Sites.  Landing Sites are web pages that show your desired input(s) / keybinds and are fully customisable.  For example, a Landing Site to represent a certain panel / area within your game can be created with appripriate button / switch graphics.  With Keybinds that are defined in a Keybind name to Key Press dictionary, an interaction on a Landing Site can request a keybind by name having the corresponding key press made on the computer running SinpleKeybindProxy.


# What Does it Do?
Allows you to create web pages that represent button/switch boxes that can then be accessed over the local network with a touchscreen device, allowing that device to input keypresses on the target computer.


# How Does it Work?
Running SimpleKeyBindProxy starts a simple web server - each time a request is made it is inspected for a Reserved Word (a command i.e. KeyBind_).  When a reserved word is not found, the requested Landing Site is shown based on the request URL.  When a command keyword is detected, that command is processed - for Keybinds, the name of the requested keybind is looked up in a dictionary for the desired key press(s).

Landing Sites are shown based on the request URl and must have a matching directory stucture to the request.  Any request that contains a Reserved Word will work regardless of specified landing sites; if a command is given with no landing site in the URL, a generic error HTML page is shown.


# Setup
Once you have downloaded the release version, you will have a SimpleKeyBindProxy folder - place this folder in a location you're happy with, this is where you will run it from.  By default, the SimpleKeyBindProxy folder contains both a "Binds" directory and a "Landing" directory - these are both required, although you can define an alternative location when starting the program.  There are two sample Landing Sites to give a very, very basic demonstration of their purpose.

You will also need to configure your binds.txt dictionary - by default this is in "SimpleKeybindProxy/Binds/binds.txt".  There are some pre-defined to act as a demonstration; they are not needed and can be removed / renamed.  The purpose of this file is to define a 'keybind name' and it's matching Key Press.  While the name can be anything you like, the associated Key Press must match exactly - a list of available key presses can be found in the "SimpleKeybindProxy/KeyPressNames.txt" file.  Note: Some expirimentation may be required for symbols / certain keys due to language / regional differences.

To run the program, run SimpleKeybindProxy.exe AS AN ADMINISTRATOR - if you do not give the program admin permissions the server likely won't start.  By default, you can access your landing sites at "http://localhost:8001" (See Usage below on how to change this).

# Usage
When running from the command line, you can provide the following arguments: 
> -l - Define a custom Landing Site directory location.  Example: -l "C:\Folder1\LandingSites\"

> -b - Define the directory and / or keybind dictionary file.  Example: -b "C:\Folder1\Binds\" OR -b "C:\Folder1\Binds\bindfile.txt" OR -b "bindfile.txt"

> -a - Define the IP address the server will listen for connections on.  Defaults to "*" or every address.  Example: -a 127.0.0.1

> -p - Define the Port the server will listen for connections on.  Defaults to 8001.  Example: -p 1234 

Once you have your binds defined and have created your landing site (or are using the samples), you can view them at http://localhost:8001 (or the IP and Port you specified with -a / -p).  The URL request structure needs to match your Landing Site directory stucture.  For example, if you add "SitesByBob/Panel_1/" into the Landing Site folder, you can view this landing site at http://localhost:8001/SitesByBob/Pane_1/.

When an interaction on a landing site requests a key press, the keybind name and Key Press will be displaed in the console output.  This can be tested using a "test" keybind name in the request - it does not need to be defined in a bind dictionary.

You can make a manual key press by requesting the KeyBind URL directly.  To do this, navigate to http://<server_address>:<server_port>/KeyBind_<name_of_keybind> - you don't need to include a Landing page in your request but if you don't, the server will show you the default error HTML - your keybind has sill been processed as shown in the console window.

# Making Custom Landing Sites
You can create as many landing sites as you want, providing you use an appropriate directory strucure.  Create a new directory in the Landing Site location - this acts as your container.  You can either directly place resources such as HTML here or you can create further subdirectories.  For example, if you want to create multiple panels, you can create a structure that looks like "Landing\BobsPanels\Panel_1" and "Landing\BobsPanels\Panel_2" (These would be accessed by requesting /BobsPanels/Panel_1/ in the URL.

To create the landing site, add a new .html file - it MUST have the same name as the directory it resides in i.e. "Landing\BobsPanels\Panel_1\Panel_1.html".  If another name is used, the panel must be requested directly in the URL i.e. "/BobsPanels/Panel_1/index.html".

You can issue a keybind request by making a POST request that terminates with "KeyBind_<name_of_Keybind>".  How you do this is up to you - one technique using JS is shown in the provided "Landing1" sample; by using the OnClick event and some JS, a POST request for a certain keybind name is sent when clicking on defined elements.
