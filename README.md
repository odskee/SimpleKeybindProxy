# SimpleKeybindProxy
A simple C# / .NET Console Keybind proxy


# Description
A simple and very light weight Key Press Proxy server written in C# / .NET 8.  Starts a simple 'web server' accessible over local network and provides matching Landing Sites.  Landing Sites are web pages that show your desired input(s) / keybinds and are fully customisable.  For example, a Landing Site to represent a certain panel / area within your game can be created with appripriate button / switch graphics.  With Keybinds that are defined in a Keybind name to Key Press dictionary, an interaction on a Landing Site can request a keybind by name having the corresponding key press made on the computer running SinpleKeybindProxy.


# What Does it Do?
Allows you to create web pages that represent button/switch boxes that can then be accessed over the local network with a touchscreen device, allowing that device to input keypresses on the target computer.


# How Does it Work?
It starts a simple web server - each time a request is made it is inspected for a Reserved Word (a command i.e. KeyBind_).  When a reserved word is not found, the requested Landing Site is shown based on the request URL.  When a command keyword is detected, that command is processed - for Keybinds, the name of the requested keybind is looked up in a dictionary for the desired key press(s).
