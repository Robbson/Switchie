# Switchie Changelog


## v1.4.0

Bug fixes:
- Get it run on recent Windows 11 version again

Changes:
- Instance creation has now several fallbacks so it should work on several Win 10 and Win 11 versions 


## v1.3.3

Changes:
- no longer allow mouse clicks to bring windows from another desktop to foreground
- Added tooltips / infos in the settings dialog for some of the settings which need an explanation


## v1.3.2

New features:
- Icons render mode supports a second row when there isn't enough space in one row
- Add parameters for desktop padding and icon padding in settings dialog

Changes:
- fixing performance properties
- layout improvements in settings dialog
- app versioning increase now less aggressive


## v1.3.1

Changes:
- Icons render mode now also supports selection as well as drag & drop to other desktops
- the context menu now also has some nice icons based on glyhps


## v1.3.0

New features:
- new settings dialog

Changes:
- nicer about dialog

Others:
- removed not used properties and functions


## v1.2.0

Bug fixes:
- Update application size when number of desktops are changed

New features:
- New alternative render mode: Show a list of application icons instead windows
- Save render mode current window position (on request) to registry and restore them from there on startup
- Windows can be brought to front when clicking on them

Others:
- Some refactoring and cleanup
