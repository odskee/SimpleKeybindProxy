# SimpleKeybindProxy
A simple C# / .NET Console Keybind proxy


# Description
A simple and very light weight Key Press Proxy server written in C# / .NET 8.  Starts a simple 'web server' accessible over local network and provides matching Landing Sites.  Landing Sites are web pages that show your desired input(s) / keybinds and are fully customisable.  For example, a Landing Site to represent a certain panel / area within your game can be created with appripriate button / switch graphics.  With Keybinds that are defined in a Keybind name to Key Press dictionary, an interaction on a Landing Site can request a keybind by name having the corresponding key press made on the computer running SinpleKeybindProxy.


# What Does it Do?
Allows you to create web pages that represent button/switch boxes that can then be accessed over the local network with a touchscreen device, allowing that device to input keypresses on the target computer.


# How Does it Work?
Running SimpleKeyBindProxy starts a simple web server - each time a request is made it is inspected for a Reserved Word (a command i.e. KeyBind_).  When a reserved word is not found, the requested Landing Site is shown based on the request URL.  When a command keyword is detected, that command is processed - for Keybinds, the name of the requested keybind is looked up in a dictionary for the desired key press(s).


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

Once you have your binds defined and have created your landing site (or are using the samples), you can view them at http://localhost:8001 (or the IP and Port you specified with -a / -p).
