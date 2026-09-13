# Get started with LumaNib

[Home](../README.md) · [简体中文](#简体中文) · [Support](../SUPPORT.md)

## Install and open

Download the package for your platform from the [release hub](https://github.com/GongWenAI/LumaNib/releases) when a release is available to you. Releases currently remain private drafts.

- **Mac:** Apple Silicon, macOS 14 or later. Extract the ZIP and open `LumaNib.app`.
- **Windows:** Intel/AMD x64, Windows 10 version 1809 or later, or Windows 11. Extract the entire ZIP and open `LumaNib.exe`. The included runtime files must stay beside it; no separate .NET installation is needed.

The current Mac package is ad hoc signed and not notarized. Both packages lack commercial code signing. Use packages from the author's release hub and check their origin before opening them.

## Ring and pen

**Mouse Ring:** enable the ring, adjust its size and thickness, and choose each half's color. Left click lights the left half; right click lights the right half. The ring hides while drawing and returns afterward.

**Glow Pen:** choose ink color, width and glow. In “Press to toggle,” press the drawing shortcut once to start and again to stop; Esc also stops drawing. In “Hold to draw,” hold the shortcut while left-dragging and release the shortcut to stop. These instructions describe the default shortcut workflow; the settings window also has a drawing button.

Ink is freehand: there is no straight-line, arrow, text or automatic shape tool. Undo removes the last stroke; Clear Ink removes all strokes. With auto-fade on, choose how long ink stays before fading. With it off, ink remains until cleared or the app quits. After drawing stops, clicks pass through the ink.

Close the settings window to keep LumaNib running in the menu bar or system tray. Use its icon to reopen settings or quit. On Windows, double-click the tray icon to open settings; right-click for its menu.

## Shortcuts and language

The defaults are Control + Option on Mac and Ctrl + Alt on Windows, plus **R** for the ring, **D** for drawing, **Z** for undo and **X** for clear. **Esc** stops drawing or cancels shortcut capture.

Open Shortcuts, click a field, press a combination, then choose Apply Shortcuts. Mac combinations need Control, Option or Command; Windows combinations need Ctrl, Alt or Win. Esc is reserved on both platforms, and F12 is reserved on Windows. A shortcut claimed by the system or another app may be unavailable.

LumaNib uses your preferred system interface language: Chinese languages show Simplified Chinese, and other languages show English. Restart the app after changing the language. Language changes do not reset your colors or shortcuts.

## Record with OBS

LumaNib supplies an overlay, not a recording function. Use full display capture: **macOS Screen Capture → display** on Mac, or **Display Capture** on Windows. A single-window or game capture may omit the overlay. Make a short recording to confirm both ring and ink, and check audio separately.

Windows exclusive fullscreen, elevated apps and the UAC secure desktop may restrict overlays or input monitoring. Mixed-DPI displays and long sessions still need device testing. See [support](../SUPPORT.md) if the effect is missing.

---

# 简体中文

[中文首页](../README.zh-CN.md) · [使用支持](../SUPPORT.md#简体中文)

## 安装与打开

从作者的 [Releases](https://github.com/GongWenAI/LumaNib/releases) 获取与你的系统对应的发布包。目前发布仍为私有草稿，只能由有权限的人访问。

- **Mac：** macOS 14 及以上、M 系列芯片。完整解压，打开 `LumaNib.app`。
- **Windows：** Windows 10 1809 及以上或 Windows 11、Intel/AMD 64 位。完整解压，打开 `LumaNib.exe`，保留同目录所有运行库文件，无需另装 .NET。

当前 Mac 包使用临时签名，未做 Apple 公证；两个包均未做商业代码签名。请核对下载来源。

## 圆环与画笔

“鼠标圆环”中可以开关圆环、调整大小和粗细，左右两半分别选色。左键点亮左半环，右键点亮右半环。绘制时圆环会暂时隐藏，退出绘制后恢复。

“荧光画笔”中可以修改笔迹颜色、粗细和发光强度。选“按一下切换”时，按绘制快捷键开始，再按一次或按 Esc 退出；选“按住绘制”时，按住快捷键并用左键拖动画线，松开快捷键退出。设置窗口底部也有画笔按钮。

画笔保留手绘形状，没有直线、箭头、文字或自动识别形状的工具。支持撤销上一笔、清空全部笔迹和自动淡出。关闭淡出后，笔迹保留到手动清空或退出软件；退出画笔后，笔迹不会挡住下面软件的正常点击。

关闭设置窗口后，软件继续在 Mac 菜单栏或 Windows 托盘运行。通过图标可以重新打开设置或退出；Windows 双击托盘图标打开设置，右键展开菜单。

## 快捷键与语言

默认组合是 Mac 的 Control + Option、Windows 的 Ctrl + Alt，再加 **R** 开关圆环、**D** 绘制、**Z** 撤销、**X** 清空。**Esc** 退出绘制或取消快捷键录入。

在“全局快捷键”中点击输入框，直接按组合，再点“应用快捷键”。Mac 至少包含 Control、Option 或 Command；Windows 至少包含 Ctrl、Alt 或 Win。Esc 保留给退出操作，Windows 的 F12 也不能自定义。被系统或其他软件占用的组合可能无法使用。

软件跟随系统界面语言：中文语言显示简体中文，其余语言显示英文。改完系统语言后重启软件生效，不会重置颜色和快捷键。

## 配合 OBS 录屏

LumaNib 提供屏幕效果，本身不录屏。Mac 使用“macOS 屏幕采集”中的显示器捕获，Windows 使用“显示器采集”。窗口或游戏采集可能遗漏圆环、笔迹；请先短录一段，同时单独检查声音。

Windows 的独占全屏、管理员程序及 UAC 安全桌面可能限制覆盖或输入监听。多显示器缩放和长时间使用仍需实机验证。遇到问题可查看 [使用支持](../SUPPORT.md#简体中文)。
