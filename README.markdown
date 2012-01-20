What is Droppy?
---------------
Droppy is a very low-overhead Windows desktop utility written for people who often have to open or copy/move files into the same set of folders. Droppy presents a simple drag-and-drop UI which allows a user to select favorite folders, copy files into them or easily open them.

Although it probably has uses by other professions, main motivation for writing this tool came from software development where the same set of directories (binary output, logs, configuration, data files...) is accessed extremely often.

Droppy can be docked to any edge of the desktop.  Docking makes Droppy a top-most window and auto-collapsable, so when its not in use it will shrink to strip few pixels wide and it will remain there waiting for mouse (with or without dragged file) to move over its area. 


Build Environment for Droppy
----------------------------
Droppy was written in C# against .NET Framework v3.5 (it would've been v4.0 if Microsoft didn't introduce some [top-window size animation bugs][1] which they are refusing to fix). To work on the project itself, only requires Microsoft Visual Studio 2010 w/ Service Pack 1.

However, the project also contains a one-command build and deployment script which can be used to produce a complete installation (as well as a zip package for people who are paranoid about using installers).

To get the scripted build working you will need the following:

1. [NullSoft Scriptable Install System][2] Installer
2. [MSBuild Community Tasks][3]

Optional:

1. [XAML Regions Add-On for Visual Studio][4] -- Droppy XAML files use region-style comments to partition themselves into logical parts. This add-on is needed if you want the IDE to interpret those comments as collapsable regions.
2. [HM NIS Edit][5] -- A free NSIS script editor.


Building and Deploying Droppy
-----------------------------
Even if you are planning to do all work from VS IDE, it is recommented to run msbuild script at least once as it will create some project tree directories and perform few other useful things.

To build droppy:

1. Open Visual Studio 2010 command prompt
2. *cd* into *Droppy/Build/* directory
3. Run the build script
    * Build in debug mode: *msbuild BuildDroppy.proj /p:Configuration=Debug*
	* Build in release mode: *msbuild BuildDroppy.proj /p:Configuration=Release* or simply, *msbuild BuildDroppy.proj*
	* Rebuild and make installation: *msbuild BuildDroppy.proj /t:Deploy*
	
For complete usage of BuildDroppy.proj script, open that script in your favorite text editor and you will find full command line documentation in the file header.



[1]: http://connect.microsoft.com/VisualStudio/feedback/details/715415/window-width-height-animation-in-wpf-got-broken-on-net-framework-4-0
[2]: http://nsis.sourceforge.net/Download
[3]: http://msbuildtasks.tigris.org/
[4]: http://visualstudiogallery.msdn.microsoft.com/3c534623-bb05-417f-afc0-c9e26bf0e177
[5]: http://hmne.sourceforge.net/