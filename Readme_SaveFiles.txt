

Save games and config files are in these folders, depending on your operating system.

WINDOWS:	C:/Users/[username]/AppData/LocalLow/Ludeon Studios/RimWorld
(On Windows, the AppData folder may be hidden.)

MAC: 		Users/[username]/library/cache/Ludeon Studios/RimWorld

LINUX: 		/home/[username]/.config/unity3d/Ludeon Studios/RimWorld


Deleting config files will reset them. This can be useful if the game is borked and won't start.

For debugging and troubleshooting, the output_log.txt file is in the _Data folder in the game install folder.

Why is it like this? Modern operating systems separate changing data from program installations for several reasons:
	-First, it allows different users to have different save and config data.
	-Second, it enhances security, because it allows the program to run without having permission to write to disk anywhere outside its own little save folder.