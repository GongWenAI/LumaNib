LumaNib 1.59 — Windows x64 preview

© 2026 宫文. All rights reserved.
Project: https://github.com/GongWenAI/LumaNib

Extract the entire ZIP, then open LumaNib.exe. Keep all files together; do not run it from inside the ZIP or copy just the EXE.
Targets Windows 10 version 1809 or later and Windows 11, on 64-bit Intel/AMD PCs. The .NET runtime is included. The app runs as a standard user and does not add itself to startup unless you enable Launch at login in General.

The interface follows the system display language: Chinese for Chinese languages, English otherwise. Restart LumaNib after changing the system language. Your colors and shortcuts are preserved.

Default shortcuts:
Ctrl + Alt + R: toggle mouse ring
Ctrl + Alt + D: start/stop drawing
Ctrl + Alt + Z: undo last stroke
Ctrl + Alt + X: clear ink
Esc: stop drawing or cancel shortcut capture

In Shortcuts, click a field, press a key combination, then click Apply Shortcuts. Include Ctrl, Alt or Win. Esc and F12 are reserved.
Choose Press to toggle or Hold to draw in Glow Pen. Drag with the left mouse button to draw. Ink remains click-through after drawing stops, until cleared or automatically faded.
Each ring half keeps its own selected color and glows when its corresponding mouse button is held.
Close settings to keep running in the system tray. Double-click the tray icon to reopen settings; right-click for actions or Quit. Quitting clears all screen ink.

In OBS, use Display Capture. Window Capture or Game Capture may miss the effects. Test a short recording first. Exclusive fullscreen, elevated apps and the UAC secure desktop may restrict overlays or input monitoring.

This build passed cross-compilation and 79 logic/mock-hotkey/startup checks. Actual Windows operation, visuals, OBS recording and long sessions have not been verified; Windows 11 device testing is pending. The download is not commercially code-signed.

For a rendering self-test, quit the running app, then open Run-SelfTest.cmd. Results go to %LOCALAPPDATA%\LumaNib\SelfTest. This does not replace drawing, shortcut or recording tests.
Settings: %LOCALAPPDATA%\LumaNib\settings.json
Error log: %LOCALAPPDATA%\LumaNib\last-error.txt

LumaNib does not record the screen, connect automatically to the internet, or store ink or keystrokes. It saves appearance and shortcut settings locally and registers a startup entry when you enable launch at login. Clicking the project link opens GitHub in your browser.
Report issues at https://github.com/GongWenAI/LumaNib/issues with OS version, display scaling and reproduction steps. Do not share passwords or private recordings.

Launch at login (1.59)

In General, enable Launch at login. It is off by default and affects only the current user. At login, LumaNib runs in the menu bar/system tray without opening settings. Turning the option off removes the startup entry.

Mac uses system Login Items; if approval is pending, use Open System Login Items. Keep the app in Applications where possible. External drives must be connected and accessible at login. On Windows, keep the entire app folder in a permanent location. After moving it or updating to a new path, use Update Startup Path. If Windows disables the entry, allow it again in Startup Apps or Task Manager.

Mac registration and removal were checked on a real Mac. Actual logout/reboot and Windows registry/login behavior have not been tested.

License: see LICENSE in the package. Free use, modification and free sharing are allowed; sale is prohibited without written permission.
许可见下载包中的 LICENSE：允许免费使用、修改及免费分享，未经书面许可禁止销售。
